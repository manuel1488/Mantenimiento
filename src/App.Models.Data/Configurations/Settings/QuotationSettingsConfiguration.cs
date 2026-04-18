using App.Models.Settings;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Settings;

public class QuotationSettingsConfiguration : IEntityTypeConfiguration<QuotationSettings>
{
    public void Configure(EntityTypeBuilder<QuotationSettings> builder)
    {
        builder.Property(e => e.ShowBankDetails).HasDefaultValue(false);
        builder.Property(e => e.ShowContactInfo).HasDefaultValue(false);

        builder.Property(e => e.BankRfc).IsUnicode(false);
        builder.Property(e => e.BankAccountNumber).IsUnicode(false);
        builder.Property(e => e.BankClabeNumber).IsUnicode(false);
        builder.Property(e => e.BankSwift).IsUnicode(false);
        builder.Property(e => e.ContactWhatsapp).IsUnicode(false);
        builder.Property(e => e.ContactPhone).IsUnicode(false);
    }
}
