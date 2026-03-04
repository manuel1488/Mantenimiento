namespace App.Core.Interfaces;

public interface ICfdiPostalCodeSeeder
{
    Task<bool> IsSeededAsync();
    Task SeedAsync();
}
