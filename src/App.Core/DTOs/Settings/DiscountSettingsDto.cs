namespace App.Core.DTOs.Settings;

public class DiscountSettingsDto
{
    public int Id { get; set; }
    public bool RequireAuthorizationForPublicDiscount { get; set; }
    public decimal MaximumPublicDiscount { get; set; }
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
