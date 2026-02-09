using App.Core.Interfaces.Shop;

namespace App.Core.Options;

public class ProductCodeGeneratorOptions
{
    public const string SectionName = "ProductCodeGenerator";
    public string Prefix { get; set; } = "PROD";
    public int MinLength { get; set; } = 8;
    public CodeGenerationStrategy DefaultStrategy { get; set; } = CodeGenerationStrategy.Sequential;
    public bool PadWithZeros { get; set; } = true;
    public int NumberPadding { get; set; } = 4;
    
    // Sequential options
    public int SequentialStartNumber { get; set; } = 1;
    
    // Random options
    public bool RandomUseNumbers { get; set; } = true;
    public bool RandomUseLetters { get; set; } = true;
    public int RandomPartLength { get; set; } = 6;
    
    // Date-based options
    public string DateFormat { get; set; } = "yyyyMMdd";
    public bool DateIncludeTime { get; set; } = false;
    public string TimeFormat { get; set; } = "HHmm";
    
    // Category-based options (future extension)
    public Dictionary<string, string> CategoryPrefixes { get; set; } = new();
}