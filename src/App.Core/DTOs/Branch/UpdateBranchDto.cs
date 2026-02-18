using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Branch;

public class UpdateBranchDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(200)]
    public string? Street { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? State { get; set; }

    [StringLength(20)]
    public string? ZipCode { get; set; }

    [StringLength(3)]
    public string? Country { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    public bool IsActive { get; set; }
}
