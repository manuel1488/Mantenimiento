using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Settings;

public class UpdateMinioConfiguracionDto
{
    [Required]
    [StringLength(200)]
    public string Endpoint { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string BucketName { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string AccessKey { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string SecretKey { get; set; } = null!;

    public bool UseSsl { get; set; } = true;

    [Required]
    [StringLength(50)]
    public string Region { get; set; } = "us-east-1";

    public int PresignedUrlExpiryHours { get; set; } = 24;
}
