using App.Models.Shop;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shop;

public class PhysicalInventoryCountLineConfiguration : IEntityTypeConfiguration<PhysicalInventoryCountLine>
{
    public void Configure(EntityTypeBuilder<PhysicalInventoryCountLine> builder)
    {
        builder.Property(e => e.SystemQuantity)
            .HasColumnType("decimal(15,6)")
            .IsRequired();

        builder.Property(e => e.CountedQuantity)
            .HasColumnType("decimal(15,6)")
            .IsRequired();

        builder.Property(e => e.Difference)
            .HasColumnType("decimal(15,6)")
            .IsRequired();

        builder.HasOne(e => e.PhysicalInventoryCount)
            .WithMany(e => e.Lines)
            .HasForeignKey(e => e.PhysicalInventoryCountId)
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
