namespace App.Core.DTOs.Identity;

public class UpdateCashierProfileDto
{
    public int LocationId { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}
