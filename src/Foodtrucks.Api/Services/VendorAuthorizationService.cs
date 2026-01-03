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

    public class VendorAuthorizationService(UserManager<User> userManager, AppDbContext db) : IVendorAuthorizationService
    {
        public async Task<int?> GetVendorIdAsync(ClaimsPrincipal principal)
        {
            var user = await userManager.GetUserAsync(principal);
            return user?.VendorId;
        }

        public async Task<bool> CanManageTruckAsync(ClaimsPrincipal principal, int truckId)
        {
            var vendorId = await GetVendorIdAsync(principal);
            if (vendorId == null) return false;

            return await db.Trucks.AnyAsync(t => t.Id == truckId && t.VendorId == vendorId);
        }
    }
}
