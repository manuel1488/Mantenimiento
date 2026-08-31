using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using App.Models.Obras;

namespace App.Models.Data.Configurations.Obras;

public class ObraMensajeConfiguration : IEntityTypeConfiguration<ObraMensaje>
{
    public void Configure(EntityTypeBuilder<ObraMensaje> builder)
    {
        builder.Property(m => m.Tipo).IsRequired();
        builder.Property(m => m.Asunto).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Cuerpo).HasMaxLength(3000).IsRequired();
        builder.Property(m => m.FotoRutaArchivo).HasMaxLength(500);
        builder.Property(m => m.FotoRutaArchivoThumbnail).HasMaxLength(500);
        builder.Property(m => m.Destinatarios).HasMaxLength(500).IsRequired();

        builder.HasOne(m => m.Obra)
            .WithMany()
            .HasForeignKey(m => m.ObraId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
