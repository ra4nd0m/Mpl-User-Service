using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MplAuthService.Data;
using MplAuthService.Interfaces;
using MplAuthService.Models;
using MplAuthService.Models.Dtos;

namespace MplAuthService.Services
{
    public class UserService(UserManager<User> userManager, RoleManager<IdentityRole> roleManager,
        AuthContext context, ILogger<UserService> logger) : IUserService
    {
        public async Task<User> CreateUser(string email, string password, OrganizationDto organization)
        {
            logger.LogInformation("Creating user with email {Email}", email);
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
                        SubscriptionStartDate = organization.SubscriptionStartDate,
                        SubscriptionEndDate = organization.SubscriptionEndDate,
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
                logger.LogInformation("User created with email {Email}", email);
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
            logger.LogInformation("Creating admin with email {Email}", email);
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
                logger.LogInformation("Admin created with email {Email}", email);
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

        public async Task DeleteUser(User user)
        {
            throw new NotImplementedException();
        }
    }
}