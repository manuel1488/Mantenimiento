using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using App.Models.Fiscal;

namespace App.Models.Data.Configurations.Fiscal;

public class ClaveProdServSatCatalogoConfiguration : IEntityTypeConfiguration<ClaveProdServSatCatalogo>
{
    public void Configure(EntityTypeBuilder<ClaveProdServSatCatalogo> builder)
    {
        builder.HasIndex(e => e.Codigo)
            .IsUnique();
    }
}
