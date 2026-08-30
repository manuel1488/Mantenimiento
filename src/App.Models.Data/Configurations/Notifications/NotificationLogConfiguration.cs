using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using App.Models.Notifications;

namespace App.Models.Data.Configurations.Notifications;

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.Property(e => e.Channel)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(e => new { e.RelatedEntityType, e.RelatedEntityId });
    }
}
