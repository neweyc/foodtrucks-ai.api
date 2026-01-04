using Foodtrucks.Api.Features.Auth;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace Foodtrucks.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            
        }



        public DbSet<Features.Auth.User> Users { get; set; }
        public DbSet<Features.Vendors.Vendor> Vendors { get; set; }
        public DbSet<Features.Trucks.Truck> Trucks { get; set; }
        public DbSet<Features.Menu.MenuCategory> MenuCategories { get; set; }
        public DbSet<Features.Menu.MenuItem> MenuItems { get; set; }
        public DbSet<Features.Orders.Order> Orders { get; set; }
        public DbSet<Features.Orders.OrderItem> OrderItems { get; set; }
    }
}
