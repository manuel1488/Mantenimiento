using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Models.Settings;

namespace App.Models.Data.Configurations.Settings;

public class CompanySettingsConfiguration : IEntityTypeConfiguration<CompanySettings>
{
    public void Configure(EntityTypeBuilder<CompanySettings> builder)
    {
        builder.Property(e => e.TimeZoneId)
            .HasMaxLength(50)
            .IsRequired()
            .IsUnicode(false);

        builder.HasIndex(e => e.Id)
            .IsUnique();
    }
}