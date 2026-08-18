using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using App.Models.Fiscal;

namespace App.Models.Data.Configurations.Fiscal;

public class SegmentoProdServSatCatalogoConfiguration : IEntityTypeConfiguration<SegmentoProdServSatCatalogo>
{
    public void Configure(EntityTypeBuilder<SegmentoProdServSatCatalogo> builder)
    {
        builder.HasIndex(e => e.Codigo)
            .IsUnique();

        builder.HasIndex(e => e.TipoCodigo);
    }
}
