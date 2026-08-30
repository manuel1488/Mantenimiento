using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using App.Models.Obras;

namespace App.Models.Data.Configurations.Obras;

public class ObraFotoGeneralConfiguration : IEntityTypeConfiguration<ObraFotoGeneral>
{
    public void Configure(EntityTypeBuilder<ObraFotoGeneral> builder)
    {
        builder.Property(f => f.RutaArchivo).HasMaxLength(500).IsRequired();
        builder.Property(f => f.RutaArchivoThumbnail).HasMaxLength(500);

        builder.HasOne(f => f.Obra)
            .WithMany(o => o.FotosGenerales)
            .HasForeignKey(f => f.ObraId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
