using App.Models.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shared;

public class CustomerFiscalProfileConfiguration : IEntityTypeConfiguration<CustomerFiscalProfile>
{
    public void Configure(EntityTypeBuilder<CustomerFiscalProfile> builder)
    {
        // 1-to-1: each customer has at most one fiscal profile
        builder.HasIndex(e => e.CustomerId)
            .IsUnique();

        // TaxId must be unique per country — enforced in service layer
        // (cross-table unique constraints require denormalization; service validates instead)
        builder.HasIndex(e => e.TaxId);

        builder.Property(e => e.TaxId)
            .IsUnicode(false);

        builder.Property(e => e.PostalCode)
            .IsUnicode(false);

        builder.Property(e => e.FiscalRegime)
            .IsUnicode(false);

        builder.Property(e => e.DefaultCfdiUse)
            .IsUnicode(false);

        builder.Property(e => e.CaGstNumber)
            .IsUnicode(false);

        builder.Property(e => e.CaPstNumber)
            .IsUnicode(false);

        builder.Property(e => e.CaHstNumber)
            .IsUnicode(false);

        builder.Property(e => e.CaQstNumber)
            .IsUnicode(false);

        builder.Property(e => e.AutoInvoice)
            .HasDefaultValue(false);

        builder.Property(e => e.SendInvoiceEmail)
            .HasDefaultValue(false);
    }
}
