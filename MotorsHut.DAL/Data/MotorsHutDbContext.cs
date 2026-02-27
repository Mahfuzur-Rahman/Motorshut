using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MotorsHut.DAL.Entities;

namespace MotorsHut.DAL.Data;

public sealed class MotorsHutDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public MotorsHutDbContext(DbContextOptions<MotorsHutDbContext> options) : base(options)
    {
    }

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<CarImage> CarImages => Set<CarImage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(p => p.LastName).HasMaxLength(100).IsRequired();
            entity.Property(p => p.IsActive).HasDefaultValue(true);
            entity.Property(p => p.CreatedAtUtc).IsRequired();
        });

        builder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("PasswordResetTokens");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.TokenHash).HasMaxLength(256).IsRequired();
            entity.Property(p => p.ExpiresAtUtc).IsRequired();
            entity.Property(p => p.CreatedAtUtc).IsRequired();

            entity.HasIndex(p => p.TokenHash).IsUnique();
            entity.HasIndex(p => new { p.UserId, p.ExpiresAtUtc });

            entity.HasOne(p => p.User)
                .WithMany(u => u.PasswordResetTokens)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Car>(entity =>
        {
            entity.ToTable("Cars");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Make).HasMaxLength(100).IsRequired();
            entity.Property(c => c.Model).HasMaxLength(100).IsRequired();
            entity.Property(c => c.Variant).HasMaxLength(100);
            entity.Property(c => c.Color).HasMaxLength(50);
            entity.Property(c => c.Vin).HasMaxLength(50);
            entity.Property(c => c.Price).HasPrecision(18, 2);
            entity.Property(c => c.CreatedAtUtc).IsRequired();
            entity.Property(c => c.UpdatedAtUtc).IsRequired();
            entity.HasIndex(c => new { c.Make, c.Model });

            entity.HasMany(c => c.Images)
                .WithOne(i => i.Car)
                .HasForeignKey(i => i.CarId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CarImage>(entity =>
        {
            entity.ToTable("CarImages");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.ImageUrl).HasMaxLength(500).IsRequired();
            entity.Property(i => i.SortOrder).IsRequired();
            entity.Property(i => i.CreatedAtUtc).IsRequired();
            entity.HasIndex(i => i.CarId);
            entity.HasIndex(i => new { i.CarId, i.IsPrimary });
            entity.HasIndex(i => new { i.CarId, i.SortOrder });
        });
    }
}
