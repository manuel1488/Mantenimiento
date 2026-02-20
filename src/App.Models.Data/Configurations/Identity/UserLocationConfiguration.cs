using App.Models.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Identity;

public class UserLocationConfiguration : IEntityTypeConfiguration<UserLocation>
{
    public void Configure(EntityTypeBuilder<UserLocation> builder)
    {
        builder.HasKey(e => new { e.UserId, e.LocationId });

        builder.HasOne(e => e.User)
            .WithMany(u => u.UserLocations)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Location)
            .WithMany()
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
