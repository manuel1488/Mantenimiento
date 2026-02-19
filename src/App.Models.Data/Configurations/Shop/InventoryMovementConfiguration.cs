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
            "CK_InventoryMovement_DifferentLocations",
            "(`MovementType` != 'TRANSFER') OR " +
            "(`MovementType` = 'TRANSFER' AND `LocationId` != `DestinationLocationId`)"));

        // Resctriction for valiadate positive quantity
         builder.ToTable(t => t.HasCheckConstraint(
            "CK_InventoryMovement_PositiveQuantity",
            "`Quantity` > 0"));

        // Resctriction for valiadate valid balance
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_InventoryMovement_ValidBalance",
            "`NewBalance` >= 0"));

        // Index for performance
        builder.HasIndex(e => new { e.ProductId, e.LocationId, e.MovementDate });

        // Relations
        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relations
        builder.HasOne(e => e.Location)
            .WithMany()
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relations
        builder.HasOne(e => e.DestinationLocation)
            .WithMany()
            .HasForeignKey(e => e.DestinationLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}