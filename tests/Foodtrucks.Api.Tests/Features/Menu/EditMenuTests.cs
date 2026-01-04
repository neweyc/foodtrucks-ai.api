using FluentValidation;
using Foodtrucks.Api.Data;
using Foodtrucks.Api.Features.Menu;
using Foodtrucks.Api.Features.Trucks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Foodtrucks.Api.Tests.Features.Menu
{
    public class EditMenuTests
    {
        private readonly AppDbContext _db;
        private readonly Mock<IValidator<EditMenuItemRequest>> _validatorMock;
        private readonly Mock<ILogger<EditMenuItemCommandHandler>> _loggerMock;

        public EditMenuTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _db = new AppDbContext(options);
            _validatorMock = new Mock<IValidator<EditMenuItemRequest>>();
            _loggerMock = new Mock<ILogger<EditMenuItemCommandHandler>>();
        }

        [Fact]
        public async Task EditItem_UpdatesProperties_WhenItemExists()
        {
            // Arrange
            var item = new MenuItem 
            { 
                Name = "Burger", 
                Price = 10,
                Sizes = new List<MenuItemSize> { new MenuItemSize { Name = "S", Price = 8, MenuItemId = 1 } },
                Options = new List<MenuItemOption> { new MenuItemOption { Name = "Cheese", Price = 1, MenuItemId = 1 } }
            };
            _db.MenuItems.Add(item);
            await _db.SaveChangesAsync();

            var request = new EditMenuItemRequest 
            { 
                Name = "Cheeseburger", 
                Price = 12,
                Sizes = new List<MenuItemSizeDto> { new MenuItemSizeDto { Name = "L", Price = 12 } },
                Options = new List<MenuItemOptionDto>() // Removing options
            };

            _validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            var handler = new EditMenuItemCommandHandler(_db, _validatorMock.Object, _loggerMock.Object);

            // Act
            var result = await handler.Handle(item.Id, request, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            
            var updatedItem = await _db.MenuItems
                .Include(i => i.Sizes)
                .Include(i => i.Options)
                .FirstOrDefaultAsync(i => i.Id == item.Id);

            Assert.NotNull(updatedItem);
            Assert.Equal("Cheeseburger", updatedItem.Name);
            Assert.Equal(12, updatedItem.Price);
            
            Assert.Single(updatedItem.Sizes);
            Assert.Equal("L", updatedItem.Sizes.First().Name);
            
            Assert.Empty(updatedItem.Options);
        }

        [Fact]
        public async Task EditItem_ReturnsNotFound_WhenItemMissing()
        {
            // Arrange
            var request = new EditMenuItemRequest { Name = "Burger", Price = 10 };
            _validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            var handler = new EditMenuItemCommandHandler(_db, _validatorMock.Object, _loggerMock.Object);

            // Act
            var result = await handler.Handle(999, request, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
        }
    }
}
