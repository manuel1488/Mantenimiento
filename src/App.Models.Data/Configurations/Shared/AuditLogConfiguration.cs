using App.Models.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Shared;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Action)
            .HasConversion<int>();

        builder.HasIndex(e => new { e.EntityType, e.EntityId });
        builder.HasIndex(e => e.Timestamp);
        builder.HasIndex(e => e.UserName);
    }
}
