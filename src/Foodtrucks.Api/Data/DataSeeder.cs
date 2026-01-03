using Foodtrucks.Api.Features.Auth;
using Foodtrucks.Api.Features.Vendors;
using Microsoft.AspNetCore.Identity;

namespace Foodtrucks.Api.Data
{
    public class DataSeeder(AppDbContext db, UserManager<User> userManager)
    {
        public async Task SeedAsync()
        {

            // Seed Users
            if (!db.Users.Any())
            {
                 var users = new[]
                {
                    new User { UserName = "vendor1@foodtrucks.com", Email = "vendor1@foodtrucks.com" },
                    new User { UserName = "vendor2@foodtrucks.com", Email = "vendor2@foodtrucks.com" }
                };

                foreach (var user in users)
                {
                    if (await userManager.FindByEmailAsync(user.Email!) == null)
                    {
                        await userManager.CreateAsync(user, "Testvendor1!");
                    }
                }
            }
           
            // Seed Vendors
            if (!db.Vendors.Any())
            {
                var vendors = new[]
                {
                    new Vendor { Name = "Tacos Hermanos", Description = "Authentic Street Tacos", PhoneNumber = "555-0101", IsActive = true },
                    new Vendor { Name = "Burger Kingish", Description = "Flame grilled (maybe)", PhoneNumber = "555-0102", IsActive = true }
                };

                db.Vendors.AddRange(vendors);
                await db.SaveChangesAsync();
            }

            // Always attempt to link users to ensure they are connected
            var allVendors = db.Vendors.ToList();
            if (allVendors.Count >= 2) 
            {
                var v1User = await userManager.FindByEmailAsync("vendor1@foodtrucks.com");
                if (v1User != null && v1User.VendorId == null) 
                {
                    v1User.VendorId = allVendors[0].Id;
                    await userManager.UpdateAsync(v1User);
                }

                var v2User = await userManager.FindByEmailAsync("vendor2@foodtrucks.com");
                if (v2User != null && v2User.VendorId == null)
                {
                    v2User.VendorId = allVendors[1].Id;
                    await userManager.UpdateAsync(v2User);
                }
            }
        }
    }
}
