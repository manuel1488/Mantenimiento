using App.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Settings;

public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.HasIndex(e => e.Code).IsUnique();
        
        builder.Property(e => e.Code)
            .IsUnicode(false);

        builder.Property(e => e.Symbol)
            .IsUnicode(false);
    }
}