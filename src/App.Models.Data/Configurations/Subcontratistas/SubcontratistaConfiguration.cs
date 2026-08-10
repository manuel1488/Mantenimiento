using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using App.Models.Subcontratistas;

namespace App.Models.Data.Configurations.Subcontratistas;

public class SubcontratistaConfiguration : IEntityTypeConfiguration<Subcontratista>
{
    public void Configure(EntityTypeBuilder<Subcontratista> builder)
    {
        builder.Property(e => e.Nombre)
            .HasMaxLength(150)
            .IsRequired();
    }
}
