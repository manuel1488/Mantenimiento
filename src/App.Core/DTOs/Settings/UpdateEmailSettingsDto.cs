using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Settings;

public class UpdateEmailSettingsDto
{
    [StringLength(100)]
    public string? SmtpHost { get; set; }
    
    [Range(1, 65535)]
    public int? SmtpPort { get; set; }
    
    [StringLength(100)]
    public string? SmtpUser { get; set; }
    
    [StringLength(100)]
    public string? SmtpPassword { get; set; }
    
    [StringLength(100)]
    [EmailAddress]
    public string? FromEmail { get; set; }
    
    [StringLength(100)]
    public string? FromName { get; set; }
    
    public bool UseSsl { get; set; }
}