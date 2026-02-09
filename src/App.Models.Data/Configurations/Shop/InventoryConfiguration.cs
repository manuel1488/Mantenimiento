using App.Models.Shop;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shop;


public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        // Primary Key
        builder.HasIndex(e => new { e.ProductId, e.WarehouseId }).IsUnique();

        // Tocken for optimistic concurrency
        builder.Property(e => e.Version)
            .IsRowVersion()
            .HasDefaultValueSql("('')")
            .HasColumnType("binary(8)")
            .IsConcurrencyToken()
            .ValueGeneratedOnAddOrUpdate();

        // Quantity
        builder.Property(e => e.Quantity)
            .HasColumnType("decimal(10,2)")
            .HasDefaultValue(0);

        // Resctriction for valiadate positive quantity
        builder.ToTable(t => t.HasCheckConstraint("CK_Inventory_Quantity", "`Quantity` >= 0"));

        // Index for performance
        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relations
        builder.HasOne(e => e.Warehouse)
            .WithMany()
            .HasForeignKey(e => e.WarehouseId) 
            .OnDelete(DeleteBehavior.Restrict);
    }
}