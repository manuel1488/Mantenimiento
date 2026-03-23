using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Models.Shop;

namespace App.Models.Data.Configurations.Shop;

public class RemissionConfiguration : IEntityTypeConfiguration<Remission>
{
    public void Configure(EntityTypeBuilder<Remission> builder)
    {
        // Indexes
        builder.HasIndex(e => e.RemissionNumber).IsUnique();
        builder.HasIndex(e => e.CustomerId);
        builder.HasIndex(e => e.RemissionDate);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.LocationId);
        builder.HasIndex(e => e.ConsolidatedSaleId);

        // Properties
        builder.Property(e => e.DiscountPercentage)
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(0);

        builder.Property(e => e.DiscountAmount)
            .HasColumnType("decimal(10,2)")
            .HasDefaultValue(0);

        builder.Property(e => e.TaxRate)
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(0);

        // Relationships
        builder.HasMany(e => e.Details)
            .WithOne(e => e.Remission)
            .HasForeignKey(e => e.RemissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Customer)
            .WithMany()
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Location)
            .WithMany()
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ConsolidatedSale)
            .WithMany()
            .HasForeignKey(e => e.ConsolidatedSaleId)
            .OnDelete(DeleteBehavior.SetNull);

        // Check constraints
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Remission_DiscountRange",
            "DiscountPercentage >= 0 AND DiscountPercentage <= 100"));
    }
}
