using App.Models.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shop;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.HasIndex(e => e.Name);

        // Unique name among non-deleted branches
        builder.HasIndex(e => new { e.Name, e.IsDeleted })
            .HasFilter("IsDeleted = 0")
            .IsUnique();

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);
    }
}
