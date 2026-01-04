using System.Security.Claims;
using Foodtrucks.Api.Data;
using Foodtrucks.Api.Features.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Foodtrucks.Api.Services
{
    public interface IVendorAuthorizationService
    {
        Task<int?> GetVendorIdAsync(ClaimsPrincipal principal);
        Task<bool> CanManageTruckAsync(ClaimsPrincipal principal, int truckId);
    }

    public class VendorAuthorizationService(AppDbContext db) : IVendorAuthorizationService
    {
        public async Task<int?> GetVendorIdAsync(ClaimsPrincipal principal)
        {
            // First try to get from claims (fastest)
            var vendorIdClaim = principal.FindFirst("VendorId")?.Value;
            if (int.TryParse(vendorIdClaim, out int vendorId))
            {
                return vendorId;
            }

            // Fallback to database lookup via NameIdentifier (UserId)
            var userIdStr = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                 var user = await db.Users.FindAsync(userId);
                 return user?.VendorId;
            }

            return null;
        }

        public async Task<bool> CanManageTruckAsync(ClaimsPrincipal principal, int truckId)
        {
            var vendorId = await GetVendorIdAsync(principal);
            if (vendorId == null) return false;

            return await db.Trucks.AnyAsync(t => t.Id == truckId && t.VendorId == vendorId);
        }
    }
}
