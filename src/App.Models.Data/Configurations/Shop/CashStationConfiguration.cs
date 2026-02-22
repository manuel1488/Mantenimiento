using App.Models.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shop;

public class CashStationConfiguration : IEntityTypeConfiguration<CashStation>
{
    public void Configure(EntityTypeBuilder<CashStation> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.ModifiedBy)
            .HasMaxLength(256);

        builder.HasIndex(e => new { e.LocationId, e.Name })
            .IsUnique();

        builder.HasOne(e => e.Location)
            .WithMany(l => l.CashStations)
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
