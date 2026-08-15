using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using App.Models.Fiscal;

namespace App.Models.Data.Configurations.Fiscal;

public class RegimenFiscalCatalogoConfiguration : IEntityTypeConfiguration<RegimenFiscalCatalogo>
{
    public void Configure(EntityTypeBuilder<RegimenFiscalCatalogo> builder)
    {
        builder.HasIndex(e => e.Codigo)
            .IsUnique();
    }
}
