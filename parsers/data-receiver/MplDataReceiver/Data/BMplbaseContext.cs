using Microsoft.EntityFrameworkCore;
using MplDataReceiver.Models;

namespace MplDataReceiver.Data;

public partial class BMplbaseContext : DbContext
{
    public BMplbaseContext()
    {
    }

    public BMplbaseContext(DbContextOptions<BMplbaseContext> options)
        : base(options)
    {
    }

    public virtual DbSet<DeliveryType> DeliveryTypes { get; set; }

    public virtual DbSet<Material> Materials { get; set; }

    public virtual DbSet<MaterialGroup> MaterialGroups { get; set; }

    public virtual DbSet<MaterialProperty> MaterialProperties { get; set; }

    public virtual DbSet<MaterialSource> MaterialSources { get; set; }

    public virtual DbSet<MaterialValue> MaterialValues { get; set; }

    public virtual DbSet<Property> Properties { get; set; }

    public virtual DbSet<SchemaMigration> SchemaMigrations { get; set; }

    public virtual DbSet<Source> Sources { get; set; }

    public virtual DbSet<Unit> Units { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");

        modelBuilder.Entity<DeliveryType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("delivery_type_pk");

            entity.ToTable("delivery_type");

            entity.HasIndex(e => e.Name, "delivery_type_unique").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
        });

        modelBuilder.Entity<Material>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("material_pk");

            entity.ToTable("material");

            entity.HasIndex(e => e.Name, "material_name_uindex").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
        });

        modelBuilder.Entity<MaterialGroup>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("material_group_pk");

            entity.ToTable("material_group");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
        });

        modelBuilder.Entity<MaterialProperty>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("material_property_pk");

            entity.ToTable("material_property");

            entity.HasIndex(e => new { e.Uid, e.PropertyId }, "material_property_uid_property_id_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PropertyId).HasColumnName("property_id");
            entity.Property(e => e.Uid).HasColumnName("uid");

            entity.HasOne(d => d.Property).WithMany(p => p.MaterialProperties)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("material_property_property_fk");

            entity.HasOne(d => d.UidNavigation).WithMany(p => p.MaterialProperties)
                .HasForeignKey(d => d.Uid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("material_property_uid_fk");
        });

        modelBuilder.Entity<MaterialSource>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("material_source_pk");

            entity.ToTable("material_source");

            entity.HasIndex(e => new { e.MaterialId, e.SourceId, e.TargetMarket, e.UnitId, e.DeliveryTypeId, e.MaterialGroupId }, "material_source_material_id_source_id_target_market_unit_de_key").IsUnique();

            entity.HasIndex(e => e.Uid, "material_source_uid_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DeliveryTypeId).HasColumnName("delivery_type_id");
            entity.Property(e => e.Description)
                .HasColumnType("character varying")
                .HasColumnName("description");
            entity.Property(e => e.MaterialGroupId).HasColumnName("material_group_id");
            entity.Property(e => e.MaterialId).HasColumnName("material_id");
            entity.Property(e => e.RoundTo).HasColumnName("round_to");
            entity.Property(e => e.SourceId).HasColumnName("source_id");
            entity.Property(e => e.TargetMarket)
                .HasColumnType("character varying")
                .HasColumnName("target_market");
            entity.Property(e => e.Uid).HasColumnName("uid");
            entity.Property(e => e.UnitId).HasColumnName("unit_id");

            entity.HasOne(d => d.DeliveryType).WithMany(p => p.MaterialSources)
                .HasForeignKey(d => d.DeliveryTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("material_source_delivery_type_id_fk");

            entity.HasOne(d => d.MaterialGroup).WithMany(p => p.MaterialSources)
                .HasForeignKey(d => d.MaterialGroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("material_source_material_group_id_fk");

            entity.HasOne(d => d.Material).WithMany(p => p.MaterialSources)
                .HasForeignKey(d => d.MaterialId)
                .HasConstraintName("material_source_material_fk");

            entity.HasOne(d => d.Source).WithMany(p => p.MaterialSources)
                .HasForeignKey(d => d.SourceId)
                .HasConstraintName("material_source_source_fk");

            entity.HasOne(d => d.Unit).WithMany(p => p.MaterialSources)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("material_source_unit_id_fk");
        });

        modelBuilder.Entity<MaterialValue>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("material_value_pk");

            entity.ToTable("material_value");

            entity.HasIndex(e => new { e.Uid, e.PropertyId, e.CreatedOn }, "material_value_all_together_uindex").IsUnique();

            entity.HasIndex(e => e.Id, "material_value_id_uindex").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedOn).HasColumnName("created_on");
            entity.Property(e => e.LastUpdated)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("last_updated");
            entity.Property(e => e.PropertyId).HasColumnName("property_id");
            entity.Property(e => e.Uid).HasColumnName("uid");
            entity.Property(e => e.ValueDecimal).HasColumnName("value_decimal");
            entity.Property(e => e.ValueStr)
                .HasColumnType("character varying")
                .HasColumnName("value_str");

            entity.HasOne(d => d.Property).WithMany(p => p.MaterialValues)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("material_value_property_fk");

            entity.HasOne(d => d.UidNavigation).WithMany(p => p.MaterialValues)
                .HasForeignKey(d => d.Uid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("material_value_material_source_fk");
        });

        modelBuilder.Entity<Property>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("property_pk");

            entity.ToTable("property");

            entity.HasIndex(e => e.Name, "property_name_uindex").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Kind)
                .HasColumnType("character varying")
                .HasColumnName("kind");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
        });

        modelBuilder.Entity<SchemaMigration>(entity =>
        {
            entity.HasKey(e => e.Version).HasName("schema_migrations_pkey");

            entity.ToTable("schema_migrations");

            entity.Property(e => e.Version)
                .ValueGeneratedNever()
                .HasColumnName("version");
            entity.Property(e => e.Dirty).HasColumnName("dirty");
        });

        modelBuilder.Entity<Source>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("source_pk");

            entity.ToTable("source");

            entity.HasIndex(e => e.Name, "source_name_uindex").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Kind)
                .HasColumnType("character varying")
                .HasColumnName("kind");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
            entity.Property(e => e.Url)
                .HasColumnType("character varying")
                .HasColumnName("url");
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("unit_pk");

            entity.ToTable("unit");

            entity.HasIndex(e => e.Name, "unit_name_unique").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_pk");

            entity.ToTable("user");

            entity.HasIndex(e => e.Username, "user_username_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Password)
                .HasColumnType("character varying")
                .HasColumnName("password");
            entity.Property(e => e.Username)
                .HasColumnType("character varying")
                .HasColumnName("username");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
