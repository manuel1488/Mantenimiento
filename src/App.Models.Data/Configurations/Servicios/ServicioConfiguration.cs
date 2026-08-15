using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using App.Models.Servicios;

namespace App.Models.Data.Configurations.Servicios;

public class ServicioConfiguration : IEntityTypeConfiguration<Servicio>
{
    public void Configure(EntityTypeBuilder<Servicio> builder)
    {
        builder.Property(e => e.Nombre)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasOne(e => e.UnidadMedida)
            .WithMany()
            .HasForeignKey(e => e.UnidadMedidaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
