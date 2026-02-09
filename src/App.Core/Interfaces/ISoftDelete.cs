namespace App.Core.Interfaces;

public interface ISoftDelete
{
    UInt32 IsDeleted { get; set; }
    string? DeletedBy { get; set; }
    DateTime? DeletedAt { get; set; }
}