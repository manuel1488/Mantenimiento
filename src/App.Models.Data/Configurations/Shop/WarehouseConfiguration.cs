using App.Models.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shop;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.HasIndex(e => e.Name);

        // Branch relationship (optional)
        builder.HasOne(e => e.Branch)
            .WithMany(b => b.Warehouses)
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.BranchId);
    }
}