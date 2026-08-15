using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using App.Models.Fiscal;

namespace App.Models.Data.Configurations.Fiscal;

public class UsoCfdiCatalogoConfiguration : IEntityTypeConfiguration<UsoCfdiCatalogo>
{
    public void Configure(EntityTypeBuilder<UsoCfdiCatalogo> builder)
    {
        builder.HasIndex(e => e.Codigo)
            .IsUnique();
    }
}
