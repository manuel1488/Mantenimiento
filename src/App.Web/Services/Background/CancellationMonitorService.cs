using App.Core.Interfaces.Billing;
using App.Models.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace App.Web.Services.Background;

/// <summary>
/// Background service that periodically polls SAT via PAC to refresh the status of
/// invoices whose cancellation is pending receiver acceptance (code 204).
/// Can also be triggered manually from the UI via <see cref="TriggerAsync"/>.
/// </summary>
public class CancellationMonitorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CancellationMonitorService> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(2);
    private static readonly TimeSpan ThrottleDelay = TimeSpan.FromMilliseconds(500);

    /// <summary>UTC timestamp of the last completed check. Null if never run.</summary>
    public DateTime? LastRunAt { get; private set; }

    /// <summary>Number of invoices whose status changed in the last run.</summary>
    public int LastRunUpdated { get; private set; }

    /// <summary>True while a check is actively running.</summary>
    public bool IsRunning => _semaphore.CurrentCount == 0;

    public CancellationMonitorService(
        IServiceScopeFactory scopeFactory,
        ILogger<CancellationMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "CancellationMonitorService starting. Initial delay: {Delay}", InitialDelay);

        try
        {
            await Task.Delay(InitialDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCheckAsync(stoppingToken);

            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>
    /// Runs a status check immediately. If a check is already in progress, returns without waiting.
    /// Intended for manual refresh from the UI.
    /// </summary>
    public async Task TriggerAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("CancellationMonitorService: manual trigger requested");
        await RunCheckAsync(cancellationToken);
    }

    private async Task RunCheckAsync(CancellationToken cancellationToken)
    {
        if (!await _semaphore.WaitAsync(0, cancellationToken))
        {
            _logger.LogInformation(
                "CancellationMonitorService: check already in progress, skipping");
            return;
        }

        try
        {
            await RunCheckInternalAsync(cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task RunCheckInternalAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CancellationMonitorService: checking pending cancellations");

        List<long> pendingIds;

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var contextFactory = scope.ServiceProvider
                .GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            pendingIds = await context.MexicoInvoices
                .AsNoTracking()
                .Where(i => i.Status == "CancellationPending" && i.CancellationStatus == "Pending")
                .Select(i => i.Id)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CancellationMonitorService: error querying pending invoices");
            return;
        }

        if (pendingIds.Count == 0)
        {
            _logger.LogDebug("CancellationMonitorService: no pending cancellations found");
            return;
        }

        _logger.LogInformation(
            "CancellationMonitorService: found {Count} pending cancellation(s) to check",
            pendingIds.Count);

        var succeeded = 0;
        var failed = 0;

        foreach (var id in pendingIds)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var invoiceService = scope.ServiceProvider
                    .GetRequiredService<IMexicoInvoiceService>();

                var result = await invoiceService.RefreshCancellationStatusAsync(id);

                if (result.IsSuccess)
                {
                    succeeded++;
                    _logger.LogDebug(
                        "CancellationMonitorService: invoice {Id} status refreshed", id);
                }
                else
                {
                    failed++;
                    _logger.LogWarning(
                        "CancellationMonitorService: invoice {Id} refresh returned error: {Error}",
                        id, result.Error);
                }
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(ex,
                    "CancellationMonitorService: unhandled error refreshing invoice {Id}", id);
            }

            if (pendingIds.Count > 1)
                await Task.Delay(ThrottleDelay, cancellationToken);
        }

        LastRunAt = DateTime.UtcNow;
        LastRunUpdated = succeeded;

        _logger.LogInformation(
            "CancellationMonitorService: completed. Succeeded: {S}, Failed: {F}",
            succeeded, failed);
    }
}
