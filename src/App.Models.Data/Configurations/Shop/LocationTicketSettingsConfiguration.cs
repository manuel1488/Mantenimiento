using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Models.Shop;

namespace App.Models.Data.Configurations.Shop;

public class LocationTicketSettingsConfiguration : IEntityTypeConfiguration<LocationTicketSettings>
{
    public void Configure(EntityTypeBuilder<LocationTicketSettings> builder)
    {
        // Unique constraint - one configuration per location
        builder.HasIndex(e => e.LocationId)
            .IsUnique();

        // Properties
        builder.Property(e => e.PrinterName)
            .IsUnicode(false);

        builder.Property(e => e.PaperWidth)
            .HasDefaultValue(80);

        builder.Property(e => e.AutoPrint)
            .HasDefaultValue(false);

        builder.Property(e => e.Copies)
            .HasDefaultValue(1);

        builder.Property(e => e.ShowLogo)
            .HasDefaultValue(true);

        builder.Property(e => e.ShowFullAddress)
            .HasDefaultValue(true);

        builder.Property(e => e.ShowQrCode)
            .HasDefaultValue(false);

        builder.Property(e => e.ShowPrices)
            .HasDefaultValue(true);

        builder.Property(e => e.ShowTaxBreakdown)
            .HasDefaultValue(true);

        // Relationships
        builder.HasOne(e => e.Location)
            .WithMany()
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
