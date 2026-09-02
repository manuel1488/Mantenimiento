using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using App.Models.Obras;

namespace App.Models.Data.Configurations.Obras;

public class ObraClienteAccesoConfiguration : IEntityTypeConfiguration<ObraClienteAcceso>
{
    public void Configure(EntityTypeBuilder<ObraClienteAcceso> builder)
    {
        builder.Property(a => a.Token).HasMaxLength(64).IsRequired();

        builder.HasIndex(a => a.Token).IsUnique();
        builder.HasIndex(a => a.ObraId).IsUnique();

        builder.HasOne(a => a.Obra)
            .WithOne()
            .HasForeignKey<ObraClienteAcceso>(a => a.ObraId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
