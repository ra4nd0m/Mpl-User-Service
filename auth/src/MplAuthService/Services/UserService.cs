using Microsoft.AspNetCore.Identity;
using MplAuthService.Data;
using MplAuthService.Interfaces;
using MplAuthService.Models;
using MplAuthService.Models.Dtos;

namespace MplAuthService.Services
{
    public class UserService(UserManager<User> userManager, RoleManager<IdentityRole> roleManager,
        AuthContext context) : IUserService
    {
        public async Task<User> CreateUser(string email, string password, OrganizationDto organization)
        {
            if (await userManager.FindByEmailAsync(email) != null)
            {
                throw new InvalidOperationException("User already exists");
            }

            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var newOrg = new Organization
                {
                    Name = organization.Name,
                    Inn = organization.Inn,
                    SubscriptionType = organization.SubscriptionType,
                    SubscriptionStartDate = organization.SubscriptionStartDate,
                    SubscriptionEndDate = organization.SubscriptionEndDate,
                };

                await context.Organizations.AddAsync(newOrg);
                await context.SaveChangesAsync();

                var user = new User
                {
                    Email = email,
                    UserName = email,
                    Organization = newOrg,
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