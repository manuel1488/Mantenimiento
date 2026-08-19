using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Models.Settings;

namespace App.Models.Data.Configurations.Settings;

public class CotizacionTemplateSettingsConfiguration : IEntityTypeConfiguration<CotizacionTemplateSettings>
{
    public void Configure(EntityTypeBuilder<CotizacionTemplateSettings> builder)
    {
        builder.HasIndex(e => e.Id)
            .IsUnique();
    }
}
