using App.Models.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shop;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        // Indexes
        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => e.Type);
        builder.HasIndex(e => e.PostalCode);
        builder.HasIndex(e => new { e.City, e.State });

        // Properties
        builder.Property(e => e.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.Country)
            .HasDefaultValue("MX")
            .IsUnicode(false);

        builder.Property(e => e.PostalCode)
            .IsUnicode(false);

        // Precision for coordinates
        builder.Property(e => e.Latitude)
            .HasPrecision(10, 8);

        builder.Property(e => e.Longitude)
            .HasPrecision(11, 8);
    }
}
