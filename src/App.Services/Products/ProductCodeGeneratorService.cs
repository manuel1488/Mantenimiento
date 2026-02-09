using App.Core.Interfaces.Shop;
using App.Core.Options;
using App.Models.Data.Contexts;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Text;

namespace App.Services.Products;

public class ProductCodeGeneratorService : IProductCodeGeneratorService, IDisposable
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<ProductCodeGeneratorService> _logger;
    private readonly ProductCodeGeneratorOptions _options;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public ProductCodeGeneratorService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<ProductCodeGeneratorService> logger,
        IOptions<ProductCodeGeneratorOptions> options)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<string> GenerateProductCodeAsync(CancellationToken cancellationToken = default)
    {
        return await GenerateProductCodeAsync(_options.DefaultStrategy, cancellationToken);
    }

    public async Task<string> GenerateProductCodeAsync(CodeGenerationStrategy strategy, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            return strategy switch
            {
                CodeGenerationStrategy.Sequential => await GenerateSequentialCodeAsync(cancellationToken),
                CodeGenerationStrategy.Random => await GenerateRandomCodeAsync(cancellationToken),
                CodeGenerationStrategy.DateBased => await GenerateDateBasedCodeAsync(cancellationToken),
                CodeGenerationStrategy.CategoryBased => await GenerateCategoryBasedCodeAsync(cancellationToken),
                _ => await GenerateSequentialCodeAsync(cancellationToken)
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<string> PreviewNextCodeAsync(CodeGenerationStrategy? strategy = null)
    {
        var useStrategy = strategy ?? _options.DefaultStrategy;

        return useStrategy switch
        {
            CodeGenerationStrategy.Sequential => await PreviewSequentialCodeAsync(),
            CodeGenerationStrategy.DateBased => PreviewDateBasedCode(),
            CodeGenerationStrategy.Random => PreviewRandomCode(),
            CodeGenerationStrategy.CategoryBased => await PreviewCategoryBasedCodeAsync(),
            _ => await PreviewSequentialCodeAsync()
        };
    }

    public bool IsValidGeneratedCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        if (!code.StartsWith(_options.Prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        if (code.Length < _options.MinLength)
            return false;

        return true;
    }

    public async Task<int> GetNextSequentialNumberAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Get the highest sequential number directly using a more efficient query
            var maxSequentialNumber = await context.Products
                .AsNoTracking()
                .Where(p => p.Code.StartsWith(_options.Prefix))
                .CountAsync(cancellationToken: default);

            return maxSequentialNumber + 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next sequential number for product code");
            throw;
        }
    }

    private async Task<string> GenerateSequentialCodeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var nextNumber = await GetNextSequentialNumberAsync();
            var code = FormatSequentialCode(nextNumber);

            return await EnsureUniqueCodeAsync(code, () => GenerateSequentialCodeAsync(cancellationToken), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating sequential product code");
            throw;
        }
    }

    private async Task<string> GenerateRandomCodeAsync(CancellationToken cancellationToken)
    {
        const int maxAttempts = 100;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var code = GenerateRandomCode();

            await using var context = await _contextFactory.CreateDbContextAsync();
            var exists = await context.Products
                .AsNoTracking()
                .AnyAsync(p => p.Code == code, cancellationToken);

            if (!exists)
                return code;
        }

        throw new InvalidOperationException($"Unable to generate unique random product code after {maxAttempts} attempts");
    }

    private async Task<string> GenerateDateBasedCodeAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var datePart = now.ToString(_options.DateFormat);
        var timePart = _options.DateIncludeTime ? now.ToString(_options.TimeFormat) : string.Empty;

        var baseCode = $"{_options.Prefix}{datePart}{timePart}";

        // Add sequential number for same date/time
        var sequentialPart = await GetSequentialPartForDate(datePart);
        var code = $"{baseCode}{sequentialPart:D2}";

        return await EnsureUniqueCodeAsync(code, () => GenerateDateBasedCodeAsync(cancellationToken), cancellationToken);
    }

    private async Task<string> GenerateCategoryBasedCodeAsync(CancellationToken cancellationToken)
    {
        // Default implementation - could be extended based on product category
        var defaultCategory = _options.CategoryPrefixes.FirstOrDefault().Key ?? "GEN";
        var categoryPrefix = _options.CategoryPrefixes.GetValueOrDefault(defaultCategory, "GEN");

        var nextNumber = await GetNextSequentialNumberAsync();
        var code = $"{_options.Prefix}{categoryPrefix}{nextNumber:D4}";

        return await EnsureUniqueCodeAsync(code, () => GenerateCategoryBasedCodeAsync(cancellationToken), cancellationToken);
    }

    private async Task<string> PreviewSequentialCodeAsync()
    {
        var nextNumber = await GetNextSequentialNumberAsync();
        return FormatSequentialCode(nextNumber);
    }

    private string PreviewDateBasedCode()
    {
        var now = DateTime.UtcNow;
        var datePart = now.ToString(_options.DateFormat);
        var timePart = _options.DateIncludeTime ? now.ToString(_options.TimeFormat) : string.Empty;

        return $"{_options.Prefix}{datePart}{timePart}01";
    }

    private string PreviewRandomCode()
    {
        return GenerateRandomCode();
    }

    private async Task<string> PreviewCategoryBasedCodeAsync()
    {
        var defaultCategory = _options.CategoryPrefixes.FirstOrDefault().Key ?? "GEN";
        var categoryPrefix = _options.CategoryPrefixes.GetValueOrDefault(defaultCategory, "GEN");

        var nextNumber = await GetNextSequentialNumberAsync();
        return $"{_options.Prefix}{categoryPrefix}{nextNumber:D4}";
    }

    private string FormatSequentialCode(int number)
    {
        string formattedNumber = _options.PadWithZeros
            ? number.ToString().PadLeft(_options.NumberPadding, '0')
            : number.ToString();

        return $"{_options.Prefix}{formattedNumber}";
    }

    private string GenerateRandomCode()
    {
        var random = new Random();
        var chars = new StringBuilder();

        if (_options.RandomUseNumbers)
            chars.Append("0123456789");

        if (_options.RandomUseLetters)
            chars.Append("ABCDEFGHIJKLMNOPQRSTUVWXYZ");

        var charArray = chars.ToString();
        var randomPart = new string(Enumerable.Repeat(charArray, _options.RandomPartLength)
            .Select(s => s[random.Next(s.Length)]).ToArray());

        return $"{_options.Prefix}{randomPart}";
    }

    private async Task<int> GetSequentialPartForDate(string datePart)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var dateBasedCodes = await context.Products
                .AsNoTracking()
                .Where(p => p.Code.StartsWith($"{_options.Prefix}{datePart}"))
                .Select(p => p.Code)
                .ToListAsync();

            int maxSequential = 0;

            foreach (var code in dateBasedCodes)
            {
                var sequentialPart = code.Substring($"{_options.Prefix}{datePart}".Length);
                if (_options.DateIncludeTime)
                {
                    sequentialPart = sequentialPart.Substring(_options.TimeFormat.Length);
                }

                if (int.TryParse(sequentialPart, out int sequential))
                {
                    maxSequential = Math.Max(maxSequential, sequential);
                }
            }

            return maxSequential + 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sequential part for date {DatePart}", datePart);
            return 1;
        }
    }

    private bool TryExtractSequentialNumber(string code, out int number)
    {
        number = 0;

        if (!code.StartsWith(_options.Prefix))
            return false;

        var numberPart = code.Substring(_options.Prefix.Length);
        return int.TryParse(numberPart, out number);
    }

    private async Task<string> EnsureUniqueCodeAsync(string code, Func<Task<string>> regenerateFunc, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var exists = await context.Products
            .AsNoTracking()
            .AnyAsync(p => p.Code == code, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Generated code {Code} already exists, regenerating...", code);
            return await regenerateFunc();
        }

        return code;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _semaphore?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}