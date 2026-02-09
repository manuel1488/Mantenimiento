using App.Models.Shop;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shop;


public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasIndex(e => e.Code);

        builder.HasMany(e => e.Images)
            .WithOne(e => e.Product)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

         builder.HasOne(e => e.MexicoProductService)
            .WithMany()
            .HasForeignKey(e => e.MexicoProductServiceId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.Content)
            .HasColumnType("decimal(10,3)")
            .HasDefaultValue(1);

        builder.Property(e => e.IsPartialSaleAllowed)
            .HasDefaultValue(false);
    }
}