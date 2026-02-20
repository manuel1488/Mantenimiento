using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Models.Shop;

namespace App.Models.Identity;

[Table("id_user_locations")]
public class UserLocation
{
    [Required]
    public string UserId { get; set; } = null!;

    public int LocationId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser User { get; set; } = null!;

    [ForeignKey(nameof(LocationId))]
    public virtual Location Location { get; set; } = null!;
}
