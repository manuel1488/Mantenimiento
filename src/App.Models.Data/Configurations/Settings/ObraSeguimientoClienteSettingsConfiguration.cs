using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Models.Settings;

namespace App.Models.Data.Configurations.Settings;

public class ObraSeguimientoClienteSettingsConfiguration : IEntityTypeConfiguration<ObraSeguimientoClienteSettings>
{
    public void Configure(EntityTypeBuilder<ObraSeguimientoClienteSettings> builder)
    {
        // Solo debe existir una configuración de vigencia del enlace de Cliente activa
        builder.HasIndex(e => e.Id)
            .IsUnique();
    }
}
