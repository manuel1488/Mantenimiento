using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Models.Shop;

namespace App.Models.Data.Configurations.Shop;

public class QuotationConfiguration : IEntityTypeConfiguration<Quotation>
{
    public void Configure(EntityTypeBuilder<Quotation> builder)
    {
        // Indexes
        builder.HasIndex(e => e.CustomerId);
        builder.HasIndex(e => e.QuoteDate);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.QuotationNumber).IsUnique();

        // Properties
        builder.Property(e => e.DiscountPercentage)
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(0);

        builder.Property(e => e.DiscountAmount)
            .HasColumnType("decimal(10,2)")
            .HasDefaultValue(0);

        // Relationships
        builder.HasMany(e => e.Details)
            .WithOne(e => e.Quotation)
            .HasForeignKey(e => e.QuotationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Customer)
            .WithMany()
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Check constraints
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Quotation_DiscountRange",
            "DiscountPercentage >= 0 AND DiscountPercentage <= 100"));
    }
}
