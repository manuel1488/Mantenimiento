using App.Models.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shop;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.HasIndex(e => e.Name);
        
        // Ensure only one active public sales warehouse
        builder.HasIndex(e => new { e.IsPublicSalesWarehouse, e.IsActive, e.IsDeleted })
            .HasFilter("IsPublicSalesWarehouse = 1 AND IsActive = 1 AND IsDeleted = 0")
            .IsUnique();
            
        // Default value for the new flag
        builder.Property(e => e.IsPublicSalesWarehouse)
            .HasDefaultValue(false);
    }
}