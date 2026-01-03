using FluentValidation;
using Foodtrucks.Api.Data;
using Foodtrucks.Api.Features.Menu;
using Foodtrucks.Api.Features.Orders;
using Foodtrucks.Api.Features.Trucks;
using Foodtrucks.Api.Features.Vendors;
using Foodtrucks.Api.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Foodtrucks.Api.Tests.Features.Orders
{
    public class PlaceOrderTests
    {
        private readonly AppDbContext _db;
        private readonly Mock<IPaymentService> _paymentMock;
        private readonly Mock<ISmsService> _smsMock;
        private readonly Mock<IValidator<PlaceOrderRequest>> _validatorMock;

        public PlaceOrderTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _db = new AppDbContext(options);
            _paymentMock = new Mock<IPaymentService>();
            _smsMock = new Mock<ISmsService>();
            _validatorMock = new Mock<IValidator<PlaceOrderRequest>>();
        }

        [Fact]
        public async Task PlaceOrder_CreatesOrder_WhenValid()
        {
            // Arrange
            var vendor = new Vendor { Name = "V" };
            _db.Vendors.Add(vendor);
            var truck = new Truck { Name = "T1", VendorId = vendor.Id }; // Assuming ID 1
            _db.Trucks.Add(truck);
            await _db.SaveChangesAsync();

            var category = new MenuCategory { Name = "C1", TruckId = truck.Id };
            _db.MenuCategories.Add(category);
            await _db.SaveChangesAsync();

            var item = new MenuItem { Name = "Burger", Price = 10, MenuCategoryId = category.Id };
            _db.MenuItems.Add(item);
            await _db.SaveChangesAsync();

            var request = new PlaceOrderRequest
            {
                TruckId = truck.Id,
                CustomerName = "John",
                CustomerPhone = "555",
                PaymentToken = "tok_123",
                Items = new List<PlaceOrderItemDto> 
                { 
                    new PlaceOrderItemDto { MenuItemId = item.Id, Quantity = 2 } 
                }
            };

            _validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _paymentMock.Setup(p => p.ProcessPaymentAsync(20, "USD", "tok_123"))
                .ReturnsAsync(true);

            var handler = new PlaceOrderCommandHandler(_db, _paymentMock.Object, _smsMock.Object, _validatorMock.Object, CancellationToken.None);

            // Act
            var result = await handler.Handle(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Id);

            var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == result.Id);
            Assert.NotNull(order);
            Assert.Equal(20, order.TotalAmount);
            Assert.Equal(OrderStatus.Paid, order.Status);
            Assert.Single(order.Items);
            Assert.Equal("Burger", order.Items[0].ItemName);

            _smsMock.Verify(s => s.SendSmsAsync("555", It.Is<string>(msg => msg.Contains("Order #"))), Times.Once);
        }

        [Fact]
        public async Task PlaceOrder_Fails_WhenPaymentFails()
        {
            // Arrange
            var truck = new Truck { Name = "T1", VendorId = 1 };
            _db.Trucks.Add(truck);
            var category = new MenuCategory { Name = "C1", TruckId = truck.Id };
            _db.MenuCategories.Add(category);
            var item = new MenuItem { Name = "Burger", Price = 10, MenuCategoryId = category.Id };
            _db.MenuItems.Add(item);
            await _db.SaveChangesAsync();

            var request = new PlaceOrderRequest
            {
                TruckId = truck.Id,
                CustomerName = "John",
                PaymentToken = "tok_fail",
                Items = new List<PlaceOrderItemDto> { new PlaceOrderItemDto { MenuItemId = item.Id, Quantity = 1 } }
            };

            _validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _paymentMock.Setup(p => p.ProcessPaymentAsync(It.IsAny<decimal>(), "USD", "tok_fail"))
                .ReturnsAsync(false);

            var handler = new PlaceOrderCommandHandler(_db, _paymentMock.Object, _smsMock.Object, _validatorMock.Object, CancellationToken.None);

            // Act
            var result = await handler.Handle(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Payment failed", result.Message);
            Assert.Empty(_db.Orders);
        }
    }
}
