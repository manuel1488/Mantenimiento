namespace App.Core.Interfaces.Shop;

public enum CodeGenerationStrategy
{
    Sequential,
    Random,
    DateBased,
    CategoryBased
}

public interface IProductCodeGeneratorService
{
    /// <summary>
    /// Generates a unique product code using the configured strategy
    /// </summary>
    Task<string> GenerateProductCodeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates a unique product code using a specific strategy
    /// </summary>
    Task<string> GenerateProductCodeAsync(CodeGenerationStrategy strategy, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates if the generated code follows the expected format
    /// </summary>
    bool IsValidGeneratedCode(string code);
    
    /// <summary>
    /// Gets the next sequential number for product codes
    /// </summary>
    Task<int> GetNextSequentialNumberAsync();
    
    /// <summary>
    /// Previews what the next code would be without generating it
    /// </summary>
    Task<string> PreviewNextCodeAsync(CodeGenerationStrategy? strategy = null);
}