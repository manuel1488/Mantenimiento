using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Models.Shop;

namespace App.Models.Data.Configurations.Shop;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        // Indexes
        builder.HasIndex(e => e.CustomerId);
        builder.HasIndex(e => e.SaleDate);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.SaleType);

        // Properties
        builder.Property(e => e.Status)
            .IsUnicode(false);

        builder.Property(e => e.PaymentMethod)
            .IsUnicode(false);

        builder.Property(e => e.DiscountPercentage)
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(0);

        builder.Property(e => e.DiscountAmount)
            .HasColumnType("decimal(10,2)")
            .HasDefaultValue(0);

        // Relationships
        builder.HasMany(e => e.Details)
            .WithOne(e => e.Sale)
            .HasForeignKey(e => e.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Customer)
            .WithMany()
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Check constraints
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Sale_DiscountRange",
            "DiscountPercentage >= 0 AND DiscountPercentage <= 100"));
    }
}
