using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using App.Models.Facturas;
using App.Models.Obras;

namespace App.Models.Data.Configurations.Obras;

public class ObraConfiguration : IEntityTypeConfiguration<Obra>
{
    public void Configure(EntityTypeBuilder<Obra> builder)
    {
        builder.Property(e => e.Direccion)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(e => e.Estado)
            .HasConversion<int>()
            .IsRequired();

        builder.HasOne(e => e.Cliente)
            .WithMany(c => c.Obras)
            .HasForeignKey(e => e.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Factura)
            .WithOne(f => f.Obra)
            .HasForeignKey<Factura>(f => f.ObraId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CotizacionOrigen)
            .WithOne(c => c.ObraGenerada)
            .HasForeignKey<Obra>(e => e.CotizacionOrigenId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
