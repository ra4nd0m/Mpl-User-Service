using Microsoft.EntityFrameworkCore;
using MplUserService.Models;

namespace MplUserService.Data
{
    public class UserContext : DbContext
    {
        public UserContext(DbContextOptions<UserContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Filter> Filters { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FavouriteIds)
                    .HasColumnType("jsonb");
            });

            modelBuilder.Entity<Filter>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Groups)
                    .HasColumnType("jsonb");
                entity.Property(e => e.Sources)
                    .HasColumnType("jsonb");
                entity.Property(e => e.Units)
                    .HasColumnType("jsonb");
                entity.Property(e => e.MaterialIds)
                    .HasColumnType("jsonb");
                entity.Property(e => e.Properties)
                    .HasColumnType("jsonb");
            });
        }
    }
}