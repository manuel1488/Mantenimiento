using System.Globalization;

using CsvHelper;
using CsvHelper.Configuration;

using App.Core.DTOs.Fiscal;
using App.Core.Interfaces;
using App.Services.Mappings;

using Microsoft.Extensions.Logging;

namespace App.Services.Data;

public class CsvFiscalCatalogReader : IFiscalCatalogDataReader
{
    private readonly ILogger<CsvFiscalCatalogReader> _logger;
    private readonly string _dataPath;

    public CsvFiscalCatalogReader(string dataPath, ILogger<CsvFiscalCatalogReader> logger)
    {
        _dataPath = dataPath;
        _logger = logger;

        if (!Directory.Exists(_dataPath))
        {
            _logger.LogWarning("Directory not found: {DataPath}", _dataPath);
            Directory.CreateDirectory(_dataPath);
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
            return [];
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

    public async Task<IEnumerable<CreateRegimenFiscalCatalogoDto>> GetRegimenesFiscalesAsync()
    {
        return await ReadCsvFileAsync<CreateRegimenFiscalCatalogoDto, RegimenFiscalCatalogoCsvMap>("regimenes_fiscales.csv");
    }

    public async Task<IEnumerable<CreateUsoCfdiCatalogoDto>> GetUsosCfdiAsync()
    {
        return await ReadCsvFileAsync<CreateUsoCfdiCatalogoDto, UsoCfdiCatalogoCsvMap>("usos_cfdi.csv");
    }

    public async Task<IEnumerable<CreateClaveUnidadSatCatalogoDto>> GetClavesUnidadSatAsync()
    {
        return await ReadCsvFileAsync<CreateClaveUnidadSatCatalogoDto, ClaveUnidadSatCatalogoCsvMap>("claves_unidad_sat.csv");
    }

    public async Task<IEnumerable<CreateClaveProdServSatCatalogoDto>> GetClavesProdServSatAsync()
    {
        return await ReadCsvFileAsync<CreateClaveProdServSatCatalogoDto, ClaveProdServSatCatalogoCsvMap>("claves_prod_serv_sat.csv");
    }

    public async Task<IEnumerable<CreateTipoProdServSatCatalogoDto>> GetTiposProdServSatAsync()
    {
        return await ReadCsvFileAsync<CreateTipoProdServSatCatalogoDto, TipoProdServSatCatalogoCsvMap>("tipos_prod_serv_sat.csv");
    }

    public async Task<IEnumerable<CreateSegmentoProdServSatCatalogoDto>> GetSegmentosProdServSatAsync()
    {
        return await ReadCsvFileAsync<CreateSegmentoProdServSatCatalogoDto, SegmentoProdServSatCatalogoCsvMap>("segmentos_prod_serv_sat.csv");
    }

    public async Task<IEnumerable<CreateFamiliaProdServSatCatalogoDto>> GetFamiliasProdServSatAsync()
    {
        return await ReadCsvFileAsync<CreateFamiliaProdServSatCatalogoDto, FamiliaProdServSatCatalogoCsvMap>("familias_prod_serv_sat.csv");
    }
}
