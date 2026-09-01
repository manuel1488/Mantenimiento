using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Models.Settings;

namespace App.Models.Data.Configurations.Settings;

public class TelegramSettingsConfiguration : IEntityTypeConfiguration<TelegramSettings>
{
    public void Configure(EntityTypeBuilder<TelegramSettings> builder)
    {
        // Solo debe existir una configuración de Telegram activa
        builder.HasIndex(e => e.Id)
            .IsUnique();
    }
}
