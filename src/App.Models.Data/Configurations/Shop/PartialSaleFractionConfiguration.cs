using App.Models.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shop;

public class PartialSaleFractionConfiguration : IEntityTypeConfiguration<PartialSaleFraction>
{
    public void Configure(EntityTypeBuilder<PartialSaleFraction> builder)
    {
        builder.HasIndex(e => e.Code)
            .IsUnique();

        builder.HasIndex(e => new { e.IsActive, e.IsDeleted });

        builder.Property(e => e.FractionValue)
            .HasColumnType("decimal(10,6)");

        // Seed standard fractions
        var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            new PartialSaleFraction
            {
                Id = 1,
                Code = "1/2",
                Name = "Mitad",
                Numerator = 1,
                Denominator = 2,
                FractionValue = 0.5m,
                DisplayOrder = 1,
                IsActive = true,
                CreatedBy = "System",
                CreatedAt = seedDate,
                IsDeleted = 0
            },
            new PartialSaleFraction
            {
                Id = 2,
                Code = "1/4",
                Name = "Cuarto",
                Numerator = 1,
                Denominator = 4,
                FractionValue = 0.25m,
                DisplayOrder = 2,
                IsActive = true,
                CreatedBy = "System",
                CreatedAt = seedDate,
                IsDeleted = 0
            },
            new PartialSaleFraction
            {
                Id = 3,
                Code = "1/8",
                Name = "Octavo",
                Numerator = 1,
                Denominator = 8,
                FractionValue = 0.125m,
                DisplayOrder = 3,
                IsActive = true,
                CreatedBy = "System",
                CreatedAt = seedDate,
                IsDeleted = 0
            }
        );
    }
}
