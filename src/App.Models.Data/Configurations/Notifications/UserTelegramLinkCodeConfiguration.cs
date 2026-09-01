using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Models.Notifications;

namespace App.Models.Data.Configurations.Notifications;

public class UserTelegramLinkCodeConfiguration : IEntityTypeConfiguration<UserTelegramLinkCode>
{
    public void Configure(EntityTypeBuilder<UserTelegramLinkCode> builder)
    {
        builder.HasIndex(e => e.Code);
        builder.HasIndex(e => e.UserId);
    }
}
