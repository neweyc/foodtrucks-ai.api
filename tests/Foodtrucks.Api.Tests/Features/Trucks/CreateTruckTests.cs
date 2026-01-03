using FluentValidation;
using Foodtrucks.Api.Data;
using Foodtrucks.Api.Features.Trucks;
using Foodtrucks.Api.Features.Vendors;
using Foodtrucks.Api.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Foodtrucks.Api.Tests.Features.Trucks
{
    public class CreateTruckTests
    {
        private readonly AppDbContext _db;
        private readonly Mock<IValidator<CreateTruckRequest>> _validatorMock;
        private readonly Mock<IVendorAuthorizationService> _authServiceMock;

        public CreateTruckTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _db = new AppDbContext(options);
            _validatorMock = new Mock<IValidator<CreateTruckRequest>>();
            _authServiceMock = new Mock<IVendorAuthorizationService>();
        }

        [Fact]
        public async Task Helper_CreatesTruck_WhenValid()
        {
            // Arrange
            // Create a vendor first
            var vendor = new Vendor { Name = "V", IsActive = true };
            _db.Vendors.Add(vendor);
            await _db.SaveChangesAsync();

            var request = new CreateTruckRequest
            {
                Name = "Taco Truck",
                Description = "Tacos",
                Schedule = "Mon-Fri"
            };

            _validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            // Mock Auth Service to return the vendor ID
            _authServiceMock.Setup(a => a.GetVendorIdAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(vendor.Id);

            var handler = new CreateTruckCommandHandler(_db, _validatorMock.Object, _authServiceMock.Object);

            // Act
            var user = new System.Security.Claims.ClaimsPrincipal();
            var result = await handler.Handle(request, user, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Id);
            
            var truckInDb = await _db.Trucks.FindAsync(result.Id);
            Assert.NotNull(truckInDb);
            Assert.Equal("Taco Truck", truckInDb.Name);
            Assert.Equal(vendor.Id, truckInDb.VendorId);
        }

        [Fact]
        public async Task Helper_ReturnsFailure_WhenUserNotLinkedToVendor()
        {
            // Arrange
            var request = new CreateTruckRequest { Name = "Truck" }; 

            _validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            // Mock Auth Service to return null (no vendor linked)
            _authServiceMock.Setup(a => a.GetVendorIdAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync((int?)null);

            var handler = new CreateTruckCommandHandler(_db, _validatorMock.Object, _authServiceMock.Object);

            // Act
            var user = new System.Security.Claims.ClaimsPrincipal();
            var result = await handler.Handle(request, user, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("User is not associated with a vendor account", result.Message);
        }
    }
}
