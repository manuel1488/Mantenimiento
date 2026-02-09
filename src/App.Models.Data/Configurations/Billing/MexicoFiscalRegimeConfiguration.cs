using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Models.Billing;

namespace App.Models.Data.Configurations.Billing;

public class MexicoFiscalRegimeConfiguration : IEntityTypeConfiguration<MexicoFiscalRegime>
{
    public void Configure(EntityTypeBuilder<MexicoFiscalRegime> builder)
    {
        builder.HasIndex(e => e.Id).IsUnique();
    }
}