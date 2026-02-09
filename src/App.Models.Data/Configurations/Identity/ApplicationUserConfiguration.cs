using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Models.Identity;

namespace App.Models.Data.Configurations.Identity;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // Eliminar los índices predeterminados
        var userNameIndex = builder.Metadata.FindIndex(new[] { builder.Property(e => e.NormalizedUserName).Metadata });
        if (userNameIndex != null)
            builder.Metadata.RemoveIndex(userNameIndex);
            
        var emailIndex = builder.Metadata.FindIndex(new[] { builder.Property(e => e.NormalizedEmail).Metadata });
        if (emailIndex != null)
            builder.Metadata.RemoveIndex(emailIndex);
                
        builder.HasIndex(e => new { e.NormalizedUserName, e.IsDeleted })
            .HasDatabaseName("UserNameIndex")
            .IsUnique();
                    
        builder.HasIndex(e => new { e.NormalizedEmail, e.IsDeleted })
            .HasDatabaseName("EmailIndex");
    }
}