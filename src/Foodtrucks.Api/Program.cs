
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
                    builder => builder.SetIsOriginAllowed(_ => true) // Allow any origin in dev
                                      .AllowAnyMethod()
                                      .AllowAnyHeader()
                                      .AllowCredentials());
            });

            // Custom Cookie Authentication
            builder.Services.AddAuthentication("Cookies")
                .AddCookie("Cookies", options =>
                {
                    options.Cookie.Name = "FoodTrucksAuth";
                    // In Development (localhost), use Lax + SameAsRequest to avoid HTTPS/SameSite issues
                    // In Production, use None + Always for cross-origin/SSL
                    if (builder.Environment.IsDevelopment())
                    {
                        options.Cookie.SameSite = SameSiteMode.Lax;
                        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    }
                    else
                    {
                        options.Cookie.SameSite = SameSiteMode.None;
                        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    }
                    
                    options.Events.OnRedirectToLogin = context =>
                    {
                        context.Response.StatusCode = 401;
                        return Task.CompletedTask;
                    };
                });
            
            // Explicitly remove Identity services
            
            builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
            {
                options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            });
            
            builder.Services.AddValidatorsFromAssemblyContaining<Program>();

            builder.Services.AddScoped<Foodtrucks.Api.Services.IPaymentService, Foodtrucks.Api.Services.MockPaymentService>();
            builder.Services.AddScoped<Foodtrucks.Api.Services.ISmsService, Foodtrucks.Api.Services.MockSmsService>();
            builder.Services.AddScoped<Foodtrucks.Api.Services.IVendorAuthorizationService, Foodtrucks.Api.Services.VendorAuthorizationService>();
            builder.Services.AddScoped<Foodtrucks.Api.Services.IPasswordHasher, Foodtrucks.Api.Services.PasswordHasher>();
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
            
            //app.UseHttpsRedirection(); // moved up if needed, but currently commented out
            
            app.UseCors("AllowAll"); // Moved CORS early
            
            app.UseSerilogRequestLogging();

            app.UseAuthentication();
            app.UseAuthorization();
            
           

            ConfigureEndpoints(app);

            // Seed Data
            using (var scope = app.Services.CreateScope())
            {
                var seeder = scope.ServiceProvider.GetRequiredService<Foodtrucks.Api.Data.DataSeeder>();
                var db = scope.ServiceProvider.GetRequiredService<Foodtrucks.Api.Data.AppDbContext>();
                try 
                {
                    // db.Database.EnsureCreated();
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
