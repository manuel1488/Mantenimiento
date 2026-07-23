using System.Globalization;

using App.Core.Interfaces;
using App.Models.Billing;
using App.Models.Data.Contexts;
using App.Shared.Services;

using CsvHelper;
using CsvHelper.Configuration;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Seeders;

public class CfdiPostalCodeSeeder : ICfdiPostalCodeSeeder
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<CfdiPostalCodeSeeder> _logger;
    private readonly IDateTime _dateTime;
    private readonly string _csvFilePath;
    private const int BatchSize = 1000;
    private const string SystemUser = "System";

    // IANA timezone mapping for the 5 Mexican timezone zones in the SAT catalog
    private static readonly Dictionary<string, (string IanaId, int OffsetWinter, int OffsetSummer)> _timezoneMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Tiempo del Centro"] = ("America/Mexico_City", -6, -6),
            ["Tiempo del Centro en Frontera"] = ("America/Matamoros", -6, -5),
            ["Tiempo del Noroeste"] = ("America/Tijuana", -8, -7),
            ["Tiempo del Pacífico"] = ("America/Hermosillo", -7, -7),
            ["Tiempo del Sureste"] = ("America/Cancun", -5, -5),
        };

    public CfdiPostalCodeSeeder(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<CfdiPostalCodeSeeder> logger,
        IDateTime dateTime,
        string csvFilePath)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _dateTime = dateTime;
        _csvFilePath = csvFilePath;
    }

    public async Task<bool> IsSeededAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.CfdiPostalCodes.AsNoTracking().AnyAsync();
    }

    public async Task SeedAsync()
    {
        if (await IsSeededAsync())
        {
            _logger.LogInformation("CFDI postal codes catalog already seeded, skipping");
            return;
        }

        if (!File.Exists(_csvFilePath))
        {
            _logger.LogWarning("CFDI postal codes CSV not found at {Path}", _csvFilePath);
            return;
        }

        var records = await ReadCsvAsync();
        if (records.Count == 0)
        {
            _logger.LogWarning("No postal code records to seed");
            return;
        }

        await BulkInsertAsync(records);
    }

    private async Task<List<CfdiPostalCode>> ReadCsvAsync()
    {
        var results = new List<CfdiPostalCode>();
        var now = _dateTime.Now;
        var unknownZones = new HashSet<string>();

        try
        {
            using var reader = new StreamReader(_csvFilePath);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
                TrimOptions = TrimOptions.Trim,
                IgnoreBlankLines = true
            };

            using var csv = new CsvReader(reader, config);

            await foreach (var row in csv.GetRecordsAsync<CfdiPostalCodeCsvRow>())
            {
                if (string.IsNullOrWhiteSpace(row.Code) || string.IsNullOrWhiteSpace(row.TimeZone))
                    continue;

                if (!_timezoneMap.TryGetValue(row.TimeZone, out var tzInfo))
                {
                    unknownZones.Add(row.TimeZone);
                    tzInfo = ("America/Mexico_City", row.TimeZoneOffset, row.TimeZoneOffset);
                }

                results.Add(new CfdiPostalCode
                {
                    Code = row.Code,
                    StateId = row.StateId ?? string.Empty,
                    MunicipalityId = string.IsNullOrWhiteSpace(row.MunicipalityId) ? null : row.MunicipalityId,
                    LocalityId = string.IsNullOrWhiteSpace(row.LocalityId) ? null : row.LocalityId,
                    IsBorderZone = row.IsBorderZone == 1,
                    TimeZoneName = row.TimeZone,
                    IanaTimeZoneId = tzInfo.IanaId,
                    OffsetWinter = tzInfo.OffsetWinter,
                    OffsetSummer = tzInfo.OffsetSummer,
                    CreatedBy = SystemUser,
                    CreatedAt = now
                });
            }

            foreach (var zone in unknownZones)
                _logger.LogWarning("Unknown timezone zone in CSV (defaulted to Mexico City): {Zone}", zone);

            _logger.LogInformation("Read {Count} postal code records from CSV", results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading CFDI postal codes CSV");
            throw;
        }

        return results;
    }

    private async Task BulkInsertAsync(List<CfdiPostalCode> records)
    {
        var total = records.Count;
        var batches = (total + BatchSize - 1) / BatchSize;

        _logger.LogInformation("Seeding {Total} CFDI postal code records in {Batches} multi-row INSERT batches", total, batches);

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var strategy = context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await context.Database.BeginTransactionAsync();

                for (int i = 0; i < batches; i++)
                {
                    var batch = records.Skip(i * BatchSize).Take(BatchSize).ToList();
                    await InsertBatchAsync(context, batch);
                    _logger.LogInformation("Seeded batch {Current}/{Total} of CFDI postal codes", i + 1, batches);
                }

                await transaction.CommitAsync();
            });
            _logger.LogInformation("Successfully seeded {Total} CFDI postal code records", total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk inserting CFDI postal codes");
            throw;
        }
    }

    // Single INSERT ... VALUES (row1),(row2),...,(rowN) per batch.
    // Nullable fields (MunicipalityId, LocalityId) use SQL NULL literal directly
    // when the value is null — avoids EF Core type-inference issues with null objects.
    private static async Task InsertBatchAsync(ApplicationDbContext context, List<CfdiPostalCode> batch)
    {
        var sql = new System.Text.StringBuilder(
            "INSERT INTO `cat_cfdi_postal_codes` " +
            "(`Code`,`StateId`,`MunicipalityId`,`LocalityId`,`IsBorderZone`," +
            "`TimeZoneName`,`IanaTimeZoneId`,`OffsetWinter`,`OffsetSummer`," +
            "`CreatedBy`,`CreatedAt`,`IsDeleted`) VALUES ");

        var parameters = new List<object>();
        int p = 0;

        for (int j = 0; j < batch.Count; j++)
        {
            var r = batch[j];
            if (j > 0) sql.Append(',');
            sql.Append('(');

            // Non-nullable fields → parameterized
            sql.Append($"{{{p++}}}"); parameters.Add(r.Code);
            sql.Append($",{{{p++}}}"); parameters.Add(r.StateId);

            // Nullable strings → NULL literal when null, parameterized otherwise
            sql.Append(r.MunicipalityId != null ? $",{{{p++}}}" : ",NULL");
            if (r.MunicipalityId != null) parameters.Add(r.MunicipalityId);

            sql.Append(r.LocalityId != null ? $",{{{p++}}}" : ",NULL");
            if (r.LocalityId != null) parameters.Add(r.LocalityId);

            // Remaining non-nullable fields → parameterized
            sql.Append($",{{{p++}}}"); parameters.Add(r.IsBorderZone ? 1 : 0);
            sql.Append($",{{{p++}}}"); parameters.Add(r.TimeZoneName);
            sql.Append($",{{{p++}}}"); parameters.Add(r.IanaTimeZoneId);
            sql.Append($",{{{p++}}}"); parameters.Add(r.OffsetWinter);
            sql.Append($",{{{p++}}}"); parameters.Add(r.OffsetSummer);
            sql.Append($",{{{p++}}}"); parameters.Add(r.CreatedBy);
            sql.Append($",{{{p++}}}"); parameters.Add(r.CreatedAt);
            sql.Append(",0"); // IsDeleted always 0

            sql.Append(')');
        }

        await context.Database.ExecuteSqlRawAsync(sql.ToString(), parameters);
    }

    private sealed class CfdiPostalCodeCsvRow
    {
        public string? Id { get; set; }
        public string Code { get; set; } = null!;
        public string? StateId { get; set; }
        public string? MunicipalityId { get; set; }
        public string? LocalityId { get; set; }
        public int IsBorderZone { get; set; }
        public string TimeZone { get; set; } = null!;
        public int TimeZoneOffset { get; set; }
    }
}
