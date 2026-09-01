using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Models.Notifications;

namespace App.Models.Data.Configurations.Notifications;

public class UserNotificationSubscriptionConfiguration : IEntityTypeConfiguration<UserNotificationSubscription>
{
    public void Configure(EntityTypeBuilder<UserNotificationSubscription> builder)
    {
        builder.HasIndex(e => new { e.UserId, e.EventType, e.ChannelType })
            .IsUnique();
    }
}
