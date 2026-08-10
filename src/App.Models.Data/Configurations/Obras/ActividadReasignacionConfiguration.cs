using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using App.Models.Obras;

namespace App.Models.Data.Configurations.Obras;

public class ActividadReasignacionConfiguration : IEntityTypeConfiguration<ActividadReasignacion>
{
    public void Configure(EntityTypeBuilder<ActividadReasignacion> builder)
    {
        builder.Property(e => e.Motivo)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasOne(e => e.Actividad)
            .WithMany(a => a.Reasignaciones)
            .HasForeignKey(e => e.ActividadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.TecnicoAnterior)
            .WithMany()
            .HasForeignKey(e => e.TecnicoAnteriorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.SubcontratistaAnterior)
            .WithMany()
            .HasForeignKey(e => e.SubcontratistaAnteriorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.TecnicoNuevo)
            .WithMany()
            .HasForeignKey(e => e.TecnicoNuevoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.SubcontratistaNuevo)
            .WithMany()
            .HasForeignKey(e => e.SubcontratistaNuevoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
