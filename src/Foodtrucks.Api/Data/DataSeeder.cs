using Foodtrucks.Api.Features.Auth;
using Foodtrucks.Api.Features.Vendors;
using Foodtrucks.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Foodtrucks.Api.Data
{
    public class DataSeeder(AppDbContext db, IPasswordHasher passwordHasher)
    {
        public async Task SeedAsync()
        {

            // Seed Users
            if (!db.Users.Any())
            {
                 var users = new[]
                {
                    new User { UserName = "vendor1@foodtrucks.com", Email = "vendor1@foodtrucks.com", Role = "Vendor" },
                    new User { UserName = "vendor2@foodtrucks.com", Email = "vendor2@foodtrucks.com" , Role = "Vendor"},
                    new User { UserName = "neweycm@gmail.com", Email = "neweycm@gmail.com", Role = "Admin" }
                };

                foreach (var user in users)
                {
                    if (!await db.Users.AnyAsync(u => u.Email == user.Email))
                    {
                        var password = user.Email == "neweycm@gmail.com" ? "Admin1!" : "Testvendor1!";
                        user.PasswordHash = passwordHasher.HashPassword(password);
                        db.Users.Add(user);
                    }
                }
                await db.SaveChangesAsync();
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
            var allVendors = await db.Vendors.ToListAsync();
            if (allVendors.Count >= 2) 
            {
                var v1User = await db.Users.FirstOrDefaultAsync(u => u.Email == "vendor1@foodtrucks.com");
                if (v1User != null && v1User.VendorId == null) 
                {
                    v1User.VendorId = allVendors[0].Id;
                    // EF Core tracks changes, SaveChangesAsync at the end or explicitly here
                }

                var v2User = await db.Users.FirstOrDefaultAsync(u => u.Email == "vendor2@foodtrucks.com");
                if (v2User != null && v2User.VendorId == null)
                {
                    v2User.VendorId = allVendors[1].Id;
                }
                
                await db.SaveChangesAsync();
            }
        }
    }
}
