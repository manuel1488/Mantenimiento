using App.Models.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shop;

public class WholesaleTierConfiguration : IEntityTypeConfiguration<WholesaleTier>
{
    public void Configure(EntityTypeBuilder<WholesaleTier> builder)
    {
        builder.HasIndex(e => e.Name)
            .IsUnique()
            .HasFilter("IsDeleted = 0");

        builder.HasIndex(e => new { e.IsActive, e.IsDeleted });

        // Seed wholesale tiers
        var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            new WholesaleTier
            {
                Id = 1,
                Name = "Medio Mayoreo",
                DisplayOrder = 1,
                IsActive = true,
                CreatedBy = "System",
                CreatedAt = seedDate,
                IsDeleted = 0
            },
            new WholesaleTier
            {
                Id = 2,
                Name = "Mayoreo",
                DisplayOrder = 2,
                IsActive = true,
                CreatedBy = "System",
                CreatedAt = seedDate,
                IsDeleted = 0
            }
        );
    }
}
