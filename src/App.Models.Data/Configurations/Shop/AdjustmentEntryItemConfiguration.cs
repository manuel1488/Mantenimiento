using App.Models.Shop;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shop;

public class AdjustmentEntryItemConfiguration : IEntityTypeConfiguration<AdjustmentEntryItem>
{
    public void Configure(EntityTypeBuilder<AdjustmentEntryItem> builder)
    {
        builder.Property(e => e.NewQuantity)
            .HasColumnType("decimal(15,6)")
            .IsRequired();

        builder.Property(e => e.PreviousQuantity)
            .HasColumnType("decimal(15,6)")
            .IsRequired();

        builder.HasOne(e => e.AdjustmentEntry)
            .WithMany(e => e.Items)
            .HasForeignKey(e => e.AdjustmentEntryId)
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
