using Foodtrucks.Api.Data;
using Foodtrucks.Api.Features.Trucks;
using Foodtrucks.Api.Features.Vendors;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;

namespace Foodtrucks.Api.Tests.Features.Trucks
{
    public class GetTrucksTests
    {
        private readonly AppDbContext _db;

        public GetTrucksTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _db = new AppDbContext(options);
        }

        [Fact]
        public async Task GetTrucks_ReturnsAllTrucks()
        {
            // Arrange
            // Need vendors to create trucks usually, but EF Core doesn't enforce FK constraints in InMemory unless configured? 
            // It does not enforce FKs by default. But code might not check.
            // Wait, Truck requires Vendor property? Let's check Truck entity.
            
            // Truck has VendorId. InMemory allows inserting without parent if no FK validation enabled.
            // But let's be safe.
            var vendor = new Vendor { Name = "V" };
            _db.Vendors.Add(vendor);
            await _db.SaveChangesAsync();

            _db.Trucks.Add(new Truck { Name = "T1", VendorId = vendor.Id });
            _db.Trucks.Add(new Truck { Name = "T2", VendorId = vendor.Id });
            await _db.SaveChangesAsync();

            var handler = new GetTrucksQueryHandler(_db);

            // Act
            var result = await handler.Handle(null, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetTruck_ReturnsTruck_WhenFound()
        {
            // Arrange
            var vendor = new Vendor { Name = "V" };
            _db.Vendors.Add(vendor);
            await _db.SaveChangesAsync();

            var truck = new Truck { Name = "Target", VendorId = vendor.Id };
            _db.Trucks.Add(truck);
            await _db.SaveChangesAsync();

            var handler = new GetTruckQueryHandler(_db);

            // Act
            var result = await handler.Handle(truck.Id, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Target", result.Data.Name);
        }

        [Fact]
        public async Task GetTruck_ReturnsNotFound_WhenMissing()
        {
            var handler = new GetTruckQueryHandler(_db);

            // Act
            var result = await handler.Handle(999, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
        }
    }
}
