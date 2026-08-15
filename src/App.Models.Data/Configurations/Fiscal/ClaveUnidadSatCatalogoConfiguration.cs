using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using App.Models.Fiscal;

namespace App.Models.Data.Configurations.Fiscal;

public class ClaveUnidadSatCatalogoConfiguration : IEntityTypeConfiguration<ClaveUnidadSatCatalogo>
{
    public void Configure(EntityTypeBuilder<ClaveUnidadSatCatalogo> builder)
    {
        builder.HasIndex(e => e.Codigo)
            .IsUnique();
    }
}
