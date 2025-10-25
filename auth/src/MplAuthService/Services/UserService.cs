using System.Net.Http.Headers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MplAuthService.Data;
using MplAuthService.Interfaces;
using MplAuthService.Models;
using MplAuthService.Models.Dtos;
using MplAuthService.Models.Enums;
using MplAuthService.Utils;

namespace MplAuthService.Services
{
    public class UserService(UserManager<User> userManager, RoleManager<IdentityRole> roleManager,
        AuthContext context, ILogger<UserService> logger, IJwtService jwtService, IHttpClientFactory httpClientFactory, IOrgService orgService) : IUserService
    {
        public async Task<User> CreateUser(CreateUserDto userDto)
        {
            logger.LogInformation("Creating user with email {Email}", EmailObfuscator.ObfuscateEmail(userDto.Email));
            var email = userDto.Email;
            var password = userDto.Password;
            var organization = userDto.Organization;
            var subscription = userDto.SubscriptionData;
            if (await userManager.FindByEmailAsync(email) != null)
            {
                throw new InvalidOperationException("User already exists");
            }

            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                if (subscription == null && organization == null)
                {
                    throw new ArgumentException("Either organization or subscription details must be provided");
                }

                if( organization != null && subscription != null)
                {
                    throw new ArgumentException("Cannot provide both organization and subscription details");
                }

                var user = new User
                {
                    Email = email,
                    UserName = email,
                };

                if (organization != null && subscription == null)
                {
                    var existingOrg = await orgService.GetOrganization(organization.Inn);
                    Organization org;
                    if (existingOrg == null)
                    {
                        org = await orgService.CreateOrganization(organization);
                    }
                    else
                    {
                        org = existingOrg;
                    }
                    user.Organization = org;
                }

                if (subscription != null && organization == null)
                {
                    user.IndividualSubscription = new IndividualSubscription
                    {
                        SubscriptionType = subscription.SubscriptionType,
                        SubscriptionStartDate = subscription.SubscriptionStartDate,
                        SubscriptionEndDate = subscription.SubscriptionEndDate,
                        User = user
                    };
                }

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

        public async Task<User> UpdateUser(User user, UpdateUserDto updateUser)
        {
            logger.LogInformation("Updating user with email {Email}", EmailObfuscator.ObfuscateEmail(user.Email));
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                // Update email (with uniqueness check and normalization)
                if (!string.IsNullOrEmpty(updateUser.NewEmail) && !string.Equals(user.Email, updateUser.NewEmail, StringComparison.OrdinalIgnoreCase))
                {
                    var existing = await userManager.FindByEmailAsync(updateUser.NewEmail);
                    if (existing != null && existing.Id != user.Id)
                    {
                        throw new InvalidOperationException("A user with the specified email already exists");
                    }

                    var setEmailResult = await userManager.SetEmailAsync(user, updateUser.NewEmail);
                    if (!setEmailResult.Succeeded)
                    {
                        throw new InvalidOperationException($"Failed to update email: {string.Join(", ", setEmailResult.Errors.Select(e => e.Description))}");
                    }

                    var setUserNameResult = await userManager.SetUserNameAsync(user, updateUser.NewEmail);
                    if (!setUserNameResult.Succeeded)
                    {
                        throw new InvalidOperationException($"Failed to update username: {string.Join(", ", setUserNameResult.Errors.Select(e => e.Description))}");
                    }
                }

                if (!string.IsNullOrEmpty(updateUser.Password))
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    var result = await userManager.ResetPasswordAsync(user, token, updateUser.Password);
                    if (!result.Succeeded)
                    {
                        throw new InvalidOperationException($"Failed to update password: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }

                // Update organization (reuse by INN if exists, otherwise create)
                if (updateUser.Organization != null && user.IndividualSubscription == null)
                {
                    var desiredInn = updateUser.Organization.Inn;
                    var existingOrg = await orgService.GetOrganization(desiredInn);

                    Organization orgToUse;
                    if (existingOrg != null)
                    {
                        orgToUse = existingOrg;
                    }
                    else
                    {
                        orgToUse = await orgService.CreateOrganization(updateUser.Organization);
                    }

                    user.Organization = orgToUse;
                }

                if (updateUser.SubscriptionData != null && user.Organization == null)
                {
                    if(user.IndividualSubscription != null)
                    {
                        user.IndividualSubscription.SubscriptionType = updateUser.SubscriptionData.SubscriptionType;
                        user.IndividualSubscription.SubscriptionStartDate = updateUser.SubscriptionData.SubscriptionStartDate;
                        user.IndividualSubscription.SubscriptionEndDate = updateUser.SubscriptionData.SubscriptionEndDate;
                    }
                    else
                    {
                        user.IndividualSubscription = new IndividualSubscription
                        {
                            SubscriptionType = updateUser.SubscriptionData.SubscriptionType,
                            SubscriptionStartDate = updateUser.SubscriptionData.SubscriptionStartDate,
                            SubscriptionEndDate = updateUser.SubscriptionData.SubscriptionEndDate,
                            User = user
                        };
                    }
                }

                // Persist user changes (Identity + FK/navigations)
                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to update user: {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
                }

                // Ensure any remaining tracked changes are saved
                await context.SaveChangesAsync();

                // Reload with Organization to return fully populated entity
                var updatedUser = await context.Users
                    .Include(u => u.Organization)
                    .Include(u => u.IndividualSubscription)
                    .FirstAsync(u => u.Id == user.Id);

                await transaction.CommitAsync();
                logger.LogInformation("User updated with email {Email}", EmailObfuscator.ObfuscateEmail(updatedUser.Email));

                return updatedUser;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Failed to update user with email {Email}", EmailObfuscator.ObfuscateEmail(user.Email));
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
            try
            {
                logger.LogInformation("Fetching user by email {Email}", EmailObfuscator.ObfuscateEmail(email));

                if (string.IsNullOrWhiteSpace(email))
                {
                    throw new ArgumentException("Email must be provided", nameof(email));
                }

                var identityUser = await userManager.FindByEmailAsync(email) ?? throw new InvalidOperationException("User not found");
                var userWithOrg = await context.Users
                    .Include(u => u.Organization)
                    .Include(u => u.IndividualSubscription)
                    .FirstOrDefaultAsync(u => u.Id == identityUser.Id);

                if (userWithOrg == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                return userWithOrg;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch user by email {Email}", EmailObfuscator.ObfuscateEmail(email));
                throw;
            }
        }
        public async Task<List<User>> GetUsers()
        {
            try
            {
                logger.LogInformation("Getting all users");
                var users = await context.Users
                    .Include(u => u.Organization)
                    .Include(u => u.IndividualSubscription)
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
                if (refreshTokens.Count > 0)
                {
                    context.RefreshTokens.RemoveRange(refreshTokens);
                    await context.SaveChangesAsync();
                }

                var individualSubscription = await context.IndividualSubscriptions
                    .FirstOrDefaultAsync(sub => sub.UserId == user.Id);
                if (individualSubscription != null)
                {
                    context.IndividualSubscriptions.Remove(individualSubscription);
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
                    logger.LogError(ex, "Error calling external api");
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