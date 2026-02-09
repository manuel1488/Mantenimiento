using App.Models.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shop;

public class ProductPartialSurchargeConfiguration : IEntityTypeConfiguration<ProductPartialSurcharge>
{
    public void Configure(EntityTypeBuilder<ProductPartialSurcharge> builder)
    {
        builder.HasIndex(e => new { e.ProductId, e.PartialSaleFractionId })
            .IsUnique()
            .HasFilter("IsDeleted = 0");

        builder.HasIndex(e => e.ProductId);

        builder.HasOne(e => e.Product)
            .WithMany(p => p.PartialSurcharges)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.PartialSaleFraction)
            .WithMany(f => f.ProductSurcharges)
            .HasForeignKey(e => e.PartialSaleFractionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.SurchargePercentage)
            .HasColumnType("decimal(5,2)");
    }
}
