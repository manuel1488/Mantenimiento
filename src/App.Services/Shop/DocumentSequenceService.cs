using App.Core.Interfaces.Shop;
using App.Models.Data.Contexts;
using App.Models.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Shop;

public class DocumentSequenceService : IDocumentSequenceService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<DocumentSequenceService> _logger;

    public DocumentSequenceService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<DocumentSequenceService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<string> GetNextNumberAsync(string documentType, string prefix, int year)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Use raw SQL for atomic increment (UPDATE + SELECT in one round-trip)
        var sequence = await context.DocumentSequences
            .FirstOrDefaultAsync(s => s.DocumentType == documentType && s.Year == year);

        if (sequence == null)
        {
            sequence = new DocumentSequence
            {
                DocumentType = documentType,
                Year = year,
                CurrentValue = 1
            };
            context.DocumentSequences.Add(sequence);
        }
        else
        {
            sequence.CurrentValue++;
        }

        await context.SaveChangesAsync();

        return $"{prefix}-{year}-{sequence.CurrentValue:D4}";
    }
}
