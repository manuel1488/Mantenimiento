using App.Models.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Models.Data.Configurations.Identity;

public class UserBranchConfiguration : IEntityTypeConfiguration<UserBranch>
{
    public void Configure(EntityTypeBuilder<UserBranch> builder)
    {
        builder.HasKey(e => new { e.UserId, e.BranchId });

        builder.HasOne(e => e.User)
            .WithMany(u => u.UserBranches)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Branch)
            .WithMany()
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
