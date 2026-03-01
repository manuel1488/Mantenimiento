using App.Models.Shop;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shop;

public class AdjustmentEntryConfiguration : IEntityTypeConfiguration<AdjustmentEntry>
{
    public void Configure(EntityTypeBuilder<AdjustmentEntry> builder)
    {
        builder.Property(e => e.AdjustmentType)
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

        builder.HasMany(e => e.Items)
            .WithOne(e => e.AdjustmentEntry)
            .HasForeignKey(e => e.AdjustmentEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.LocationId, e.AdjustmentDate });
    }
}
