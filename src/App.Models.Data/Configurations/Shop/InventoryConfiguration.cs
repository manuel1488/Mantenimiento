using App.Models.Shop;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shop;


public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        // Unique constraint: Product can exist only once per location
        builder.HasIndex(e => new { e.ProductId, e.LocationId })
            .IsUnique();

        // Token for optimistic concurrency
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

        // Check constraint: Positive quantity
        builder.ToTable(t => t.HasCheckConstraint("CK_Inventory_Quantity", "`Quantity` >= 0"));

        // Relations
        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Location)
            .WithMany()
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}