using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Models.Billing;

namespace App.Models.Data.Configurations.Billing;

public class MexicoPaymentFormConfiguration : IEntityTypeConfiguration<MexicoPaymentForm>
{
    public void Configure(EntityTypeBuilder<MexicoPaymentForm> builder)
    {
        builder.HasIndex(e => e.Id).IsUnique();
        builder.HasIndex(e => e.Code).IsUnique();

        builder.Property(e => e.Code)
            .IsUnicode(false);
    }
}