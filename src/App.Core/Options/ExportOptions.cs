using System.ComponentModel.DataAnnotations;

namespace App.Core.Options;

public class ExportOptions
{
    public const string SectionName = "Export";
    
    [Range(1, 100000)]
    public int MaxExportRecords { get; set; } = 10000;

    [Range(1, 10000)]
    public int MaxPdfRecords { get; set; } = 10000;

    [Range(100, 5000)]
    public int DefaultChunkSize { get; set; } = 1000;

    [Required]
    public string TempPath { get; set; } = "wwwroot/temp";
}