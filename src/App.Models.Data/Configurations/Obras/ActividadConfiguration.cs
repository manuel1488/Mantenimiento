using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using App.Models.Obras;

namespace App.Models.Data.Configurations.Obras;

public class ActividadConfiguration : IEntityTypeConfiguration<Actividad>
{
    public void Configure(EntityTypeBuilder<Actividad> builder)
    {
        builder.Property(e => e.Estado)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.Descripcion)
            .HasMaxLength(500);

        builder.HasOne(e => e.Obra)
            .WithMany(o => o.Actividades)
            .HasForeignKey(e => e.ObraId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Servicio)
            .WithMany()
            .HasForeignKey(e => e.ServicioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Tecnico)
            .WithMany()
            .HasForeignKey(e => e.TecnicoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Subcontratista)
            .WithMany()
            .HasForeignKey(e => e.SubcontratistaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
