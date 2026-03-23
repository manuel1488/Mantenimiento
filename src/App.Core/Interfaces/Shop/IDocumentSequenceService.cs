namespace App.Core.Interfaces.Shop;

public interface IDocumentSequenceService
{
    /// <summary>
    /// Atomically gets the next sequence number for a document type and year.
    /// </summary>
    Task<string> GetNextNumberAsync(string documentType, string prefix, int year);
}
