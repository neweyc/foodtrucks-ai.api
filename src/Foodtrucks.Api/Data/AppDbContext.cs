using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Foodtrucks.Api.Features.Auth;

namespace Foodtrucks.Api.Data
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Features.Vendors.Vendor> Vendors { get; set; }
        public DbSet<Features.Trucks.Truck> Trucks { get; set; }
        public DbSet<Features.Menu.MenuCategory> MenuCategories { get; set; }
        public DbSet<Features.Menu.MenuItem> MenuItems { get; set; }
        public DbSet<Features.Orders.Order> Orders { get; set; }
        public DbSet<Features.Orders.OrderItem> OrderItems { get; set; }
    }
}
