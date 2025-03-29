using System.Net.Http.Headers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MplAuthService.Data;
using MplAuthService.Interfaces;
using MplAuthService.Models;
using MplAuthService.Models.Dtos;
using MplAuthService.Utils;

namespace MplAuthService.Services
{
    public class UserService(UserManager<User> userManager, RoleManager<IdentityRole> roleManager,
        AuthContext context, ILogger<UserService> logger, IJwtService jwtService, IHttpClientFactory httpClientFactory) : IUserService
    {
        public async Task<User> CreateUser(string email, string password, OrganizationDto organization)
        {
            logger.LogInformation("Creating user with email {Email}", EmailObfuscator.ObfuscateEmail(email));
            if (await userManager.FindByEmailAsync(email) != null)
            {
                throw new InvalidOperationException("User already exists");
            }

            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var existingOrg = await context.Organizations.FirstOrDefaultAsync(o => o.Inn == organization.Inn);
                Organization org;
                if (existingOrg == null)
                {
                    org = new Organization
                    {
                        Name = organization.Name,
                        Inn = organization.Inn,
                        SubscriptionType = organization.SubscriptionType,
                        SubscriptionStartDate = DateTime.SpecifyKind(organization.SubscriptionStartDate, DateTimeKind.Utc),
                        SubscriptionEndDate = DateTime.SpecifyKind(organization.SubscriptionEndDate, DateTimeKind.Utc)
                    };
                    await context.Organizations.AddAsync(org);
                    await context.SaveChangesAsync();
                }
                else
                {
                    org = existingOrg;
                }
                var user = new User
                {
                    Email = email,
                    UserName = email,
                    Organization = org,
                };

                var result = await userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
                if (!await roleManager.RoleExistsAsync("User"))
                {
                    await roleManager.CreateAsync(new IdentityRole("User"));
                }
                await userManager.AddToRoleAsync(user, "User");

                await transaction.CommitAsync();
                logger.LogInformation("User created with email {Email}", EmailObfuscator.ObfuscateEmail(email));
                return user;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<User> CreateAdmin(string email, string password)
        {
            logger.LogInformation("Creating admin with email {Email}", EmailObfuscator.ObfuscateEmail(email));
            if (await userManager.FindByEmailAsync(email) != null)
            {
                throw new InvalidOperationException("User already exists");
            }

            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var admin = new User
                {
                    Email = email,
                    UserName = email,
                    Organization = null // Admin has no organization
                };

                var result = await userManager.CreateAsync(admin, password);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to create admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }

                // Create Admin role if it doesn't exist
                if (!await roleManager.RoleExistsAsync("Admin"))
                {
                    await roleManager.CreateAsync(new IdentityRole("Admin"));
                }

                await userManager.AddToRoleAsync(admin, "Admin");
                await transaction.CommitAsync();
                logger.LogInformation("Admin created with email {Email}", EmailObfuscator.ObfuscateEmail(email));
                return admin;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<User> GetUserByEmail(string email)
        {
            throw new NotImplementedException();
        }
        public async Task<List<User>> GetUsers()
        {
            try
            {
                logger.LogInformation("Getting all users");
                var users = await context.Users
                    .Include(u => u.Organization)
                    .ToListAsync();
                return users;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get users");
                throw;
            }
        }

        public async Task DeleteUser(User user)
        {
            logger.LogInformation("Deleting user with email {Email}", EmailObfuscator.ObfuscateEmail(user.Email));
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var refreshTokens = await context.RefreshTokens
                    .Where(rt => rt.UserId == user.Id)
                    .ToListAsync();
                if (refreshTokens.Count == 0)
                {
                    context.RefreshTokens.RemoveRange(refreshTokens);
                    await context.SaveChangesAsync();
                }

                try
                {
                    var client = httpClientFactory.CreateClient("ExternalUserApi");
                    string internalToken = jwtService.GenerateInternalToken();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", internalToken);

                    var response = await client.DeleteAsync($"user/{user.Id}");
                    if (!response.IsSuccessStatusCode)
                    {
                        logger.LogWarning("Failed to delete user from userapi. Status: {Status}", response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,"Error calling external api");
                }
                var result = await userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to delete user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
                await transaction.CommitAsync();
                logger.LogInformation("User deleted with email {Email}", EmailObfuscator.ObfuscateEmail(user.Email));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Failed to delete user with email {Email}", EmailObfuscator.ObfuscateEmail(user.Email));
                throw;
            }
        }
    }
}