using App.Models.Shop;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shop;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => e.TaxId);

        builder.HasMany(e => e.InventoryMovements)
            .WithOne(e => e.Supplier)
            .HasForeignKey(e => e.SupplierId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
