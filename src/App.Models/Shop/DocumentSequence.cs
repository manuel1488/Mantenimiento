using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Models.Shop;

[Table("sh_document_sequences")]
public class DocumentSequence
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string DocumentType { get; set; } = null!;

    public int Year { get; set; }

    public int CurrentValue { get; set; }
}
