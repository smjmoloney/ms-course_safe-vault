using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using SafeVaultShared;
using SafeVaultServer.Models;

namespace SafeVaultServer.Controllers;

public static class AuthController
{
	public static void MapRoutes(WebApplication app)
	{
		app.MapPost("/api/auth/login", async (LoginRequest req, UserManager<ApplicationUser> userManager, IConfiguration config) =>
		{
			if (!InputValidation.IsValidInput(req.Username))
				return Results.BadRequest("Invalid username.");

			var user = await userManager.FindByNameAsync(req.Username);
			if (user == null || !await userManager.CheckPasswordAsync(user, req.Password))
				return Results.Unauthorized();

			var roles = await userManager.GetRolesAsync(user);
			var token = GenerateToken(user, roles, config);
			return Results.Ok(new { token, role = roles.FirstOrDefault() ?? "Employee" });
		});

		app.MapPost("/api/auth/register", async (RegisterRequest req, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager) =>
		{
			if (!InputValidation.IsValidInput(req.Username))
				return Results.BadRequest("Invalid username.");

			if (req.Role != "Admin" && req.Role != "Employee")
				return Results.BadRequest("Role must be Admin or Employee.");

			var user = new ApplicationUser { UserName = req.Username };
			var result = await userManager.CreateAsync(user, req.Password);

			if (!result.Succeeded)
				return Results.BadRequest(result.Errors.Select(e => e.Description));

			await userManager.AddToRoleAsync(user, req.Role);
			return Results.Ok(new { message = $"{req.Role} '{req.Username}' registered.", passwordHash = user.PasswordHash });
		}).RequireAuthorization("AdminOnly");

		app.MapGet("/api/auth/hash/{username}", async (string username, UserManager<ApplicationUser> userManager) =>
		{
			var user = await userManager.FindByNameAsync(username);
			if (user == null)
				return Results.NotFound("User not found.");

			return Results.Ok(new { username = user.UserName, passwordHash = user.PasswordHash });
		}).RequireAuthorization("AdminOnly");
	}

	private static string GenerateToken(ApplicationUser user, IList<string> roles, IConfiguration config)
	{
		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
		var expiry = DateTime.UtcNow.AddMinutes(double.Parse(config["Jwt:ExpiryMinutes"]!));

		var claims = new List<Claim>
		{
			new(JwtRegisteredClaimNames.Sub, user.UserName!),
			new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
		};
		claims.AddRange(roles.Select(r => new Claim("role", r)));

		var token = new JwtSecurityToken(
			issuer: config["Jwt:Issuer"],
			audience: config["Jwt:Audience"],
			claims: claims,
			expires: expiry,
			signingCredentials: creds);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}
