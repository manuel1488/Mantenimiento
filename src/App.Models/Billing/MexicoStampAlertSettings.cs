using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;
using App.Core.Interfaces;

namespace App.Models.Billing;

[Table("mx_stamp_alert_settings")]
public class MexicoStampAlertSettings : BaseEntity<int>, IAuditTracked
{
    /// <summary>Send alert when available stamps fall at or below this value.</summary>
    public int LowStampThreshold { get; set; } = 50;

    public bool AlertEnabled { get; set; } = true;

    /// <summary>Minimum hours between consecutive alert emails (anti-spam cooldown).</summary>
    public int AlertCooldownHours { get; set; } = 24;

    /// <summary>UTC timestamp of the last alert that was successfully sent.</summary>
    public DateTime? LastAlertSentAt { get; set; }
}
