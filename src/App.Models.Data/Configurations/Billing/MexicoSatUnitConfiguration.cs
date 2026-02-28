using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Models.Billing;

namespace App.Models.Data.Configurations.Billing;

public class MexicoSatUnitConfiguration : IEntityTypeConfiguration<MexicoSatUnit>
{
    public void Configure(EntityTypeBuilder<MexicoSatUnit> builder)
    {
        builder.HasIndex(e => e.Code).IsUnique();
    }
}
