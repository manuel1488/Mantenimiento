using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using App.Models.Cotizaciones;

namespace App.Models.Data.Configurations.Cotizaciones;

public class CotizacionLineaConfiguration : IEntityTypeConfiguration<CotizacionLinea>
{
    public void Configure(EntityTypeBuilder<CotizacionLinea> builder)
    {
        builder.Property(e => e.ServicioNombre)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(e => e.UnidadMedida)
            .HasMaxLength(20)
            .IsRequired()
            .IsUnicode(false);

        builder.HasOne(e => e.Cotizacion)
            .WithMany(c => c.Lineas)
            .HasForeignKey(e => e.CotizacionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Actividad)
            .WithMany()
            .HasForeignKey(e => e.ActividadId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
