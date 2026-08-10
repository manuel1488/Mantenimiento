using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using App.Models.Clientes;

namespace App.Models.Data.Configurations.Clientes;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.Property(e => e.Rfc)
            .HasMaxLength(13)
            .IsRequired()
            .IsUnicode(false);

        builder.HasIndex(e => e.Rfc)
            .IsUnique();

        builder.Property(e => e.Correo)
            .HasMaxLength(150)
            .IsRequired();
    }
}
