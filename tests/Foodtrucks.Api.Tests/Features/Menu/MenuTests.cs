using FluentValidation;
using Foodtrucks.Api.Data;
using Foodtrucks.Api.Features.Menu;
using Foodtrucks.Api.Features.Trucks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Foodtrucks.Api.Tests.Features.Menu
{
    public class MenuTests
    {
        private readonly AppDbContext _db;
        private readonly Mock<IValidator<AddMenuCategoryRequest>> _categoryValidatorMock;
        private readonly Mock<IValidator<AddMenuItemRequest>> _itemValidatorMock;
        private readonly Mock<ILogger<AddMenuItemCommandHandler>> _loggerMock;

        public MenuTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _db = new AppDbContext(options);
            _categoryValidatorMock = new Mock<IValidator<AddMenuCategoryRequest>>();
            _itemValidatorMock = new Mock<IValidator<AddMenuItemRequest>>();
            _loggerMock = new Mock<ILogger<AddMenuItemCommandHandler>>();
        }

        [Fact]
        public async Task AddCategory_CreatesCategory_WhenTruckExists()
        {
            // Arrange
            var truck = new Truck { Name = "T1", VendorId = 1 };
            _db.Trucks.Add(truck);
            await _db.SaveChangesAsync();

            var request = new AddMenuCategoryRequest { Name = "Burgers" };
            _categoryValidatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            var handler = new AddMenuCategoryCommandHandler(_db, _categoryValidatorMock.Object);

            // Act
            var result = await handler.Handle(truck.Id, request, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(_db.MenuCategories.FirstOrDefault(c => c.Name == "Burgers"));
        }

        [Fact]
        public async Task AddCategory_ReturnsNotFound_WhenTruckMissing()
        {
            var request = new AddMenuCategoryRequest { Name = "Burgers" };
            _categoryValidatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            var handler = new AddMenuCategoryCommandHandler(_db, _categoryValidatorMock.Object);

            // Act
            var result = await handler.Handle(999, request, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
        }

        [Fact]
        public async Task AddItem_CreatesItem_WhenCategoryExists()
        {
            // Arrange
            var category = new MenuCategory { Name = "C1", TruckId = 1 };
            _db.MenuCategories.Add(category);
            await _db.SaveChangesAsync();

            var request = new AddMenuItemRequest { Name = "Cheeseburger", Price = 10 };
            _itemValidatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            var handler = new AddMenuItemCommandHandler(_db, _itemValidatorMock.Object, _loggerMock.Object);

            // Act
            var result = await handler.Handle(category.Id, request, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(_db.MenuItems.FirstOrDefault(i => i.Name == "Cheeseburger"));
        }

        [Fact]
        public async Task AddItem_ReturnsNotFound_WhenCategoryMissing()
        {
            var request = new AddMenuItemRequest { Name = "Cheeseburger", Price = 10 };
            _itemValidatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            var handler = new AddMenuItemCommandHandler(_db, _itemValidatorMock.Object, _loggerMock.Object);

            // Act
            var result = await handler.Handle(999, request, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
        }
    }
}
