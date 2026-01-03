using System.ComponentModel.DataAnnotations;
using Foodtrucks.Api.Features.Trucks;

namespace Foodtrucks.Api.Features.Menu
{
    public class MenuCategory
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public int TruckId { get; set; }
        // public Truck Truck { get; set; } // Avoid cycle for now or configure in DbContext
        
        public List<MenuItem> MenuItems { get; set; } = new();
    }

    public class MenuItem
    {
        public int Id { get; set; }
        public int MenuCategoryId { get; set; }
        public MenuCategory MenuCategory { get; set; }
        
        // Denormalized or direct reference to Truck for easier querying? 
        // Strict hierarchy: Truck -> Category -> Item
        
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string PhotoUrl { get; set; } = string.Empty;
        public bool IsAvailable { get; set; } = true;

        public List<MenuItemSize> Sizes { get; set; } = new();
        public List<MenuItemOption> Options { get; set; } = new();
    }

    public class MenuItemSize
    {
        public int Id { get; set; }
        public int MenuItemId { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class MenuItemOption
    {
        public int Id { get; set; }
        public int MenuItemId { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty; // e.g., "Extra Cheese"
        public string Section { get; set; } = string.Empty; // e.g., "Add-ons", "Removals"
        public decimal Price { get; set; } // Can be 0
    }
}
