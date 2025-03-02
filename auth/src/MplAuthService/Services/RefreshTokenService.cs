

using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using MplAuthService.Data;
using MplAuthService.Interfaces;
using MplAuthService.Models;

namespace MplAuthService.Services
{
    public class RefreshTokenService(IConfiguration configuration, AuthContext context) : IRefreshTokenService
    {
        public async Task<RefreshToken> GenerateRefreshToken(User user)
        {
            int tokenTokenExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryDays"] ?? throw new InvalidOperationException("RefreshTokenExpiryDays is not set"));
            RefreshToken token = new()
            {
                UserId = user.Id,
                User = user,
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                Expires = DateTime.UtcNow.AddDays(tokenTokenExpiryDays),
            };
            await context.RefreshTokens.AddAsync(token);
            await context.SaveChangesAsync();
            return token;
        }
        public async Task<RefreshToken?> ValidateRefreshToken(string token)
        {
            var refreshToken = await context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token);
            if (refreshToken == null || refreshToken.IsExpired)
            {
                return null;
            }
            return refreshToken;
        }
        public async Task RecallRefreshToken(string token)
        {
            var refreshToken = await context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token);
            if (refreshToken != null)
            {
                refreshToken.Expires = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
        }
    }
}