using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using MplAuthService.Interfaces;
using MplAuthService.Models;
using MplAuthService.Utils;

namespace MplAuthService.Services
{
    public class JwtService(IConfiguration configuration, UserManager<User> userManager, ILogger<JwtService> logger) : IJwtService
    {
        public async Task<string> GenerateJwtToken(User user)
        {
            logger.LogInformation("Generating JWT token for user {email}", EmailObfuscator.ObfuscateEmail(user.Email));
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is not configured")));
            var signingCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var roles = await userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new (ClaimTypes.Name, user.Email ?? throw new InvalidOperationException("User email is not set")),
                new(ClaimTypes.NameIdentifier, user.Id),

            };

            if (!roles.Contains("Admin"))
            {
                if (user.Organization != null)
                {
                    claims.AddRange(
                    [
                        new("OrganizationId", user.Organization?.Id.ToString() ?? String.Empty),
                        new("SubscriptionType", user.Organization?.SubscriptionType.ToString() ?? throw new InvalidOperationException("Subscription type is not set")),
                        new("SubscriptionEnd", user.Organization?.SubscriptionEndDate.ToString("O") ?? throw new InvalidOperationException("Subscription end date is not set"))
                    ]);
                }
                else if (user.IndividualSubscription != null)
                {
                    claims.AddRange(
                    [
                        new("SubscriptionType", user.IndividualSubscription.SubscriptionType.ToString() ?? throw new InvalidOperationException("Subscription type is not set")),
                        new("SubscriptionEnd", user.IndividualSubscription.SubscriptionEndDate.ToString("O") ?? throw new InvalidOperationException("Subscription end date is not set"))
                    ]);
                }

            }

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            int tokenTokenExpiryMinutes = int.Parse(configuration["Jwt:TokenExpiryMinutes"] ?? throw new InvalidOperationException("Token expiration is not configured"));
            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(tokenTokenExpiryMinutes),
                signingCredentials: signingCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public string GenerateInternalToken()
        {
            logger.LogInformation("Generating internal token");
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is not configured")));
            var signingCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, "internal"),
                new(ClaimTypes.NameIdentifier, "internal"),
                new(ClaimTypes.Role, "internal")
            };

            int tokenTokenExpiryMinutes = int.Parse(configuration["Jwt:TokenExpiryMinutes"] ?? throw new InvalidOperationException("Token expiration is not configured"));
            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(tokenTokenExpiryMinutes),
                signingCredentials: signingCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}