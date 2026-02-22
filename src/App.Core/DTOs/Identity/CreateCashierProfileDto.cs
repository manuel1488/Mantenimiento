using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Identity;

public class CreateCashierProfileDto
{
    [Required]
    public string UserId { get; set; } = null!;

    [Required]
    public int LocationId { get; set; }

    public string? Notes { get; set; }
}
