using App.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Settings;

public class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.Property(e => e.Type)
            .HasConversion<int>();

        builder.Property(e => e.CardSubtype)
            .HasConversion<int?>();

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.SortOrder)
            .HasDefaultValue(0);

        builder.HasIndex(e => e.IsActive);
        builder.HasIndex(e => e.SortOrder);
    }
}
