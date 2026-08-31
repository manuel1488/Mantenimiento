using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using App.Models.Obras;

namespace App.Models.Data.Configurations.Obras;

public class ActividadAvanceRegistroConfiguration : IEntityTypeConfiguration<ActividadAvanceRegistro>
{
    public void Configure(EntityTypeBuilder<ActividadAvanceRegistro> builder)
    {
        builder.HasOne(e => e.Actividad)
            .WithMany(a => a.AvanceRegistros)
            .HasForeignKey(e => e.ActividadId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
