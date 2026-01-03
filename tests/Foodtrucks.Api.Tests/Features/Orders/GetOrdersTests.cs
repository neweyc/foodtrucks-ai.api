using Foodtrucks.Api.Data;
using Foodtrucks.Api.Features.Orders;
using Foodtrucks.Api.Features.Trucks;
using Microsoft.EntityFrameworkCore;

namespace Foodtrucks.Api.Tests.Features.Orders
{
    public class GetOrdersTests
    {
        private readonly AppDbContext _db;

        public GetOrdersTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _db = new AppDbContext(options);
        }

        [Fact]
        public async Task GetOrders_ReturnsList()
        {
            var truck = new Truck { Name = "T1", VendorId = 1 };
            _db.Trucks.Add(truck);
            _db.Orders.Add(new Order { TruckId = truck.Id, CustomerName = "A", TotalAmount = 10 });
            _db.Orders.Add(new Order { TruckId = truck.Id, CustomerName = "B", TotalAmount = 20 });
            await _db.SaveChangesAsync();

            var handler = new GetOrdersQueryHandler(_db);
            var result = await handler.Handle(truck.Id, CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(2, result.Data.Count);
        }

        [Fact]
        public async Task GetOrder_ReturnsOrder_WhenFound()
        {
            var order = new Order { CustomerName = "Target", TotalAmount = 50, TruckId = 1 };
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            var handler = new GetOrderQueryHandler(_db);
            var result = await handler.Handle(order.TrackingCode, CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("Target", result.Data.CustomerName);
        }

        [Fact]
        public async Task GetOrder_ReturnsNotFound()
        {
            var handler = new GetOrderQueryHandler(_db);
            var result = await handler.Handle("999", CancellationToken.None);

            Assert.True(result.IsNotFound);
        }
    }
}
