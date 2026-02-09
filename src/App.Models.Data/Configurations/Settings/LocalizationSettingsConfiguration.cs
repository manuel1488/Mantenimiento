using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Models.Settings;

namespace App.Models.Data.Configurations.Settings;

public class LocalizationSettingsConfiguration : IEntityTypeConfiguration<LocalizationSettings>
{
    public void Configure(EntityTypeBuilder<LocalizationSettings> builder)
    {
        // Asegurar que DefaultLanguage siga el formato ISO (xx o xx-XX)
        builder.Property(e => e.DefaultLanguage)
            .HasMaxLength(5)
            .IsRequired()
            .IsUnicode(false);  // Solo caracteres ASCII
            
        // Asegurar que DefaultTimeZone sea un valor IANA válido
        builder.Property(e => e.DefaultTimeZone)
            .HasMaxLength(20)
            .IsRequired()
            .IsUnicode(false);  // Solo caracteres ASCII

        // Validar formatos numéricos y de fecha
        builder.Property(e => e.NumberFormat)
            .HasMaxLength(20)
            .IsRequired()
            .IsUnicode(false);

        builder.Property(e => e.DateFormat)
            .HasMaxLength(20)
            .IsRequired()
            .IsUnicode(false);

        builder.Property(e => e.TimeFormat)
            .HasMaxLength(20)
            .IsRequired()
            .IsUnicode(false);

        // Solo debe existir una configuración de localización activa
        builder.HasIndex(e => e.Id)
            .IsUnique();
    }
}