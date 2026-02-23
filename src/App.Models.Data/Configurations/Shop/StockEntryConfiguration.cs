using App.Models.Shop;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shop;

public class StockEntryConfiguration : IEntityTypeConfiguration<StockEntry>
{
    public void Configure(EntityTypeBuilder<StockEntry> builder)
    {
        builder.Property(e => e.MovementType)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.MovementSubType)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.DocumentNumber)
            .HasMaxLength(100);

        builder.Property(e => e.Reference)
            .HasMaxLength(50);

        builder.Property(e => e.SupplierName)
            .HasMaxLength(100);

        builder.Property(e => e.AttachmentFileName)
            .HasMaxLength(255);

        builder.Property(e => e.AttachmentMimeType)
            .HasMaxLength(100);

        builder.Property(e => e.AttachmentData)
            .HasColumnType("LONGBLOB");

        builder.HasOne(e => e.Location)
            .WithMany()
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Supplier)
            .WithMany()
            .HasForeignKey(e => e.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Items)
            .WithOne(e => e.StockEntry)
            .HasForeignKey(e => e.StockEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.LocationId, e.EntryDate });
        builder.HasIndex(e => e.DocumentNumber);
    }
}
