using App.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Settings;

public class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.HasIndex(e => e.Code).IsUnique();
        
        builder.Property(e => e.Code)
            .IsUnicode(false);

        builder.Property(e => e.DefaultCurrencyCode)
            .IsUnicode(false);
    }
}