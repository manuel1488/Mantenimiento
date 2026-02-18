using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Models.Shop;

namespace App.Models.Identity;

[Table("id_user_branches")]
public class UserBranch
{
    [Required]
    public string UserId { get; set; } = null!;

    public int BranchId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser User { get; set; } = null!;

    [ForeignKey(nameof(BranchId))]
    public virtual Branch Branch { get; set; } = null!;
}
