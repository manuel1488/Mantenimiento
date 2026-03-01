using App.Models.Shop;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shop;

public class BulkLabelJobConfiguration : IEntityTypeConfiguration<BulkLabelJob>
{
    public void Configure(EntityTypeBuilder<BulkLabelJob> builder)
    {
        builder.HasIndex(e => e.ProductId);
        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => e.BatchNumber);

        builder.Property(e => e.LabelCount)
            .HasDefaultValue(1);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
