using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Billing;


public class MexicoInvoiceConfiguration : IEntityTypeConfiguration<MexicoInvoice>
{
    public void Configure(EntityTypeBuilder<MexicoInvoice> builder)
    {
        builder.HasIndex(e => e.SaleId).IsUnique();

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