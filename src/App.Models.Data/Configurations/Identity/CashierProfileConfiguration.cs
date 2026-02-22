using App.Models.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Identity;

public class CashierProfileConfiguration : IEntityTypeConfiguration<CashierProfile>
{
    public void Configure(EntityTypeBuilder<CashierProfile> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.UserId)
            .IsUnique();

        builder.Property(e => e.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.ModifiedBy)
            .HasMaxLength(256);

        builder.HasOne(e => e.User)
            .WithOne(u => u.CashierProfile)
            .HasForeignKey<CashierProfile>(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Location)
            .WithMany()
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
