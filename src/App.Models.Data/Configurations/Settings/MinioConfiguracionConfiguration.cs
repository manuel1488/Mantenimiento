using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Models.Settings;

namespace App.Models.Data.Configurations.Settings;

public class MinioConfiguracionConfiguration : IEntityTypeConfiguration<MinioConfiguracion>
{
    public void Configure(EntityTypeBuilder<MinioConfiguracion> builder)
    {
        builder.Property(e => e.Endpoint)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.BucketName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.AccessKey)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.SecretKey)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Region)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(e => e.Id)
            .IsUnique();
    }
}
