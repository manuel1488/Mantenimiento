using App.Models.Shop;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shop;

public class StockEntryItemConfiguration : IEntityTypeConfiguration<StockEntryItem>
{
    public void Configure(EntityTypeBuilder<StockEntryItem> builder)
    {
        builder.Property(e => e.Quantity)
            .HasColumnType("decimal(15,6)")
            .IsRequired();

        builder.Property(e => e.UnitCost)
            .HasColumnType("decimal(10,2)");

        builder.HasOne(e => e.StockEntry)
            .WithMany(e => e.Items)
            .HasForeignKey(e => e.StockEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.InventoryMovement)
            .WithMany()
            .HasForeignKey(e => e.InventoryMovementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
