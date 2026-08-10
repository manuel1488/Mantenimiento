using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using App.Models.Facturas;

namespace App.Models.Data.Configurations.Facturas;

public class FacturaConfiguration : IEntityTypeConfiguration<Factura>
{
    public void Configure(EntityTypeBuilder<Factura> builder)
    {
        builder.Property(e => e.Folio)
            .HasMaxLength(50)
            .IsRequired()
            .IsUnicode(false);

        builder.HasIndex(e => e.Folio)
            .IsUnique();

        builder.HasIndex(e => e.ObraId)
            .IsUnique();
    }
}
