using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Models.Settings;

namespace App.Models.Data.Configurations.Settings;

public class TaxSettingsConfiguration : IEntityTypeConfiguration<TaxSettings>
{
    public void Configure(EntityTypeBuilder<TaxSettings> builder)
    {
        // Only one record is allowed
        builder.HasIndex(e => e.Id)
            .IsUnique();

        // TaxId must be unique for each country
        builder.HasIndex(e => new { e.CountryCode, e.TaxId })
            .IsUnique();

        // Validate that the country code exists
        builder.HasOne<Country>()
            .WithMany()
            .HasForeignKey(e => e.CountryCode)
            .HasPrincipalKey(e => e.Code)
            .OnDelete(DeleteBehavior.Restrict);

        // Fields that doesn't be unicode
        builder.Property(e => e.CountryCode)
            .IsUnicode(false);
        
        builder.Property(e => e.TaxId)
            .IsUnicode(false);

        builder.Property(e => e.PostalCode)
            .IsUnicode(false);

        // Fields that must be unicode for Mexico settings
        builder.Property(e => e.MxDefaultCfdiUse)
            .IsUnicode(false);
        
        builder.Property(e => e.MxDefaultPaymentMethod)
            .IsUnicode(false);
        
        builder.Property(e => e.MxDefaultPaymentType)
            .IsUnicode(false);

        // Fields that must be unicode for Canada settings
        builder.Property(e => e.CaGstNumber)
            .IsUnicode(false);
        
        builder.Property(e => e.CaPstNumber)
            .IsUnicode(false);
        
        builder.Property(e => e.CaHstNumber)
            .IsUnicode(false);
        
        builder.Property(e => e.CaQstNumber)
            .IsUnicode(false);
    }
}