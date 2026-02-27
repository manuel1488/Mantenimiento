namespace App.Core.DTOs.Billing.Mexico;

/// <summary>Data returned by GET /management/v2/api/users/balance from SW Sapien.</summary>
public class SwSapienStampBalanceData
{
    public string? IdUserBalance { get; set; }
    public string? IdUser { get; set; }
    public int StampsBalance { get; set; }
    public int StampsUsed { get; set; }
    public int StampsAssigned { get; set; }
    public bool IsUnlimited { get; set; }
    public DateTime? ExpirationDate { get; set; }
}

/// <summary>Consolidated view: PAC provider balance + local invoice count.</summary>
public class MexicoStampBalanceDto
{
    /// <summary>Stamps available at the PAC (stampsBalance).</summary>
    public int Available { get; set; }

    /// <summary>Stamps used at the PAC (stampsUsed).</summary>
    public int UsedAtProvider { get; set; }

    /// <summary>Total stamps contracted (stampsAssigned).</summary>
    public int TotalAssigned { get; set; }

    public bool IsUnlimited { get; set; }
    public DateTime? ExpirationDate { get; set; }

    /// <summary>Locally stamped invoices in this system.</summary>
    public int LocalInvoicesStamped { get; set; }

    public DateTime FetchedAt { get; set; }

    /// <summary>True when the PAC was reachable and returned valid data.</summary>
    public bool IsConfigured { get; set; }

    /// <summary>Percent of remaining stamps (0–100). Null when unlimited.</summary>
    public double? AvailablePercent =>
        !IsUnlimited && TotalAssigned > 0
            ? Math.Round((double)Available / TotalAssigned * 100, 1)
            : null;
}

public class StampAlertSettingsDto
{
    public int LowStampThreshold { get; set; } = 50;
    public bool AlertEnabled { get; set; } = true;
    public int AlertCooldownHours { get; set; } = 24;
    public DateTime? LastAlertSentAt { get; set; }
}

public class UpdateStampAlertSettingsDto
{
    public int LowStampThreshold { get; set; } = 50;
    public bool AlertEnabled { get; set; } = true;
    public int AlertCooldownHours { get; set; } = 24;
}
