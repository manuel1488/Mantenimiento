using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using App.Models.Cotizaciones;

namespace App.Models.Data.Configurations.Cotizaciones;

public class CotizacionConfiguration : IEntityTypeConfiguration<Cotizacion>
{
    public void Configure(EntityTypeBuilder<Cotizacion> builder)
    {
        builder.Property(e => e.Estado)
            .HasConversion<int>()
            .IsRequired();

        builder.HasOne(e => e.Obra)
            .WithMany(o => o.Cotizaciones)
            .HasForeignKey(e => e.ObraId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.ObraId, e.Version })
            .IsUnique();
    }
}
