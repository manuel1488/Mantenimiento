using App.Core.Interfaces;

using Microsoft.AspNetCore.Identity;

namespace App.Models.Identity;

public class ApplicationUser : IdentityUser, IAuditableEntity, ISoftDelete
{
    public string FullName { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLogin { get; set; }

    // Branch assignments
    public virtual ICollection<UserBranch> UserBranches { get; set; } = new List<UserBranch>();

    // Implementación de IAuditableEntity
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    // Implementación de ISoftDelete
    public uint IsDeleted { get; set; }
    public string? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
}