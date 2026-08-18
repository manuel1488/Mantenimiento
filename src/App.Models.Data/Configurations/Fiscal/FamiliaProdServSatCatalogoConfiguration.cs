using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using App.Models.Fiscal;

namespace App.Models.Data.Configurations.Fiscal;

public class FamiliaProdServSatCatalogoConfiguration : IEntityTypeConfiguration<FamiliaProdServSatCatalogo>
{
    public void Configure(EntityTypeBuilder<FamiliaProdServSatCatalogo> builder)
    {
        builder.HasIndex(e => e.Codigo)
            .IsUnique();

        builder.HasIndex(e => e.SegmentoCodigo);
    }
}
