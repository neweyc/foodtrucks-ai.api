using System.ComponentModel.DataAnnotations;
using Foodtrucks.Api.Features.Menu;
using Foodtrucks.Api.Features.Trucks;

namespace Foodtrucks.Api.Features.Orders
{
    public enum OrderStatus
    {
        Pending,
        Paid,
        Cooking,
        Ready,
        Completed,
        Cancelled
    }

    public class Order
    {
        public int Id { get; set; }
        public int TruckId { get; set; }
        // public Truck Truck { get; set; }

        [Required]
        public string CustomerName { get; set; } = string.Empty;
        [Required]
        public string CustomerPhone { get; set; } = string.Empty;
        
        public string TrackingCode { get; set; } = Guid.NewGuid().ToString("N");

        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }

        public List<OrderItem> Items { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        // public Order Order { get; set; }

        public int MenuItemId { get; set; }
        // public MenuItem MenuItem { get; set; }

        public string ItemName { get; set; } = string.Empty; // Snapshot
        public decimal Price { get; set; } // Snapshot
        public int Quantity { get; set; }
        
        public string? SelectedSize { get; set; } // Snapshot, e.g. "Large"
        public string? SelectedOptions { get; set; } // Snapshot, JSON or comma-separated, e.g. "Extra Cheese, No Onions"
    }
}
