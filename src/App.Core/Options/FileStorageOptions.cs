namespace App.Core.Options;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    public string TempPath { get; set; } = string.Empty;
}