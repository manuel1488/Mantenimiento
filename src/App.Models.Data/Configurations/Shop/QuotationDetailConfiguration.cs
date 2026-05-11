using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Models.Shop;

namespace App.Models.Data.Configurations.Shop;

public class QuotationDetailConfiguration : IEntityTypeConfiguration<QuotationDetail>
{
    public void Configure(EntityTypeBuilder<QuotationDetail> builder)
    {
        // Indexes
        builder.HasIndex(e => e.QuotationId);
        builder.HasIndex(e => e.ProductId);

        // Properties
        builder.Property(e => e.DiscountPercentage)
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(0);

        builder.Property(e => e.DiscountAmount)
            .HasColumnType("decimal(18,6)")
            .HasDefaultValue(0);

        builder.Property(e => e.TaxRate)
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(0);

        builder.Property(e => e.TaxAmount)
            .HasColumnType("decimal(10,2)")
            .HasDefaultValue(0);

        // Relationships
        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
