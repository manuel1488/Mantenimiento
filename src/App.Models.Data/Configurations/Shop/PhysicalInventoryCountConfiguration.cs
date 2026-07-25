using App.Models.Shop;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shop;

public class PhysicalInventoryCountConfiguration : IEntityTypeConfiguration<PhysicalInventoryCount>
{
    public void Configure(EntityTypeBuilder<PhysicalInventoryCount> builder)
    {
        builder.Property(e => e.BatchNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Reference)
            .HasMaxLength(50);

        builder.HasOne(e => e.Location)
            .WithMany()
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Lines)
            .WithOne(e => e.PhysicalInventoryCount)
            .HasForeignKey(e => e.PhysicalInventoryCountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.LocationId, e.CountDate });
        builder.HasIndex(e => e.BatchId);
    }
}
