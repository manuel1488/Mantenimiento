using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Models.Settings;

namespace App.Models.Data.Configurations.Settings;

public class CompanySettingsConfiguration : IEntityTypeConfiguration<CompanySettings>
{
    public void Configure(EntityTypeBuilder<CompanySettings> builder)
    {
        builder.Property(e => e.TimeZoneId)
            .HasMaxLength(100)
            .IsRequired()
            .IsUnicode(false);

        builder.Property(e => e.TimeZoneDisplayName)
            .HasMaxLength(200);

        builder.HasIndex(e => e.Id)
            .IsUnique();
    }
}