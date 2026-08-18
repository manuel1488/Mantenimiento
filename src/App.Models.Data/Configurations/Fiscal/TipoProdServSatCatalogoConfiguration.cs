using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using App.Models.Fiscal;

namespace App.Models.Data.Configurations.Fiscal;

public class TipoProdServSatCatalogoConfiguration : IEntityTypeConfiguration<TipoProdServSatCatalogo>
{
    public void Configure(EntityTypeBuilder<TipoProdServSatCatalogo> builder)
    {
        builder.HasIndex(e => e.Codigo)
            .IsUnique();
    }
}
