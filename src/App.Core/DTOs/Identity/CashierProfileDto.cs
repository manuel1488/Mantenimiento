namespace App.Core.DTOs.Identity;

public class CashierProfileDto
{
    public long Id { get; set; }
    public string UserId { get; set; } = null!;
    public string UserFullName { get; set; } = null!;
    public string? UserEmail { get; set; }
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
