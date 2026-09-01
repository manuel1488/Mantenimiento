using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Models.Settings;

namespace App.Models.Data.Configurations.Settings;

public class ObraFolioSettingsConfiguration : IEntityTypeConfiguration<ObraFolioSettings>
{
    public void Configure(EntityTypeBuilder<ObraFolioSettings> builder)
    {
        // Solo debe existir una configuración de folio de Obra activa
        builder.HasIndex(e => e.Id)
            .IsUnique();
    }
}
