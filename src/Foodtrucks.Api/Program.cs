
using Foodtrucks.Api.Routing;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Serilog;

namespace Foodtrucks.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            // Add Serilog
            builder.Host.UseSerilog((ctx, lc) => lc
                .ReadFrom.Configuration(ctx.Configuration));

            // Add services to the container.

            
            builder.Services.AddDbContext<Foodtrucks.Api.Data.AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddAuthorization();
            
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder => builder.AllowAnyOrigin()
                                      .AllowAnyMethod()
                                      .AllowAnyHeader());
            });

            builder.Services.AddIdentityApiEndpoints<Foodtrucks.Api.Features.Auth.User>()
                .AddEntityFrameworkStores<Foodtrucks.Api.Data.AppDbContext>();

            builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
            {
                options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            });
            
            builder.Services.AddValidatorsFromAssemblyContaining<Program>();

            builder.Services.AddScoped<Foodtrucks.Api.Services.IPaymentService, Foodtrucks.Api.Services.MockPaymentService>();
            builder.Services.AddScoped<Foodtrucks.Api.Services.ISmsService, Foodtrucks.Api.Services.MockSmsService>();
            builder.Services.AddScoped<Foodtrucks.Api.Services.IVendorAuthorizationService, Foodtrucks.Api.Services.VendorAuthorizationService>();
            builder.Services.AddScoped<Foodtrucks.Api.Data.DataSeeder>();
            
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();
            //Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            //app.UseHttpsRedirection();
            
            app.UseExceptionHandler(); 
            
            //app.UseHttpsRedirection();
            
            app.UseSerilogRequestLogging();

            app.UseCors("AllowAll");

            app.UseAuthentication();
            app.UseAuthorization();
            
            app.MapGroup("/api/auth").MapIdentityApi<Foodtrucks.Api.Features.Auth.User>();

           

            ConfigureEndpoints(app);

            // Seed Data
            using (var scope = app.Services.CreateScope())
            {
                var seeder = scope.ServiceProvider.GetRequiredService<Foodtrucks.Api.Data.DataSeeder>();
                var db = scope.ServiceProvider.GetRequiredService<Foodtrucks.Api.Data.AppDbContext>();
                try 
                {
                    db.Database.Migrate();
                    seeder.SeedAsync().Wait();
                }
                catch (Exception ex)
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }

            app.Run();
        }


        private static void ConfigureEndpoints(WebApplication app)
        {
            var endpointServiceType = typeof(IEndpoint);
            var endpointServices = typeof(Program).Assembly.GetTypes()
                .Where(type => endpointServiceType.IsAssignableFrom(type) && !type.IsInterface);
            foreach (var endpointService in endpointServices)
            {
                IEndpoint? ep = (IEndpoint?)Activator.CreateInstance(endpointService);
                if (ep != null)
                {
                    ep.MapEndpoint(app);
                }
            }
        }
    }
}
