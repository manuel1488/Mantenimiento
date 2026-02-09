using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Models.Billing;

namespace App.Models.Data.Configurations.Billing;

public class MexicoProductServiceConfiguration : IEntityTypeConfiguration<MexicoProductService>
{
    public void Configure(EntityTypeBuilder<MexicoProductService> builder)
    {
        builder.HasIndex(e => e.Id).IsUnique();
    }
}