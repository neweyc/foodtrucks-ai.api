using System.ComponentModel.DataAnnotations;

namespace Foodtrucks.Api.Features.Vendors
{
    public class Vendor
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        
        [MaxLength(100)]
        public string? StripeAccountId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
