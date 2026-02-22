using App.Models.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shop;

public class CashRegisterConfiguration : IEntityTypeConfiguration<CashRegister>
{
    public void Configure(EntityTypeBuilder<CashRegister> builder)
    {
        builder.Property(e => e.Status).HasConversion<int>();

        builder.HasIndex(e => e.LocationId);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.OpenedAt);

        // Composite index for "find active register for user+location" query
        builder.HasIndex(e => new { e.LocationId, e.UserId, e.Status });

        builder.HasOne(e => e.Location)
            .WithMany()
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CashStation)
            .WithMany(s => s.CashRegisters)
            .HasForeignKey(e => e.CashStationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Movements)
            .WithOne(e => e.CashRegister)
            .HasForeignKey(e => e.CashRegisterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Denominations)
            .WithOne(e => e.CashRegister)
            .HasForeignKey(e => e.CashRegisterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
