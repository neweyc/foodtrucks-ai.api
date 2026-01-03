using Foodtrucks.Api.Data;
using Foodtrucks.Api.Features.Trucks;
using Foodtrucks.Api.Features.Vendors;
using Foodtrucks.Api.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace Foodtrucks.Api.Tests.Features.Trucks
{
    public class UpdateTruckTests
    {
        private readonly AppDbContext _db;
        private readonly Mock<IVendorAuthorizationService> _authServiceMock;

        public UpdateTruckTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _db = new AppDbContext(options);
            _authServiceMock = new Mock<IVendorAuthorizationService>();
        }

        [Fact]
        public async Task Helper_UpdatesTruck_WhenUserIsOwner()
        {
            // Arrange
            var vendor = new Vendor { Name = "V", IsActive = true };
            _db.Vendors.Add(vendor);
            await _db.SaveChangesAsync();

            var truck = new Truck { Name = "Old Name", VendorId = vendor.Id, IsActive = true };
            _db.Trucks.Add(truck);
            await _db.SaveChangesAsync();

            var request = new UpdateTruckRequest("New Name", "New Desc", "New Sched");

            // Mock Auth Service: User CAN manage this truck
            _authServiceMock.Setup(a => a.CanManageTruckAsync(It.IsAny<ClaimsPrincipal>(), truck.Id))
                .ReturnsAsync(true);

            var handler = new UpdateTruckCommandHandler(_db, _authServiceMock.Object);

            // Act
            var result = await handler.Handle(truck.Id, request, new ClaimsPrincipal(), CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            
            var updatedTruck = await _db.Trucks.FindAsync(truck.Id);
            Assert.Equal("New Name", updatedTruck!.Name);
        }

        [Fact]
        public async Task Helper_ReturnsForbidden_WhenUserIsNotOwner()
        {
            // Arrange
            var truckId = 123;
            var request = new UpdateTruckRequest("New Name", "New Desc", "New Sched");

            // Mock Auth Service: User CANNOT manage this truck
            _authServiceMock.Setup(a => a.CanManageTruckAsync(It.IsAny<ClaimsPrincipal>(), truckId))
                .ReturnsAsync(false);

            var handler = new UpdateTruckCommandHandler(_db, _authServiceMock.Object);

            // Act
            var result = await handler.Handle(truckId, request, new ClaimsPrincipal(), CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.IsForbidden);
            Assert.Contains("permission", result.Message);
        }

        [Fact]
        public async Task Helper_ReturnsNotFound_WhenTruckDoesNotExist()
        {
             // Arrange
            var truckId = 999;
             var request = new UpdateTruckRequest("New Name", "New Desc", "New Sched");

            // Mock Auth Service: User CAN manage this truck (conceptually, if it existed, or simple bypass)
            // Note: Our handler checks Auth check first. If we return true here, we expect Not Found next.
            _authServiceMock.Setup(a => a.CanManageTruckAsync(It.IsAny<ClaimsPrincipal>(), truckId))
                .ReturnsAsync(true);

            var handler = new UpdateTruckCommandHandler(_db, _authServiceMock.Object);

            // Act
            var result = await handler.Handle(truckId, request, new ClaimsPrincipal(), CancellationToken.None);

             // Assert
            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
        }
    }
}
