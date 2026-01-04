using System.Security.Claims;
using Foodtrucks.Api.Data;
using Foodtrucks.Api.Routing;
using Foodtrucks.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Foodtrucks.Api.Features.Auth
{
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
        public string OldPassword { get; set; } = string.Empty;
    }

    public class AuthEndpoints : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/auth").WithTags("Auth");

            // POST /api/auth/login
            group.MapPost("/login", async (
                [FromBody] LoginRequest request,
                AppDbContext db,
                IPasswordHasher passwordHasher,
                HttpContext httpContext) =>
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user == null || !passwordHasher.VerifyPassword(user.PasswordHash, request.Password))
                {
                    // For security, generic error
                    return Results.Unauthorized();
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                if (user.VendorId.HasValue)
                {
                    claims.Add(new Claim("VendorId", user.VendorId.Value.ToString()));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddDays(14)
                };

                await httpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Return success (Token structure not needed anymore, but keeping JSON response format for client compatibility if needed)
                return Results.Ok(new { message = "Logged in successfully" });
            });

            // POST /api/auth/logout
            group.MapPost("/logout", async (HttpContext httpContext) =>
            {
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Ok(new { message = "Logged out" });
            });



            // POST /api/auth/manage/info (Change Password)
            group.MapPost("/manage/info", async (
                [FromBody] ChangePasswordRequest request,
                AppDbContext db,
                IPasswordHasher passwordHasher,
                HttpContext httpContext) =>
            {
                var userIdStr = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return Results.Unauthorized();
                }

                var user = await db.Users.FindAsync(userId);
                if (user == null) return Results.NotFound();

                if (!passwordHasher.VerifyPassword(user.PasswordHash, request.OldPassword))
                {
                    return Results.BadRequest(new { error = "Invalid old password" });
                }

                user.PasswordHash = passwordHasher.HashPassword(request.NewPassword);
                await db.SaveChangesAsync();

                return Results.Ok(new { message = "Password updated" });
            }).RequireAuthorization();
        }
    }
}
