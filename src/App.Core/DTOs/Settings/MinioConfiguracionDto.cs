namespace App.Core.DTOs.Settings;

public class MinioConfiguracionDto
{
    public int Id { get; set; }
    public string Endpoint { get; set; } = null!;
    public string BucketName { get; set; } = null!;
    public string AccessKey { get; set; } = null!;
    public string SecretKey { get; set; } = null!;
    public bool UseSsl { get; set; }
    public string Region { get; set; } = null!;
    public int PresignedUrlExpiryHours { get; set; }
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
