using App.Models.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shop;

public class CashRegisterDenominationConfiguration : IEntityTypeConfiguration<CashRegisterDenomination>
{
    public void Configure(EntityTypeBuilder<CashRegisterDenomination> builder)
    {
        builder.Property(e => e.DenominationType).HasConversion<int>();

        builder.HasIndex(e => e.CashRegisterId);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_CashRegisterDenomination_QuantityNonNegative",
            "Quantity >= 0"));
    }
}
