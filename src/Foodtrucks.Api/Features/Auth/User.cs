using Microsoft.AspNetCore.Identity;

namespace Foodtrucks.Api.Features.Auth
{
    public class User
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "Vendor"; // Admin or Vendor
        public int? VendorId { get; set; }
    }
}
