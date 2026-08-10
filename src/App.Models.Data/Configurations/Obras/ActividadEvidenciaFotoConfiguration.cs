using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using App.Models.Obras;

namespace App.Models.Data.Configurations.Obras;

public class ActividadEvidenciaFotoConfiguration : IEntityTypeConfiguration<ActividadEvidenciaFoto>
{
    public void Configure(EntityTypeBuilder<ActividadEvidenciaFoto> builder)
    {
        builder.Property(e => e.Tipo)
            .HasConversion<int>()
            .IsRequired();

        builder.HasOne(e => e.Actividad)
            .WithMany(a => a.Evidencias)
            .HasForeignKey(e => e.ActividadId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
