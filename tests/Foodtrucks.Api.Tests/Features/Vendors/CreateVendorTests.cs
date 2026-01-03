using FluentValidation;
using Foodtrucks.Api.Data;
using Foodtrucks.Api.Features.Vendors;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Foodtrucks.Api.Tests.Features.Vendors
{
    public class CreateVendorTests
    {
        private readonly AppDbContext _db;
        private readonly Mock<IValidator<CreateVendorRequest>> _validatorMock;

        public CreateVendorTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test
                .Options;
            _db = new AppDbContext(options);
            _validatorMock = new Mock<IValidator<CreateVendorRequest>>();
        }

        [Fact]
        public async Task Helper_CreatesVendor_WhenValid()
        {
            // Arrange
            var request = new CreateVendorRequest
            {
                Name = "Test Vendor",
                Description = "Delicious food",
                PhoneNumber = "1234567890",
                Website = "http://test.com"
            };

            _validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            var handler = new CreateVendorCommandHandler(_db, _validatorMock.Object);

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Id);
            
            var vendorInDb = await _db.Vendors.FindAsync(result.Id);
            Assert.NotNull(vendorInDb);
            Assert.Equal("Test Vendor", vendorInDb.Name);
        }

        [Fact]
        public async Task Helper_ReturnsFailure_WhenValidationFails()
        {
            // Arrange
            var request = new CreateVendorRequest();
            var validationResult = new FluentValidation.Results.ValidationResult();
            validationResult.Errors.Add(new FluentValidation.Results.ValidationFailure("Name", "Name is required"));

            _validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            var handler = new CreateVendorCommandHandler(_db, _validatorMock.Object);

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Name: Name is required", result.Message);
            
            Assert.Empty(_db.Vendors);
        }
    }
}
