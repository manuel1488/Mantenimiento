using App.Models.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shop;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.HasIndex(e => e.Name);

        builder.Property(e => e.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(e => e.Type);
    }
}
