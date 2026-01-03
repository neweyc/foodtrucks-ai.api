using System.ComponentModel.DataAnnotations;
using Foodtrucks.Api.Features.Vendors;

namespace Foodtrucks.Api.Features.Trucks
{
    public class Truck
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PhotoUrl { get; set; } = string.Empty;
        
        public int VendorId { get; set; }
        public Vendor Vendor { get; set; }

        // Location
        public double CurrentLatitude { get; set; }
        public double CurrentLongitude { get; set; }
        public bool IsActive { get; set; }

        // Schedule (Simple string for now, could be complex object)
        public string Schedule { get; set; } = string.Empty;
        
        public List<Menu.MenuCategory> MenuCategories { get; set; } = new();
    }
}
