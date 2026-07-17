using App.Models.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shop;

public class ProductWholesalePriceConfiguration : IEntityTypeConfiguration<ProductWholesalePrice>
{
    public void Configure(EntityTypeBuilder<ProductWholesalePrice> builder)
    {
        builder.HasIndex(e => new { e.ProductId, e.WholesaleTierId })
            .IsUnique()
            .HasFilter("IsDeleted = 0");

        builder.HasIndex(e => e.ProductId);

        builder.HasOne(e => e.Product)
            .WithMany(p => p.WholesalePrices)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.WholesaleTier)
            .WithMany(t => t.ProductWholesalePrices)
            .HasForeignKey(e => e.WholesaleTierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.MinQuantity)
            .HasColumnType("decimal(10,2)");

        builder.Property(e => e.DiscountPercentage)
            .HasColumnType("decimal(9,6)");

        // Must match sh_products.Price (decimal(10,6)) — a fixed wholesale price can never
        // legitimately exceed the retail price, but the column still needs the same range or
        // MySQL rejects the save outright for any product priced at or above $1,000.
        builder.Property(e => e.FixedPrice)
            .HasColumnType("decimal(10,6)");
    }
}
