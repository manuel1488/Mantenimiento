using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Models.Settings;

namespace App.Models.Data.Configurations.Settings;

public class EmailSettingsConfiguration : IEntityTypeConfiguration<EmailSettings>
{
    public void Configure(EntityTypeBuilder<EmailSettings> builder)
    {
        
        // Configuración del host SMTP
        builder.Property(e => e.SmtpHost)
            .HasMaxLength(100)
            .IsUnicode(false);  // Solo caracteres ASCII para hosts

        // Configuración del puerto SMTP
        builder.Property(e => e.SmtpPort)
            .HasDefaultValue(587);  // Puerto común para SMTP con TLS

        // Configuración del usuario SMTP
        builder.Property(e => e.SmtpUser)
            .HasMaxLength(100)
            .IsUnicode(false);

        // Configuración de la contraseña SMTP (encriptada)
        builder.Property(e => e.SmtpPassword)
            .HasMaxLength(100)
            .IsRequired(false);

        // Configuración del email del remitente
        builder.Property(e => e.FromEmail)
            .HasMaxLength(100)
            .IsUnicode(false);

        // Configuración del nombre del remitente
        builder.Property(e => e.FromName)
            .HasMaxLength(100);

        // SSL habilitado por defecto para seguridad
        builder.Property(e => e.UseSsl)
            .HasDefaultValue(true);

        // Solo debe existir una configuración de email activa
        builder.HasIndex(e => e.Id)
            .IsUnique();

        // Asegurar que si hay host, debe haber puerto y viceversa
          builder.ToTable(tb => tb.HasCheckConstraint(
            "CK_EmailSettings_SmtpConfig",
            "(SmtpHost IS NULL AND SmtpPort IS NULL) OR (SmtpHost IS NOT NULL AND SmtpPort IS NOT NULL)"));
    }
}