using App.Models.Shop;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shop;



public class SaleDetailConfiguration : IEntityTypeConfiguration<SaleDetail>
{
    public void Configure(EntityTypeBuilder<SaleDetail> builder)
    {
        builder.HasIndex(e => new { e.SaleId, e.ProductId });

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PartialSaleFraction)
            .WithMany()
            .HasForeignKey(e => e.PartialSaleFractionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.SurchargePercentage)
            .HasColumnType("decimal(5,2)");

        builder.Property(e => e.SurchargeAmount)
            .HasColumnType("decimal(10,2)");

        builder.Property(e => e.BasePriceBeforeSurcharge)
            .HasColumnType("decimal(10,6)");
    }
}