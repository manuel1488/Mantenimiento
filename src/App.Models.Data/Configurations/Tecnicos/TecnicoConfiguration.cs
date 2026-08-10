using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using App.Models.Tecnicos;

namespace App.Models.Data.Configurations.Tecnicos;

public class TecnicoConfiguration : IEntityTypeConfiguration<Tecnico>
{
    public void Configure(EntityTypeBuilder<Tecnico> builder)
    {
        builder.Property(e => e.Nombre)
            .HasMaxLength(150)
            .IsRequired();
    }
}
