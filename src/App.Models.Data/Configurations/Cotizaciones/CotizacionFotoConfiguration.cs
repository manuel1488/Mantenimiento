using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using App.Models.Cotizaciones;

namespace App.Models.Data.Configurations.Cotizaciones;

public class CotizacionFotoConfiguration : IEntityTypeConfiguration<CotizacionFoto>
{
    public void Configure(EntityTypeBuilder<CotizacionFoto> builder)
    {
        builder.Property(f => f.FileKey).HasMaxLength(1000).IsRequired();
        builder.Property(f => f.ThumbnailFileKey).HasMaxLength(1000);
        builder.Property(f => f.MimeType).HasMaxLength(100).IsRequired();

        builder.HasOne(f => f.Cotizacion)
            .WithMany(c => c.Fotos)
            .HasForeignKey(f => f.CotizacionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
