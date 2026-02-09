using App.Core.Interfaces;

namespace App.Core.Base;

public abstract class BaseEntity<TKey> : IEntity<TKey>, IAuditableEntity, ISoftDelete
{
    public TKey Id { get; set; } = default!;
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public uint IsDeleted { get; set; }
    public string? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
}