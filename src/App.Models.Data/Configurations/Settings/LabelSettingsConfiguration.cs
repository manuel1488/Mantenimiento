using App.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Settings;

public class LabelSettingsConfiguration : IEntityTypeConfiguration<LabelSettings>
{
    public void Configure(EntityTypeBuilder<LabelSettings> builder)
    {
        builder.HasIndex(e => e.Id).IsUnique();
    }
}
