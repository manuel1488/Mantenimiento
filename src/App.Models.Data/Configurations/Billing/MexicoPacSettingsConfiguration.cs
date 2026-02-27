using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Billing;

public class MexicoPacSettingsConfiguration : IEntityTypeConfiguration<MexicoPacSettings>
{
    public void Configure(EntityTypeBuilder<MexicoPacSettings> builder)
    {
        builder.Property(e => e.IssuerRfc)
            .IsUnicode(false);

        builder.Property(e => e.IssuerFiscalRegime)
            .IsUnicode(false);

        builder.Property(e => e.IssuerPostalCode)
            .IsUnicode(false);

        builder.Property(e => e.InvoiceSerie)
            .IsUnicode(false)
            .HasDefaultValue("A");

        builder.Property(e => e.StartFolio)
            .HasDefaultValue(1L);

        builder.Property(e => e.FolioLength)
            .HasDefaultValue(0);

        builder.Property(e => e.IsProduction)
            .HasDefaultValue(false);
    }
}
