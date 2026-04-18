using App.Models.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shared;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasIndex(e => e.Email);
        builder.HasIndex(e => e.Name);

        builder.Property(e => e.PostalCode)
            .IsUnicode(false);

        builder.Property(e => e.CountryCode)
            .IsUnicode(false);

        // 1-to-1 relationship with optional fiscal profile
        builder.HasOne(e => e.FiscalProfile)
            .WithOne(fp => fp.Customer)
            .HasForeignKey<CustomerFiscalProfile>(fp => fp.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
