using App.Models.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shop;

public class CashRegisterMovementConfiguration : IEntityTypeConfiguration<CashRegisterMovement>
{
    public void Configure(EntityTypeBuilder<CashRegisterMovement> builder)
    {
        builder.Property(e => e.MovementType).HasConversion<int>();

        builder.HasIndex(e => e.CashRegisterId);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_CashRegisterMovement_AmountPositive",
            "Amount > 0"));
    }
}
