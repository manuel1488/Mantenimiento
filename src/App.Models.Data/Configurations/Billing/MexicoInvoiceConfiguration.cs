using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Billing;

public class MexicoInvoiceConfiguration : IEntityTypeConfiguration<MexicoInvoice>
{
    public void Configure(EntityTypeBuilder<MexicoInvoice> builder)
    {
        builder.HasIndex(e => e.SaleId).IsUnique();
        builder.HasIndex(e => e.Uuid).IsUnique().HasFilter("Uuid IS NOT NULL");
        builder.HasIndex(e => new { e.Serie, e.Folio }).IsUnique();
        builder.HasIndex(e => e.IsStamped);
        builder.HasIndex(e => e.Status);

        builder.Property(e => e.Serie)
            .IsUnicode(false);

        builder.Property(e => e.Uuid)
            .IsUnicode(false);

        builder.Property(e => e.CustomerRfc)
            .IsUnicode(false);

        builder.Property(e => e.CustomerPostalCode)
            .IsUnicode(false);

        builder.Property(e => e.CustomerFiscalRegime)
            .IsUnicode(false);

        builder.Property(e => e.IssuerRfc)
            .IsUnicode(false);

        builder.Property(e => e.IssuerFiscalRegime)
            .IsUnicode(false);

        builder.Property(e => e.IssuerPostalCode)
            .IsUnicode(false);

        builder.Property(e => e.PaymentForm)
            .IsUnicode(false);

        builder.Property(e => e.PaymentMethod)
            .IsUnicode(false);

        builder.Property(e => e.CfdiUse)
            .IsUnicode(false);

        builder.Property(e => e.Currency)
            .IsUnicode(false)
            .HasDefaultValue("MXN");

        builder.Property(e => e.ExchangeRate)
            .HasDefaultValue(1m);

        builder.Property(e => e.Status)
            .IsUnicode(false)
            .HasDefaultValue("Draft");

        builder.Property(e => e.IsStamped)
            .HasDefaultValue(false);

        builder.Property(e => e.NoCertificadoSat)
            .IsUnicode(false);

        builder.Property(e => e.NoCertificadoCfdi)
            .IsUnicode(false);

        builder.HasOne(e => e.Sale)
            .WithMany()
            .HasForeignKey(e => e.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Files)
            .WithOne(e => e.Invoice)
            .HasForeignKey(e => e.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
