using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using App.Models.Cotizaciones;

namespace App.Models.Data.Configurations.Cotizaciones;

public class CotizacionFirmaConfiguration : IEntityTypeConfiguration<CotizacionFirma>
{
    public void Configure(EntityTypeBuilder<CotizacionFirma> builder)
    {
        builder.Property(f => f.FirmanteNombre).HasMaxLength(150).IsRequired();
        builder.Property(f => f.FileKey).HasMaxLength(1000).IsRequired();
        builder.Property(f => f.ContentType).HasMaxLength(100).IsRequired();

        builder.HasIndex(f => f.CotizacionId).IsUnique();

        builder.HasOne(f => f.Cotizacion)
            .WithOne(c => c.Firma)
            .HasForeignKey<CotizacionFirma>(f => f.CotizacionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
