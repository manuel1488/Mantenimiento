using App.Models.Shop;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shop;

public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement> 
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        // Primary Key
        builder.Property(e => e.MovementType)
            .HasMaxLength(20)
            .IsRequired();
        
        // Tocken for optimistic concurrency
        builder.Property(e => e.Quantity)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        // Reason max length 500
        builder.Property(e => e.Reason)
            .HasMaxLength(500)
            .IsRequired();

        // Resctriction for valid origin and destination be different for transfer
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_InventoryMovement_DifferentWarehouses",
            "(`MovementType` != 'TRANSFER') OR " +
            "(`MovementType` = 'TRANSFER' AND `WarehouseId` != `DestinationWarehouseId`)"));

        // Resctriction for valiadate positive quantity
         builder.ToTable(t => t.HasCheckConstraint(
            "CK_InventoryMovement_PositiveQuantity",
            "`Quantity` > 0"));

        // Resctriction for valiadate valid balance
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_InventoryMovement_ValidBalance",
            "`NewBalance` >= 0"));

        // Index for performance
        builder.HasIndex(e => new { e.ProductId, e.WarehouseId, e.MovementDate });

        // Relations
        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relations
        builder.HasOne(e => e.Warehouse)
            .WithMany()
            .HasForeignKey(e => e.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relations
        builder.HasOne(e => e.DestinationWarehouse)
            .WithMany()
            .HasForeignKey(e => e.DestinationWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}