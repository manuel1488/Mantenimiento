using App.Models.Settings;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Settings;

public class TaxRateConfiguration : IEntityTypeConfiguration<TaxRate>
{
    public void Configure(EntityTypeBuilder<TaxRate> builder)
    {
        // Unique index for tax code by country
        builder.HasIndex(e => new { e.CountryCode, e.Code, e.IsDeleted })
            .HasFilter("IsDeleted = 0")
            .IsUnique();

        // Only one active default tax rate per country
        builder.HasIndex(e => new { e.CountryCode, e.IsDefault, e.IsActive, e.IsDeleted })
            .HasFilter("IdDeleted = 0")
            .HasFilter("IsDefault = 1 AND IsActive = 1")
            .IsUnique();

        // For Canada: Index for searching by province
        builder.HasIndex(e => new { e.CountryCode, e.ProvinceCode, e.IsActive });

        // Field Code should not allow Unicode
        builder.Property(e => e.Code)
            .IsUnicode(false);

        // Field CountryCode should not allow Unicode
        builder.Property(e => e.CountryCode)
            .IsUnicode(false);

        // Field ProvinceCode should not allow Unicode
        builder.Property(e => e.ProvinceCode)
            .IsUnicode(false);

        // Validations for positive rates
        builder.Property(e => e.Rate)
            .HasColumnType("decimal(10,6)");

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_TaxRate_Rate",
            "Rate >= 0 AND Rate <= 1"));

        // Validations for effective dates
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_TaxRate_EffectiveDates",
            "EffectiveTo IS NULL OR EffectiveFrom < EffectiveTo"));
    }
}