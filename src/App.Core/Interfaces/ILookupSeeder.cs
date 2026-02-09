namespace App.Core.Interfaces;

public interface ILookupSeeder
{
    /// <summary>
    /// Seeds the lookup data
    /// </summary>
    Task SeedAsync();
}