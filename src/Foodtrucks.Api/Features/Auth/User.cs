using Microsoft.AspNetCore.Identity;

namespace Foodtrucks.Api.Features.Auth
{
    public class User : IdentityUser
    {
        // Add custom properties if needed, e.g. VendorId if user is a vendor
        public int? VendorId { get; set; }
    }
}
