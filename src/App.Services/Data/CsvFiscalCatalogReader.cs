using CsvHelper;
using CsvHelper.Configuration;

using App.Core.DTOs.Billing;
using App.Core.Interfaces;
using App.Services.Mappings;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace App.Services.Data;

public class CsvFiscalCatalogReader : IFiscalCatalogDataReader
{
    private readonly ILogger<CsvFiscalCatalogReader> _logger;
    private readonly string _dataPath;

    public CsvFiscalCatalogReader(
        string dataPath,
        ILogger<CsvFiscalCatalogReader> logger)
    {
        _dataPath = dataPath;
        _logger = logger;

        // Verificar que el directorio existe
        if (!Directory.Exists(_dataPath))
        {
            _logger.LogWarning("Directory not found: {DataPath}", _dataPath);
            Directory.CreateDirectory(_dataPath);
        }

        // Verificar que los archivos existen
        var requiredFiles = new[]
        {
            "fiscal_regimes.csv",
            "payment_forms.csv",
            "payment_methods.csv",
            "cfdi_uses.csv",
            "product_services.csv",
            "sat_units.csv"
        };

        foreach (var file in requiredFiles)
        {
            var filePath = Path.Combine(_dataPath, file);
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Required file not found: {FilePath}", filePath);
            }
        }
    }

    private async Task<IEnumerable<T>> ReadCsvFileAsync<T, TMap>(string fileName) 
        where T : class 
        where TMap : ClassMap<T>
    {
        var filePath = Path.Combine(_dataPath, fileName);
        
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("File not found: {FilePath}", filePath);
            return Enumerable.Empty<T>();
        }

        try
        {
            using var reader = new StreamReader(filePath);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
                TrimOptions = TrimOptions.Trim,
                IgnoreBlankLines = true
            };

            using var csv = new CsvReader(reader, config);
            csv.Context.RegisterClassMap<TMap>();

            var records = new List<T>();
            await foreach (var record in csv.GetRecordsAsync<T>())
            {
                records.Add(record);
            }
            return records;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading CSV file {FileName}", fileName);
            throw;
        }
    }

    public async Task<IEnumerable<CreateMexicoFiscalRegimeDto>> GetFiscalRegimesAsync()
    {
        return await ReadCsvFileAsync<CreateMexicoFiscalRegimeDto, MexicoFiscalRegimeCsvMap>("fiscal_regimes.csv");
    }

    public async Task<IEnumerable<CreateMexicoPaymentFormDto>> GetPaymentFormsAsync()
    {
        return await ReadCsvFileAsync<CreateMexicoPaymentFormDto, MexicoPaymentFormCsvMap>("payment_forms.csv");
    }

    public async Task<IEnumerable<CreateMexicoPaymentMethodDto>> GetPaymentMethodsAsync()
    {
        return await ReadCsvFileAsync<CreateMexicoPaymentMethodDto, MexicoPaymentMethodCsvMap>("payment_methods.csv");
    }

    public async Task<IEnumerable<CreateMexicoCfdiUseDto>> GetCfdiUsesAsync()
    {
        return await ReadCsvFileAsync<CreateMexicoCfdiUseDto, MexicoCfdiUseCsvMap>("cfdi_uses.csv");
    }

    public async Task<IEnumerable<CreateMexicoProductServiceDto>> GetProductServicesAsync()
    {
        return await ReadCsvFileAsync<CreateMexicoProductServiceDto, MexicoProductServiceCsvMap>("product_services.csv");
    }

    public async Task<IEnumerable<CreateMexicoSatUnitDto>> GetSatUnitsAsync()
    {
        return await ReadCsvFileAsync<CreateMexicoSatUnitDto, MexicoSatUnitCsvMap>("sat_units.csv");
    }
}