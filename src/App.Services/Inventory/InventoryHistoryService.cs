using System.Linq.Expressions;

using AutoMapper;
using App.Core.Constants;
using App.Core.DTOs.Inventory;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Inventory;

public class InventoryHistoryService : IInventoryHistoryService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<InventoryHistoryService> _logger;
    private readonly ICompanySettingsService _companySettingsService;
    private readonly IDateTime _dateTime;
    private readonly ICurrentUserService _currentUserService;

    public InventoryHistoryService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<InventoryHistoryService> logger,
        ICompanySettingsService companySettingsService,
        IDateTime dateTime,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _companySettingsService = companySettingsService;
        _dateTime = dateTime;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Returns null (no restriction) for global-access users; otherwise the user's assigned
    /// location ids, used to constrain queries to LocationId/DestinationLocationId columns.
    /// </summary>
    private async Task<IReadOnlyList<int>?> GetLocationRestrictionAsync()
    {
        if (await _currentUserService.GetIsGlobalAccessAsync())
            return null;

        return await _currentUserService.GetAssignedLocationIdsAsync();
    }

    public async Task<IList<InventoryMovementDto>> GetProductMovementHistoryAsync(
        long productId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var query = _context.InventoryMovements
                .Include(x => x.Product)
                .Include(x => x.Location)
                .Include(x => x.DestinationLocation)
                .Where(x => x.ProductId == productId)
                .AsNoTracking();

            var locationRestriction = await GetLocationRestrictionAsync();
            if (locationRestriction != null)
            {
                query = query.Where(x => locationRestriction.Contains(x.LocationId) ||
                    (x.DestinationLocationId.HasValue && locationRestriction.Contains(x.DestinationLocationId.Value)));
            }

            var timeZone = await _companySettingsService.GetCurrentTimeZoneAsync() ?? TimeZoneInfo.Utc;

            if (startDate.HasValue)
            {
                var utcStart = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Unspecified), timeZone);
                query = query.Where(x => x.MovementDate >= utcStart);
            }

            if (endDate.HasValue)
            {
                var utcEnd = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(endDate.Value.Date.AddDays(1), DateTimeKind.Unspecified), timeZone);
                query = query.Where(x => x.MovementDate < utcEnd);
            }

            var movementEntities = await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            var movements = _mapper.Map<IList<InventoryMovementDto>>(movementEntities);

            return movements;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting movement history for product {ProductId}", productId);
            throw;
        }
    }

    public async Task<(IList<InventoryMovementDto> items, int totalCount)> GetWarehouseMovementHistoryAsync(
        int? warehouseId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? searchString = null,
        string? movementType = null,
        string? movementSubType = null,
        int page = 0,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (warehouseId.HasValue && warehouseId.Value > 0 &&
                !await _currentUserService.HasAccessToLocationAsync(warehouseId.Value))
            {
                return (new List<InventoryMovementDto>(), 0);
            }

            await using var _context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var query = _context.InventoryMovements
                .Include(x => x.Product)
                .ThenInclude(x => x.UnitMeasure)
                .Include(x => x.Location)
                .Include(x => x.DestinationLocation)
                .AsNoTracking();

            // Filtro de almacén
            if (warehouseId.HasValue && warehouseId.Value > 0)
            {
                query = query.Where(x => x.LocationId == warehouseId.Value ||
                    x.DestinationLocationId == warehouseId.Value);
            }
            else
            {
                var locationRestriction = await GetLocationRestrictionAsync();
                if (locationRestriction != null)
                {
                    query = query.Where(x => locationRestriction.Contains(x.LocationId) ||
                        (x.DestinationLocationId.HasValue && locationRestriction.Contains(x.DestinationLocationId.Value)));
                }
            }

            if (!string.IsNullOrWhiteSpace(movementSubType))
            {
                query = query.Where(x => x.MovementSubType == movementSubType);
            }

            // Filtros de fecha
            var timeZone = await _companySettingsService.GetCurrentTimeZoneAsync() ?? TimeZoneInfo.Utc;

            if (startDate.HasValue)
            {
                var utcStart = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Unspecified), timeZone);
                query = query.Where(x => x.MovementDate >= utcStart);
            }

            if (endDate.HasValue)
            {
                var utcEnd = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(endDate.Value.Date.AddDays(1), DateTimeKind.Unspecified), timeZone);
                query = query.Where(x => x.MovementDate < utcEnd);
            }

            // Filtro de búsqueda (nombre del producto, código o referencia)
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(x =>
                    x.Product.Name.Contains(searchString) ||
                    x.Product.Code.Contains(searchString) ||
                    (x.Reference != null && x.Reference.Contains(searchString)));
            }

            // Filtro de tipo de movimiento
            if (!string.IsNullOrWhiteSpace(movementType))
            {
                query = query.Where(x => x.MovementType == movementType);
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Obtener el total antes de la paginación
            var totalCount = await query.CountAsync(cancellationToken);

            // Aplicar paginación y ordenamiento
            var movementEntities = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var movements = _mapper.Map<IList<InventoryMovementDto>>(movementEntities);

            return (movements, totalCount);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Movement history operation was cancelled for warehouse {WarehouseId}", warehouseId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged movement history for warehouse {WarehouseId}", warehouseId);
            throw;
        }
    }

    public async Task<IList<InventoryMovementDto>> GetPendingTransfersAsync(
        int warehouseId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await _currentUserService.HasAccessToLocationAsync(warehouseId))
            {
                return new List<InventoryMovementDto>();
            }

            await using var _context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var transferEntities = await _context.InventoryMovements
                .Include(x => x.Product)
                .Include(x => x.Location)
                .Include(x => x.DestinationLocation)
                .Where(x => (x.LocationId == warehouseId ||
                            x.DestinationLocationId == warehouseId) &&
                        x.MovementType == InventoryMovementType.Transfer)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            var pendingTransfers = _mapper.Map<IList<InventoryMovementDto>>(transferEntities);

            return pendingTransfers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending transfers for warehouse {WarehouseId}", warehouseId);
            throw;
        }
    }

    public async Task<(IList<InventoryMovementDto> items, int totalCount)> GetTransferHistoryAsync(
        int? sourceWarehouseId = null,
        int? destinationWarehouseId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? searchString = null,
        int page = 0,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if ((sourceWarehouseId.HasValue && sourceWarehouseId.Value > 0 &&
                    !await _currentUserService.HasAccessToLocationAsync(sourceWarehouseId.Value)) ||
                (destinationWarehouseId.HasValue && destinationWarehouseId.Value > 0 &&
                    !await _currentUserService.HasAccessToLocationAsync(destinationWarehouseId.Value)))
            {
                return (new List<InventoryMovementDto>(), 0);
            }

            await using var _context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var query = _context.InventoryMovements
                .Include(x => x.Product)
                .Include(x => x.Location)
                .Include(x => x.DestinationLocation)
                .Where(x => x.MovementType == InventoryMovementType.Transfer)
                .AsNoTracking();

            // Filtro de ubicación origen
            if (sourceWarehouseId.HasValue && sourceWarehouseId.Value > 0)
            {
                query = query.Where(x => x.LocationId == sourceWarehouseId.Value);
            }

            // Filtro de ubicación destino
            if (destinationWarehouseId.HasValue && destinationWarehouseId.Value > 0)
            {
                query = query.Where(x => x.DestinationLocationId == destinationWarehouseId.Value);
            }

            if ((!sourceWarehouseId.HasValue || sourceWarehouseId.Value <= 0) &&
                (!destinationWarehouseId.HasValue || destinationWarehouseId.Value <= 0))
            {
                var locationRestriction = await GetLocationRestrictionAsync();
                if (locationRestriction != null)
                {
                    query = query.Where(x => locationRestriction.Contains(x.LocationId) ||
                        (x.DestinationLocationId.HasValue && locationRestriction.Contains(x.DestinationLocationId.Value)));
                }
            }

            // Filtros de fecha
            var timeZone = await _companySettingsService.GetCurrentTimeZoneAsync() ?? TimeZoneInfo.Utc;

            if (startDate.HasValue)
            {
                var utcStart = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Unspecified), timeZone);
                query = query.Where(x => x.MovementDate >= utcStart);
            }

            if (endDate.HasValue)
            {
                var utcEnd = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(endDate.Value.Date.AddDays(1), DateTimeKind.Unspecified), timeZone);
                query = query.Where(x => x.MovementDate < utcEnd);
            }

            // Filtro de búsqueda (nombre del producto, código o referencia)
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(x => 
                    x.Product.Name.Contains(searchString) ||
                    x.Product.Code.Contains(searchString) ||
                    (x.Reference != null && x.Reference.Contains(searchString)));
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Obtener el total antes de la paginación
            var totalCount = await query.CountAsync(cancellationToken);

            // Aplicar paginación y ordenamiento
            var transferEntities = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var transfers = _mapper.Map<IList<InventoryMovementDto>>(transferEntities);

            return (transfers, totalCount);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Transfer history operation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged transfer history");
            throw;
        }
    }

    public async Task<IList<InventoryAlertDto>> GetCurrentAlertsAsync(
        int? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (warehouseId.HasValue && !await _currentUserService.HasAccessToLocationAsync(warehouseId.Value))
            {
                return new List<InventoryAlertDto>();
            }

            await using var _context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var query = _context.Inventory
                .Include(x => x.Product)
                .ThenInclude(x => x.UnitMeasure)
                .Include(x => x.Location)
                .Where(x =>
                    x.Product.IsActive &&
                    x.Location.IsActive &&
                    ((x.MinStock.HasValue && x.Quantity < x.MinStock.Value) ||
                    (x.MaxStock.HasValue && x.Quantity > x.MaxStock.Value)))
                .AsNoTracking();

            if (warehouseId.HasValue)
            {
                query = query.Where(x => x.LocationId == warehouseId.Value);
            }
            else
            {
                var locationRestriction = await GetLocationRestrictionAsync();
                if (locationRestriction != null)
                    query = query.Where(x => locationRestriction.Contains(x.LocationId));
            }

            var alertEntities = await query
                .ToListAsync(cancellationToken);

            var alerts = _mapper.Map<IList<InventoryAlertDto>>(alertEntities);

            return alerts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current inventory alerts");
            throw;
        }
    }

    public async Task<IList<InventoryAlertDto>> GetAlertHistoryAsync(
        DateTime startDate,
        DateTime endDate,
        int? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (warehouseId.HasValue && !await _currentUserService.HasAccessToLocationAsync(warehouseId.Value))
            {
                return new List<InventoryAlertDto>();
            }

            await using var _context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            // Para el historial de alertas, usamos los movimientos que generaron
            // situaciones de stock mínimo o máximo
            var query = _context.InventoryMovements
                .Include(x => x.Product)
                .Include(x => x.Location)
                .Where(x => x.CreatedAt >= startDate && x.CreatedAt <= endDate)
                .AsNoTracking();

            if (warehouseId.HasValue)
            {
                query = query.Where(x => x.LocationId == warehouseId.Value);
            }
            else
            {
                var locationRestriction = await GetLocationRestrictionAsync();
                if (locationRestriction != null)
                    query = query.Where(x => locationRestriction.Contains(x.LocationId));
            }

            var movements = await query.ToListAsync(cancellationToken);

            // Procesamos los movimientos para identificar las alertas
            var alerts = new List<InventoryAlertDto>();
            foreach (var movement in movements)
            {
                var inventory = await _context.Inventory
                    .Include(x => x.Product)
                    .Include(x => x.Location)
                    .FirstOrDefaultAsync(x =>
                        x.ProductId == movement.ProductId &&
                        x.LocationId == movement.LocationId,
                        cancellationToken);

                if (inventory == null) continue;

                if (inventory.MinStock.HasValue && movement.NewBalance < inventory.MinStock.Value)
                {
                    alerts.Add(new InventoryAlertDto
                    {
                        ProductId = inventory.ProductId,
                        ProductName = inventory.Product.Name,
                        ProductCode = inventory.Product.Code,
                        LocationId = inventory.LocationId,
                        LocationName = inventory.Location.Name,
                        CurrentStock = movement.NewBalance,
                        MinStock = inventory.MinStock,
                        MaxStock = inventory.MaxStock,
                        UnitMeasureName = inventory.Product.UnitMeasure.Name,
                        AlertType = InventoryAlertType.LowStock
                    });
                }
                else if (inventory.MaxStock.HasValue && movement.NewBalance > inventory.MaxStock.Value)
                {
                    alerts.Add(new InventoryAlertDto
                    {
                        ProductId = inventory.ProductId,
                        ProductName = inventory.Product.Name,
                        ProductCode = inventory.Product.Code,
                        LocationId = inventory.LocationId,
                        LocationName = inventory.Location.Name,
                        CurrentStock = movement.NewBalance,
                        MinStock = inventory.MinStock,
                        MaxStock = inventory.MaxStock,
                        UnitMeasureName = inventory.Product.UnitMeasure.Name,
                        AlertType = InventoryAlertType.OverStock
                    });
                }
            }

            return alerts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alert history");
            throw;
        }
    }
    
    public async Task<(IList<InventoryMovementDto> items, int totalCount)> GetWarehouseMovementHistoryByTypesAsync(
        int? warehouseId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? searchString = null,
        string[]? movementTypes = null,
        string? movementSubType = null,
        int page = 0,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (warehouseId.HasValue && warehouseId.Value > 0 &&
                !await _currentUserService.HasAccessToLocationAsync(warehouseId.Value))
            {
                return (new List<InventoryMovementDto>(), 0);
            }

            await using var _context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var query = _context.InventoryMovements
                .Include(x => x.Product)
                .ThenInclude(x => x.UnitMeasure)
                .Include(x => x.Location)
                .Include(x => x.DestinationLocation)
                .AsNoTracking();

            // Filtro de almacén
            if (warehouseId.HasValue && warehouseId.Value > 0)
            {
                query = query.Where(x => x.LocationId == warehouseId.Value ||
                    x.DestinationLocationId == warehouseId.Value);
            }
            else
            {
                var locationRestriction = await GetLocationRestrictionAsync();
                if (locationRestriction != null)
                {
                    query = query.Where(x => locationRestriction.Contains(x.LocationId) ||
                        (x.DestinationLocationId.HasValue && locationRestriction.Contains(x.DestinationLocationId.Value)));
                }
            }

            // Filtro de tipos de movimiento - Solución para evitar el error de Contains()
            if (movementTypes != null && movementTypes.Length > 0)
            {
                var hasStockIn = movementTypes.Contains(InventoryMovementType.StockIn);
                var hasPurchase = movementTypes.Contains(InventoryMovementType.Purchase);
                var hasReturn = movementTypes.Contains(InventoryMovementType.Return);
                var hasStockOut = movementTypes.Contains(InventoryMovementType.StockOut);
                var hasSale = movementTypes.Contains(InventoryMovementType.Sale);
                var hasReturnToSupplier = movementTypes.Contains(InventoryMovementType.ReturnToSupplier);
                var hasTransfer = movementTypes.Contains(InventoryMovementType.Transfer);
                var hasAdjustment = movementTypes.Contains(InventoryMovementType.Adjustment);
                var hasInitialLoad = movementTypes.Contains(InventoryMovementType.InitialLoad);

                query = query.Where(x => 
                    (hasStockIn && x.MovementType == InventoryMovementType.StockIn) ||
                    (hasPurchase && x.MovementType == InventoryMovementType.Purchase) ||
                    (hasReturn && x.MovementType == InventoryMovementType.Return) ||
                    (hasStockOut && x.MovementType == InventoryMovementType.StockOut) ||
                    (hasSale && x.MovementType == InventoryMovementType.Sale) ||
                    (hasReturnToSupplier && x.MovementType == InventoryMovementType.ReturnToSupplier) ||
                    (hasTransfer && x.MovementType == InventoryMovementType.Transfer) ||
                    (hasAdjustment && x.MovementType == InventoryMovementType.Adjustment) ||
                    (hasInitialLoad && x.MovementType == InventoryMovementType.InitialLoad));
            }

            // Filtro de subtipo de movimiento
            if (!string.IsNullOrWhiteSpace(movementSubType))
            {
                query = query.Where(x => x.MovementSubType == movementSubType);
            }

            // Filtros de fecha
            var timeZone = await _companySettingsService.GetCurrentTimeZoneAsync() ?? TimeZoneInfo.Utc;

            if (startDate.HasValue)
            {
                var utcStart = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Unspecified), timeZone);
                query = query.Where(x => x.MovementDate >= utcStart);
            }

            if (endDate.HasValue)
            {
                var utcEnd = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(endDate.Value.Date.AddDays(1), DateTimeKind.Unspecified), timeZone);
                query = query.Where(x => x.MovementDate < utcEnd);
            }

            // Filtro de búsqueda (nombre del producto, código o referencia)
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(x => 
                    x.Product.Name.Contains(searchString) ||
                    x.Product.Code.Contains(searchString) ||
                    (x.Reference != null && x.Reference.Contains(searchString)));
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Obtener el total antes de la paginación
            var totalCount = await query.CountAsync(cancellationToken);

            // Aplicar paginación y ordenamiento
            var movementEntities = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var movements = _mapper.Map<IList<InventoryMovementDto>>(movementEntities);

            return (movements, totalCount);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Movement history by types operation was cancelled for warehouse {WarehouseId}", warehouseId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged movement history by types for warehouse {WarehouseId}. Types: {MovementTypes}", 
                warehouseId, movementTypes != null ? string.Join(", ", movementTypes) : "null");
            throw;
        }
    }

    public async Task<int> GetCurrentAlertsCountAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            // Consulta optimizada que solo cuenta registros en la base de datos
            var query = _context.Inventory
                .AsNoTracking()
                .Where(x =>
                    x.Product.IsActive &&
                    x.Location.IsActive &&
                    ((x.MinStock.HasValue && x.Quantity < x.MinStock.Value) ||
                     (x.MaxStock.HasValue && x.Quantity > x.MaxStock.Value)));

            var locationRestriction = await GetLocationRestrictionAsync();
            if (locationRestriction != null)
                query = query.Where(x => locationRestriction.Contains(x.LocationId));

            var count = await query.CountAsync(cancellationToken);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory alerts count");
            // En caso de error, devolvemos 0 para evitar que se rompa la UI
            return 0;
        }
    }
}