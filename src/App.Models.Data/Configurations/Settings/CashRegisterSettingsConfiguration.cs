using App.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Settings;

public class CashRegisterSettingsConfiguration : IEntityTypeConfiguration<CashRegisterSettings>
{
    public void Configure(EntityTypeBuilder<CashRegisterSettings> builder)
    {
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_CashRegisterSettings_MaxWithdrawal",
            "MaxWithdrawalAmount > 0"));
    }
}
