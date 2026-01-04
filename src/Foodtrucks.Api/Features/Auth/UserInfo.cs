using System.Security.Claims;
using Foodtrucks.Api.Data;
using Foodtrucks.Api.Routing;
using Microsoft.EntityFrameworkCore;

namespace Foodtrucks.Api.Features.Auth
{
    public class UserDto
    {
        public string Email { get; set; } = string.Empty;
        public int? VendorId { get; set; }
    }

    public class UserInfoEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/auth/me", async (ClaimsPrincipal user, AppDbContext db) =>
            {
                var userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return Results.Unauthorized();
                }

                var appUser = await db.Users.FindAsync(userId);
                if (appUser == null) return Results.Unauthorized();

                return Results.Ok(new UserDto
                {
                    Email = appUser.Email ?? "",
                    VendorId = appUser.VendorId
                });
            })
            .WithTags("Auth")
            .WithOpenApi()
            .RequireAuthorization();
        }
    }
}
