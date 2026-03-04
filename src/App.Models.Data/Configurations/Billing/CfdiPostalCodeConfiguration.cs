using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Models.Billing;

namespace App.Models.Data.Configurations.Billing;

public class CfdiPostalCodeConfiguration : IEntityTypeConfiguration<CfdiPostalCode>
{
    public void Configure(EntityTypeBuilder<CfdiPostalCode> builder)
    {
        builder.HasIndex(e => e.Code);
        builder.HasIndex(e => e.StateId);
    }
}
