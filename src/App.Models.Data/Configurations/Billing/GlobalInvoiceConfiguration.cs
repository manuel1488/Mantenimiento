using App.Models.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Billing;

public class GlobalInvoiceConfiguration : IEntityTypeConfiguration<GlobalInvoice>
{
    public void Configure(EntityTypeBuilder<GlobalInvoice> builder)
    {
        builder.HasIndex(e => e.Uuid).IsUnique().HasFilter("Uuid IS NOT NULL");
        builder.HasIndex(e => new { e.Serie, e.Folio }).IsUnique();
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.StartDate, e.EndDate });

        builder.Property(e => e.Serie).IsUnicode(false);
        builder.Property(e => e.Uuid).IsUnicode(false);
        builder.Property(e => e.PaymentForm).IsUnicode(false);
        builder.Property(e => e.PeriodMonth).IsUnicode(false);
        builder.Property(e => e.IssuerRfc).IsUnicode(false);
        builder.Property(e => e.IssuerFiscalRegime).IsUnicode(false);
        builder.Property(e => e.IssuerPostalCode).IsUnicode(false);
        builder.Property(e => e.CancellationReason).IsUnicode(false);
        builder.Property(e => e.NoCertificadoSat).IsUnicode(false);
        builder.Property(e => e.NoCertificadoCfdi).IsUnicode(false);

        builder.HasMany(e => e.GlobalInvoiceSales)
            .WithOne(e => e.GlobalInvoice)
            .HasForeignKey(e => e.GlobalInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class GlobalInvoiceSaleConfiguration : IEntityTypeConfiguration<GlobalInvoiceSale>
{
    public void Configure(EntityTypeBuilder<GlobalInvoiceSale> builder)
    {
        builder.HasKey(e => new { e.GlobalInvoiceId, e.SaleId });

        builder.HasOne(e => e.Sale)
            .WithMany()
            .HasForeignKey(e => e.SaleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
