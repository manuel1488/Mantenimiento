namespace App.Core.Interfaces;

public interface ITemplateService
{
    Task<byte[]> GenerateInventoryTemplateAsync();
    Task<byte[]> GenerateProductTemplateAsync();
    string[] GetProductoHeadersName();
}