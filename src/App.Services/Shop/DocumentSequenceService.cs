using App.Models.Data.Contexts;
using App.Models.Shop;

using Microsoft.EntityFrameworkCore;

namespace App.Services.Shop;

public class DocumentSequenceService : IDocumentSequenceService
{
    public async Task<string> GetNextNumberAsync(ApplicationDbContext context, string documentType, string prefix, int year)
    {
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
