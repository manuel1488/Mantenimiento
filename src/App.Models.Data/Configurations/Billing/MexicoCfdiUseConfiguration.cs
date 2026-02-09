using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Models.Billing;

namespace App.Models.Data.Configurations.Billing;

public class MexicoCfdiUseConfiguration : IEntityTypeConfiguration<MexicoCfdiUse>
{
    public void Configure(EntityTypeBuilder<MexicoCfdiUse> builder)
    {
        builder.HasIndex(e => e.Id).IsUnique();
    }
}