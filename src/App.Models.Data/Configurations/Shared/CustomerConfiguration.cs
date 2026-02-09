using App.Models.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shared;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasIndex(e => e.Email);
        builder.HasIndex(e => e.TaxId);
        builder.HasIndex(e => new { e.CountryCode, e.TaxId })
            .IsUnique()
            .HasFilter("TaxId IS NOT NULL");

        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => e.LegalName);        
        
        builder.Property(e => e.PostalCode)
            .IsUnicode(false);
        
        builder.Property(e => e.CountryCode)
            .IsUnicode(false);

        builder.Property(e => e.TaxId)
            .IsUnicode(false);
        
        builder.Property(e => e.CaGstNumber)
            .IsUnicode(false);
        
        builder.Property(e => e.CaPstNumber)
            .IsUnicode(false);
        
        builder.Property(e => e.CaHstNumber)
            .IsUnicode(false);
        
        builder.Property(e => e.CaQstNumber)
            .IsUnicode(false);
    }
}