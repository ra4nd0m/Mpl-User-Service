using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using MplAuthService.Models;

namespace MplAuthService.Data
{
    public class AuthContext : IdentityDbContext<User>
    {
        public AuthContext(DbContextOptions<AuthContext> options) : base(options)
        {
        }
        public DbSet<Organization> Organizations { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Organization>()
                .HasMany(o => o.Users)
                .WithOne(u => u.Organization)
                .HasForeignKey(u => u.OrganizationId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId);
        }
    }
}