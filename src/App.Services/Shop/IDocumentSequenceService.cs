using App.Models.Data.Contexts;

namespace App.Services.Shop;

/// <summary>
/// Atomically gets the next sequence number for a document type and year, using the
/// caller's own DbContext so the increment participates in the caller's transaction
/// (rolls back together with it on failure or retry).
/// </summary>
public interface IDocumentSequenceService
{
    Task<string> GetNextNumberAsync(ApplicationDbContext context, string documentType, string prefix, int year);
}
