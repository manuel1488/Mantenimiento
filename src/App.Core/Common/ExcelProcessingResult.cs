using App.Core.DTOs.Product;

namespace App.Core.Common;

public class ExcelProcessingResult
{
    public BulkProductLoadRequestDto? Request { get; set; }
    public List<ExcelError> Errors { get; set; } = new();
    public bool HasErrors => Errors.Any();
    public string SheetName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
}

public class ExcelError
{
    public int RowNumber { get; set; }
    public string ColumnName { get; set; } = string.Empty;
    public string CellValue { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string CellReference { get; set; } = string.Empty;
}