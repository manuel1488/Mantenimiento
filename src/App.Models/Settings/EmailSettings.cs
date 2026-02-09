using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;

namespace App.Models.Settings;

[Table("stg_email_settings")]
public class EmailSettings : BaseEntity<int>
{
    [StringLength(100)]
    public string? SmtpHost { get; set; }

    public int? SmtpPort { get; set; }

    [StringLength(100)]
    public string? SmtpUser { get; set; }

    [StringLength(100)]
    public string? SmtpPassword { get; set; }

    [StringLength(100)]
    public string? FromEmail { get; set; }

    [StringLength(100)]
    public string? FromName { get; set; }

    public bool UseSsl { get; set; }
}

