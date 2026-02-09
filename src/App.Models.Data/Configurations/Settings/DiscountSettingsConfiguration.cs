using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Models.Settings;

namespace App.Models.Data.Configurations.Settings;

public class DiscountSettingsConfiguration : IEntityTypeConfiguration<DiscountSettings>
{
    public void Configure(EntityTypeBuilder<DiscountSettings> builder)
    {
        builder.HasIndex(e => e.Id)
            .IsUnique();

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_DiscountSettings_ValidRanges",
            "MaximumPublicDiscount >= 0 AND MaximumPublicDiscount <= 100"));
    }
}
