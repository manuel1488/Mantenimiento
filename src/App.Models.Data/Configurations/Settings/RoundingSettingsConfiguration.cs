using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Models.Settings;

namespace App.Models.Data.Configurations.Settings;

public class RoundingSettingsConfiguration : IEntityTypeConfiguration<RoundingSettings>
{
    public void Configure(EntityTypeBuilder<RoundingSettings> builder)
    {
        builder.HasIndex(e => e.Id)
            .IsUnique();

        builder.Property(e => e.Method)
            .HasConversion<int>();

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_RoundingSettings_ValidDecimalPlaces",
            "DecimalPlaces >= 0 AND DecimalPlaces <= 2"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_RoundingSettings_ValidThreshold",
            "MinimumThreshold >= 0"));
    }
}
