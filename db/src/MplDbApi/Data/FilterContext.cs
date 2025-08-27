using Microsoft.EntityFrameworkCore;
using MplDbApi.Models.Filters;

namespace MplDbApi.Data
{
    public class FilterContext : DbContext
    {
        public FilterContext()
        {
        }
        public FilterContext(DbContextOptions<FilterContext> options)
            : base(options)
        {
        }

        public DbSet<DataFilter> Filters { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DataFilter>(e =>
            {
                e.HasKey(f => f.Id);
                e.Property(f => f.AffectedRole)
                    .IsRequired()
                    .HasMaxLength(100);
            });
        }
    }
}