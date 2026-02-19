using AutoMapper;

using App.Core.Constants;
using App.Core.DTOs.Inventory;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Inventory;

public class InventoryService : IInventoryService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<InventoryService> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStringLocalizer<InventoryService> L;
    private readonly IDateTime _dateTime;
    private readonly IMapper _mapper;
    private readonly IInventoryAlertEmailService _inventoryAlertEmailService;

    public InventoryService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<InventoryService> logger,
        ICurrentUserService currentUserService,
        IStringLocalizer<InventoryService> localizer,
        IDateTime dateTime,
        IInventoryAlertEmailService inventoryAlertEmailService)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _currentUserService = currentUserService;
        L = localizer;
        _dateTime = dateTime;
        _inventoryAlertEmailService = inventoryAlertEmailService;
    }

    public async Task<bool> ValidateStockAvailabilityAsync(
        long productId,
        int locationId,
        decimal quantity,
        CancellationToken cancellationToken = default)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();

        var inventory = await _context.Inventory
            .Include(x => x.Product)
            .Include(x => x.Location)
            .Where(x => x.ProductId == productId &&
                       x.LocationId == locationId &&
                       x.Product.IsActive &&
                       x.Location.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (inventory == null)
            return false;

        // Use the new IndividualUnits field, fallback to legacy calculation if not set
        decimal availableIndividualUnits = inventory.IndividualUnits > 0
            ? inventory.IndividualUnits
            : GetAvailableIndividualUnits(inventory);

        return availableIndividualUnits >= quantity;
    }

    public async Task<MovementOperationResult> CreateMovementAsync(
        CreateInventoryMovementDto createDto,
        CancellationToken cancellationToken = default)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Obtener inventario con sus relaciones
            var inventory = await _context.Inventory
                .Include(x => x.Product)
                .Include(x => x.Location)
                .Where(x => x.ProductId == createDto.ProductId &&
                        x.LocationId == createDto.LocationId &&
                        x.Product.IsActive &&
                        x.Location.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (inventory == null)
                return MovementOperationResult.Failure(L["Invalid product or location"]);

            // Determinar si es adición o sustracción de inventario
            var isAddition = createDto.MovementType is
                InventoryMovementType.StockIn or
                InventoryMovementType.Purchase or
                InventoryMovementType.Return;

            // Calculate units for both individual and container quantities
            decimal quantityToMove = createDto.Quantity;
            decimal individualUnitsToMove = createDto.Quantity;

            if (createDto.MovementType == InventoryMovementType.Sale && inventory.Product.IsPartialSaleAllowed)
            {
                // For partial sales, quantity comes as individual units, convert to containers
                quantityToMove = ConvertToContainerUnits(inventory.Product, createDto.Quantity);
            }
            else if (createDto.MovementType != InventoryMovementType.Sale && inventory.Product.IsPartialSaleAllowed)
            {
                // For non-sales (inputs), quantity might come as containers, convert to individual
                individualUnitsToMove = createDto.Quantity * inventory.Product.Content;
            }

            // Get current balances
            decimal currentQuantity = inventory.Quantity;
            decimal currentIndividualUnits = inventory.IndividualUnits > 0
                ? inventory.IndividualUnits
                : GetAvailableIndividualUnits(inventory);

            // Verificar si hay suficiente stock para salidas
            if (!isAddition && currentQuantity < quantityToMove)
            {
                return MovementOperationResult.Failure(L["Insufficient stock"]);
            }

            // Calcular nuevos balances
            decimal newQuantityBalance, newIndividualBalance;

            if (createDto.MovementType == InventoryMovementType.Adjustment)
            {
                // For adjustments, set absolute values
                newQuantityBalance = quantityToMove;
                newIndividualBalance = individualUnitsToMove;
            }
            else if (isAddition)
            {
                newQuantityBalance = currentQuantity + quantityToMove;
                newIndividualBalance = currentIndividualUnits + individualUnitsToMove;
            }
            else
            {
                newQuantityBalance = currentQuantity - quantityToMove;
                newIndividualBalance = currentIndividualUnits - individualUnitsToMove;
            }

            // Verificar que no sean negativos
            if (newQuantityBalance < 0 || newIndividualBalance < 0)
            {
                return MovementOperationResult.Failure(L["Movement would result in negative stock"]);
            }

            // Crear el movimiento con todos los campos
            var movement = new InventoryMovement
            {
                ProductId = createDto.ProductId,
                LocationId = createDto.LocationId,
                MovementType = createDto.MovementType,
                MovementSubType = createDto.MovementSubType,

                // Movement quantities
                Quantity = quantityToMove,
                IndividualUnits = individualUnitsToMove,

                // Balance tracking
                PreviousBalance = currentQuantity,
                NewBalance = newQuantityBalance,
                PreviousIndividualBalance = currentIndividualUnits,
                NewIndividualBalance = newIndividualBalance,

                Reference = createDto.Reference,
                Document = createDto.Document,
                Reason = createDto.Reason,
                UnitCost = createDto.UnitCost,
                MovementDate = createDto.MovementDate ?? _dateTime.Now,
                RelatedParty = createDto.RelatedParty,
                CreatedBy = _currentUserService.FullName ?? "Unknown",
                CreatedAt = _dateTime.Now
            };

            _context.InventoryMovements.Add(movement);

            // Update inventory with new balances
            inventory.Quantity = newQuantityBalance;
            inventory.IndividualUnits = newIndividualBalance;
            inventory.ModifiedBy = _currentUserService.FullName;
            
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Verificar si se generaron alertas después de la operación
            InventoryAlertInfo? alertInfo = null;

            if (inventory.MinStock.HasValue && newQuantityBalance < inventory.MinStock.Value)
            {
                alertInfo = InventoryAlertInfo.LowStock(
                    inventory.Product.Name,
                    inventory.Location.Name,
                    newQuantityBalance,
                    inventory.MinStock.Value);
            }
            else if (inventory.MaxStock.HasValue && newQuantityBalance > inventory.MaxStock.Value)
            {
                alertInfo = InventoryAlertInfo.OverStock(
                    inventory.Product.Name,
                    inventory.Location.Name,
                    newQuantityBalance,
                    inventory.MaxStock.Value);
            }

            // Send email alert if alert was generated
            if (alertInfo != null)
            {
                // Fire and forget email sending to avoid blocking the main operation
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _inventoryAlertEmailService.SendInventoryAlertAsync(alertInfo, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending inventory alert email for product {ProductName} in location {LocationName}",
                            alertInfo.ProductName, alertInfo.LocationName);
                    }
                }, cancellationToken);
            }

            return MovementOperationResult.Successful(_mapper.Map<InventoryMovementDto>(movement), alertInfo);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Concurrency error creating movement");
            return MovementOperationResult.Failure(L["The inventory was modified by another process"]);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error creating movement");
            return MovementOperationResult.Failure(ex.Message);
        }
    }


    public async Task<MovementOperationResult> CreateTransferAsync(
        CreateInventoryTransferDto transferDto,
        CancellationToken cancellationToken = default)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (transferDto.LocationId == transferDto.DestinationLocationId)
            {
                return MovementOperationResult.Failure(L["Source and destination locations must be different"]);
            }

            var inventories = await _context.Inventory
                .Include(x => x.Product)
                .Include(x => x.Location)
                .Where(x => x.Product.IsActive && x.Location.IsActive &&
                    (
                        (x.ProductId == transferDto.ProductId && x.LocationId == transferDto.LocationId) ||
                        (x.ProductId == transferDto.ProductId && x.LocationId == transferDto.DestinationLocationId)
                    ))
                .ToListAsync(cancellationToken);

            var sourceInventory = inventories.FirstOrDefault(x => x.LocationId == transferDto.LocationId);

            if (sourceInventory == null)
            {
                return MovementOperationResult.Failure(L["Invalid product or location"]);
            }

            // Verificar si hay suficiente stock para la transferencia
            if (sourceInventory.Quantity < transferDto.Quantity)
            {
                return MovementOperationResult.Failure(L["Insufficient stock"]);
            }

            var destinationInventory = inventories.FirstOrDefault(x => x.LocationId == transferDto.DestinationLocationId);

            if (destinationInventory == null)
            {
                destinationInventory = new App.Models.Shop.Inventory
                {
                    ProductId = transferDto.ProductId,
                    LocationId = transferDto.DestinationLocationId,
                    Quantity = 0,
                    IndividualUnits = 0,
                    CreatedBy = _currentUserService.FullName ?? "Unknown",
                    CreatedAt = _dateTime.Now
                };
                _context.Inventory.Add(destinationInventory);
            }

            // Crear el registro de movimiento
            var movement = new InventoryMovement
            {
                ProductId = transferDto.ProductId,
                LocationId = transferDto.LocationId,
                DestinationLocationId = transferDto.DestinationLocationId,
                MovementType = InventoryMovementType.Transfer,
                MovementSubType = transferDto.TransferType,
                Quantity = transferDto.Quantity,
                Reference = transferDto.Reference,
                Document = transferDto.Document,
                Reason = transferDto.Reason,
                PreviousBalance = sourceInventory.Quantity,
                NewBalance = sourceInventory.Quantity - transferDto.Quantity,
                UnitCost = transferDto.UnitCost,
                MovementDate = transferDto.MovementDate ?? _dateTime.Now,
                RelatedParty = transferDto.RelatedParty,
                CreatedBy = _currentUserService.FullName ?? "Unknown",
                CreatedAt = _dateTime.Now
            };

            _context.InventoryMovements.Add(movement);

            // Actualizar cantidades
            decimal newSourceStock = sourceInventory.Quantity - transferDto.Quantity;
            decimal newDestinationStock = destinationInventory.Quantity + transferDto.Quantity;
            
            sourceInventory.Quantity = newSourceStock;
            sourceInventory.ModifiedBy = _currentUserService.FullName;
            destinationInventory.Quantity = newDestinationStock;
            destinationInventory.ModifiedBy = _currentUserService.FullName;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Verificar si se generaron alertas después de la operación
            InventoryAlertInfo? alertInfo = null;
            
            // Alerta en ubicación de origen
            if (sourceInventory.MinStock.HasValue && newSourceStock < sourceInventory.MinStock.Value)
            {
                alertInfo = InventoryAlertInfo.LowStock(
                    sourceInventory.Product.Name,
                    sourceInventory.Location.Name,
                    newSourceStock,
                    sourceInventory.MinStock.Value);
            }

            // Alerta en ubicación de destino
            else if (destinationInventory.MaxStock.HasValue && newDestinationStock > destinationInventory.MaxStock.Value)
            {
                alertInfo = InventoryAlertInfo.OverStock(
                    destinationInventory.Product.Name,
                    destinationInventory.Location.Name,
                    newDestinationStock,
                    destinationInventory.MaxStock.Value);
            }

            // Send email alert if alert was generated
            if (alertInfo != null)
            {
                // Fire and forget email sending to avoid blocking the main operation
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _inventoryAlertEmailService.SendInventoryAlertAsync(alertInfo, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending inventory alert email for product {ProductName} in location {LocationName}",
                            alertInfo.ProductName, alertInfo.LocationName);
                    }
                }, cancellationToken);
            }

            return MovementOperationResult.Successful(_mapper.Map<InventoryMovementDto>(movement), alertInfo);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Concurrency error creating transfer");
            return MovementOperationResult.Failure(L["The inventory was modified by another process"]);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error creating transfer");
            return MovementOperationResult.Failure(ex.Message);
        }
    }

    private async Task<InventoryMovement> CreateMovementRecordInternalAsync(
        ApplicationDbContext context,
        BaseInventoryMovementDto dto,
        string movementType,
        string movementSubType,
        decimal previousBalance,
        decimal newBalance,
        int? destinationLocationId = null,
        CancellationToken cancellationToken = default)
    {
        var movement = new InventoryMovement
        {
            ProductId = dto.ProductId,
            LocationId = dto.LocationId,
            DestinationLocationId = destinationLocationId,
            MovementType = movementType,
            MovementSubType = movementSubType,
            Quantity = dto.Quantity,
            Reference = dto.Reference,
            Document = dto.Document,
            Reason = dto.Reason,
            PreviousBalance = previousBalance,
            NewBalance = newBalance,
            UnitCost = dto.UnitCost,
            MovementDate = dto.MovementDate ?? _dateTime.Now,
            RelatedParty = dto.RelatedParty,
            CreatedBy = _currentUserService.FullName ?? "Unknown",
            CreatedAt = _dateTime.Now
        };

        context.InventoryMovements.Add(movement);
        await context.SaveChangesAsync(cancellationToken);

        return movement;
    }


    public async Task<(int TotalCount, IList<InventoryMovementDto> Items)> GetMovementsAsync(
        int page = 1,
        int pageSize = 10,
        long? productId = null,
        int? locationId = null,
        string? movementType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            IQueryable<InventoryMovement> query = _context.InventoryMovements
                .Include(x => x.Product)
                .Include(x => x.Location)
                .Include(x => x.DestinationLocation)
                .OrderByDescending(x => x.CreatedAt);

            // Apply filters
            if (productId.HasValue)
            {
                query = query.Where(x => x.ProductId == productId.Value);
            }

            if (locationId.HasValue)
            {
                query = query.Where(x => x.LocationId == locationId.Value ||
                                       x.DestinationLocationId == locationId.Value);
            }

            if (!string.IsNullOrEmpty(movementType))
            {
                query = query.Where(x => x.MovementType == movementType);
            }

            if (startDate.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.CreatedAt <= endDate.Value);
            }

            // Get total records count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => _mapper.Map<InventoryMovementDto>(x))
                .ToListAsync(cancellationToken);

            return (totalCount, items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory movements");
            throw;
        }
    }

    public async Task<IList<InventoryAlertDto>> GetStockAlertsAsync(
        int? locationId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var query = _context.Inventory
                .Include(x => x.Product)
                .Include(x => x.Location)
                .Where(x =>
                    x.Product.IsActive &&
                    x.Location.IsActive &&
                    ((x.MinStock.HasValue && x.Quantity < x.MinStock.Value) ||
                     (x.MaxStock.HasValue && x.Quantity > x.MaxStock.Value)));

            if (locationId.HasValue)
            {
                query = query.Where(x => x.LocationId == locationId.Value);
            }

            var alerts = await query
                .Select(x => _mapper.Map<InventoryAlertDto>(x))
                .ToListAsync(cancellationToken);

            return alerts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory alerts");
            throw;
        }
    }

    public async Task<InventoryDto> UpdateInventorySettingsAsync(
        long productId,
        int locationId,
        UpdateInventoryDto updateDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            // Get existing inventory with its relationships
            var inventory = await _context.Inventory
                .Include(x => x.Product)
                    .ThenInclude(x => x.UnitMeasure)
                .Include(x => x.Location)
                .FirstOrDefaultAsync(x =>
                    x.ProductId == productId &&
                    x.LocationId == locationId &&
                    x.Product.IsActive &&
                    x.Location.IsActive,
                    cancellationToken);

            if (inventory == null)
            {
                throw new InvalidOperationException(
                    L["Inventory not found for product {0} in location {1}",
                        productId, locationId]);
            }

            // Validate max stock vs min stock
            if (updateDto.MinStock.HasValue && updateDto.MaxStock.HasValue &&
                updateDto.MinStock.Value > updateDto.MaxStock.Value)
            {
                throw new InvalidOperationException(
                    L["Min stock cannot be greater than max stock"]);
            }

            // Update values
            if (updateDto.MinStock.HasValue)
            {
                inventory.MinStock = updateDto.MinStock.Value;
            }

            if (updateDto.MaxStock.HasValue)
            {
                inventory.MaxStock = updateDto.MaxStock.Value;
            }

            // Update audit fields
            inventory.ModifiedBy = _currentUserService.FullName ?? "Unknown";
            inventory.ModifiedAt = _dateTime.Now;

            await _context.SaveChangesAsync(cancellationToken);

            // Return updated DTO
            return _mapper.Map<InventoryDto>(inventory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating inventory settings for product {ProductId} in location {LocationId}",
                productId, locationId);
            throw;
        }
    }

    public async Task<InventoryOperationResult<InventoryMovementDto>> CreateInitialInventoryAsync(
        InitialInventoryLoadDto loadDto,
        CancellationToken cancellationToken = default)
    {
        if (loadDto.MinStock.HasValue && loadDto.MaxStock.HasValue &&
            loadDto.MinStock.Value > loadDto.MaxStock.Value)
        {
            return InventoryOperationResult<InventoryMovementDto>.Error(
                L["Min stock cannot be greater than max stock"]);
        }

        if (loadDto.Quantity < 0)
        {
            return InventoryOperationResult<InventoryMovementDto>.Error(
                L["Quantity must be greater than or equal to 0"]);
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Verify that product and warehouse exist and are active
                var product = await context.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == loadDto.ProductId && x.IsActive,
                        cancellationToken);

                if (product == null)
                {
                    return InventoryOperationResult<InventoryMovementDto>.Error(
                        L["Product not found or inactive"]);
                }

                var location = await context.Locations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == loadDto.LocationId && x.IsActive,
                        cancellationToken);

                if (location == null)
                {
                    return InventoryOperationResult<InventoryMovementDto>.Error(
                        L["Location not found or inactive"]);
                }

                // Check if inventory already exists
                var existingInventory = await context.Inventory
                    .AnyAsync(x => x.ProductId == loadDto.ProductId &&
                                x.LocationId == loadDto.LocationId,
                        cancellationToken);

                if (existingInventory)
                {
                    return InventoryOperationResult<InventoryMovementDto>.Error(
                        L["Product already has inventory in this location"]);
                }

                // Calculate initial individual units
                decimal individualUnits = product.IsPartialSaleAllowed && product.Content > 0
                    ? loadDto.Quantity * product.Content
                    : loadDto.Quantity;

                // Create inventory record
                var inventory = new App.Models.Shop.Inventory
                {
                    ProductId = loadDto.ProductId,
                    LocationId = loadDto.LocationId,
                    Quantity = loadDto.Quantity,
                    IndividualUnits = individualUnits,
                    MinStock = loadDto.MinStock,
                    MaxStock = loadDto.MaxStock,
                    CreatedBy = _currentUserService.FullName ?? "Unknown",
                    CreatedAt = _dateTime.Now
                };

                context.Inventory.Add(inventory);
                await context.SaveChangesAsync(cancellationToken);

                // Create movement record using the same context
                var movement = await CreateMovementRecordAsync(
                    context,
                    loadDto,
                    InventoryMovementType.InitialLoad,
                    InventoryMovementSubType.InitialCount,
                    0,
                    loadDto.Quantity,
                    0,
                    individualUnits,
                    cancellationToken: cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return InventoryOperationResult<InventoryMovementDto>.Ok(
                    _mapper.Map<InventoryMovementDto>(movement));
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating initial inventory for product {ProductId} in location {LocationId}",
                loadDto.ProductId, loadDto.LocationId);
            return InventoryOperationResult<InventoryMovementDto>.Error(
                L["Error creating initial inventory"]);
        }
    }

    public async Task<InventoryOperationResult<List<BulkInventoryLoadResultDto>>> CreateBulkInitialInventoryAsync(
        BulkInitialLoadRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var results = new List<BulkInventoryLoadResultDto>();
        var validItems = new List<(Product Product, BulkInventoryLoadDto Item)>();

        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            // Validate location
            var location = await _context.Locations
                .FirstOrDefaultAsync(x => x.Id == request.LocationId && x.IsActive,
                    cancellationToken);

            if (location == null)
            {
                return InventoryOperationResult<List<BulkInventoryLoadResultDto>>.Error(
                    L["Invalid or inactive location"]);
            }

            // Initial validations
            foreach (var item in request.Items)
            {
                try
                {
                    // Validate quantities
                    if (item.Quantity <= 0)
                    {
                        results.Add(new BulkInventoryLoadResultDto
                        {
                            ProductCode = item.ProductCode,
                            Quantity = item.Quantity,
                            Success = false,
                            Error = L["Quantity must be greater than 0"]
                        });
                        continue;
                    }

                    // Validate min/max stock
                    if (item.MinStock.HasValue && item.MaxStock.HasValue &&
                        item.MinStock.Value > item.MaxStock.Value)
                    {
                        results.Add(new BulkInventoryLoadResultDto
                        {
                            ProductCode = item.ProductCode,
                            Quantity = item.Quantity,
                            MinStock = item.MinStock,
                            MaxStock = item.MaxStock,
                            Success = false,
                            Error = L["Min stock cannot be greater than max stock"]
                        });
                        continue;
                    }

                    // Search and validate product
                    var product = await _context.Products
                        .Include(p => p.UnitMeasure)
                        .FirstOrDefaultAsync(x =>
                            x.Code == item.ProductCode && x.IsActive,
                            cancellationToken);

                    if (product == null)
                    {
                        results.Add(new BulkInventoryLoadResultDto
                        {
                            ProductCode = item.ProductCode,
                            Success = false,
                            Error = L["Product not found or inactive"]
                        });
                        continue;
                    }

                    // Check if inventory already exists
                    var existingInventory = await _context.Inventory
                        .AnyAsync(x =>
                            x.ProductId == product.Id &&
                            x.LocationId == request.LocationId,
                            cancellationToken);

                    if (existingInventory)
                    {
                        results.Add(new BulkInventoryLoadResultDto
                        {
                            ProductCode = item.ProductCode,
                            ProductName = product.Name,
                            Success = false,
                            Error = L["Product already has inventory in this location"]
                        });
                        continue;
                    }

                    validItems.Add((product, item));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating item {ProductCode}", item.ProductCode);
                    results.Add(new BulkInventoryLoadResultDto
                    {
                        ProductCode = item.ProductCode,
                        Success = false,
                        Error = L["Error validating item"]
                    });
                }
            }

            // If there are validation errors, return
            if (results.Any())
            {
                return InventoryOperationResult<List<BulkInventoryLoadResultDto>>.Error(
                    L["Validation errors found"], results);
            }

            // Process valid items in a transaction
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (var (product, item) in validItems)
                {
                    // Calculate initial individual units
                    decimal individualUnits = product.IsPartialSaleAllowed && product.Content > 0
                        ? item.Quantity * product.Content
                        : item.Quantity;

                    // Create inventory record
                    var inventory = new Models.Shop.Inventory
                    {
                        ProductId = product.Id,
                        LocationId = request.LocationId,
                        Quantity = item.Quantity,
                        IndividualUnits = individualUnits,
                        MinStock = item.MinStock,
                        MaxStock = item.MaxStock,
                        CreatedBy = _currentUserService.FullName ?? "Unknown",
                        CreatedAt = _dateTime.Now
                    };

                    _context.Inventory.Add(inventory);
                    await _context.SaveChangesAsync(cancellationToken);

                    // Create initial movement
                    var movement = new InventoryMovement
                    {
                        ProductId = product.Id,
                        LocationId = request.LocationId,
                        MovementType = InventoryMovementType.InitialLoad,
                        MovementSubType = InventoryMovementSubType.InitialCount,
                        MovementDate = _dateTime.Now,
                        Quantity = item.Quantity,
                        IndividualUnits = individualUnits,
                        Reference = L["Bulk Initial Load"],
                        Reason = L["Initial inventory setup - Bulk import"],
                        PreviousBalance = 0,
                        NewBalance = item.Quantity,
                        PreviousIndividualBalance = 0,
                        NewIndividualBalance = individualUnits,
                        CreatedBy = _currentUserService.FullName ?? "Unknown",
                        CreatedAt = _dateTime.Now
                    };

                    _context.InventoryMovements.Add(movement);
                    await _context.SaveChangesAsync(cancellationToken);

                    results.Add(new BulkInventoryLoadResultDto
                    {
                        ProductCode = product.Code,
                        ProductName = product.Name,
                        Quantity = item.Quantity,
                        MinStock = item.MinStock,
                        MaxStock = item.MaxStock,
                        Success = true
                    });
                }

                await transaction.CommitAsync(cancellationToken);
                return InventoryOperationResult<List<BulkInventoryLoadResultDto>>.Ok(results);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bulk initial inventory");
            return InventoryOperationResult<List<BulkInventoryLoadResultDto>>.Error(
                L["Error processing bulk initial inventory"]);
        }
    }


    private async Task<InventoryMovement> CreateMovementRecordAsync(
        ApplicationDbContext context,
        BaseInventoryMovementDto dto,
        string movementType,
        string movementSubType,
        decimal previousBalance,
        decimal newBalance,
        decimal previousIndividualBalance,
        decimal newIndividualBalance,
        int? destinationLocationId = null,
        CancellationToken cancellationToken = default)
    {
        // Calculate individual units for the movement quantity
        decimal individualUnits;

        // Get product to determine calculation method
        var product = await context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == dto.ProductId, cancellationToken);

        if (product != null && product.IsPartialSaleAllowed && product.Content > 0)
        {
            individualUnits = dto.Quantity * product.Content;
        }
        else
        {
            individualUnits = dto.Quantity;
        }

        var movement = new InventoryMovement
        {
            ProductId = dto.ProductId,
            LocationId = dto.LocationId,
            DestinationLocationId = destinationLocationId,
            MovementType = movementType,
            MovementSubType = movementSubType,
            Quantity = dto.Quantity,
            IndividualUnits = individualUnits,
            Reference = dto.Reference,
            Document = dto.Document,
            Reason = dto.Reason,
            PreviousBalance = previousBalance,
            NewBalance = newBalance,
            PreviousIndividualBalance = previousIndividualBalance,
            NewIndividualBalance = newIndividualBalance,
            UnitCost = dto.UnitCost,
            MovementDate = dto.MovementDate ?? _dateTime.Now,
            RelatedParty = dto.RelatedParty,
            CreatedBy = _currentUserService.FullName ?? "Unknown",
            CreatedAt = _dateTime.Now
        };

        context.InventoryMovements.Add(movement);
        await context.SaveChangesAsync(cancellationToken);

        return movement;
    }

    private async Task<App.Models.Shop.Inventory> ValidateAndGetInventoryAsync(
        long productId,
        int locationId,
        CancellationToken cancellationToken = default)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var inventory = await _context.Inventory
            .Include(x => x.Product)
            .Include(x => x.Location)
            .Where(x => x.ProductId == productId &&
                       x.LocationId == locationId &&
                       x.Product.IsActive &&
                       x.Location.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (inventory == null)
            throw new InvalidOperationException(L["Invalid product or location"]);

        return inventory;
    }

    private void ValidateStockLevelsAsync(
        Models.Shop.Inventory inventory,
        decimal newQuantity,
        bool isAddition)
    {
        // Validar stock suficiente para salidas
        if (!isAddition && inventory.Quantity < newQuantity)
        {
            throw new InvalidOperationException(L["Insufficient stock"]);
        }

        // Validar que no quede negativo
        var finalBalance = isAddition ?
            inventory.Quantity + newQuantity :
            inventory.Quantity - newQuantity;

        if (finalBalance < 0)
        {
            throw new InvalidOperationException(L["Movement would result in negative stock"]);
        }
    }


    public async Task<MovementOperationResult> CreateInventoryAdjustmentAsync(
        CreateInventoryAdjustmentDto adjustmentDto,
        CancellationToken cancellationToken = default)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var inventory = await _context.Inventory
                .Include(x => x.Product)
                .Include(x => x.Location)
                .Where(x => x.ProductId == adjustmentDto.ProductId &&
                        x.LocationId == adjustmentDto.LocationId &&
                        x.Product.IsActive &&
                        x.Location.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (inventory == null)
                return MovementOperationResult.Failure(L["Invalid product or location"]);

            // Verificar que la nueva cantidad no sea negativa
            if (adjustmentDto.NewQuantity < 0)
            {
                return MovementOperationResult.Failure(L["New quantity cannot be negative"]);
            }

            // Calcular el ajuste (positivo o negativo)
            var adjustment = adjustmentDto.NewQuantity - inventory.Quantity;
            var previousBalance = inventory.Quantity;

            // Crear el registro de movimiento
            var movement = new InventoryMovement
            {
                ProductId = adjustmentDto.ProductId,
                LocationId = adjustmentDto.LocationId,
                MovementType = InventoryMovementType.Adjustment,
                MovementSubType = adjustmentDto.AdjustmentType,
                Quantity = Math.Abs(adjustment), // Almacenar valor absoluto del ajuste
                Reference = adjustmentDto.Reference,
                Reason = adjustmentDto.Reason,
                PreviousBalance = previousBalance,
                NewBalance = adjustmentDto.NewQuantity,
                MovementDate = adjustmentDto.AdjustmentDate ?? _dateTime.Now,
                CreatedBy = _currentUserService.FullName ?? "Unknown",
                CreatedAt = _dateTime.Now
            };

            _context.InventoryMovements.Add(movement);
            
            // Actualizar inventario
            inventory.Quantity = adjustmentDto.NewQuantity;
            inventory.ModifiedBy = _currentUserService.FullName;
            inventory.ModifiedAt = _dateTime.Now;
            
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Verificar si se generaron alertas después de la operación
            InventoryAlertInfo? alertInfo = null;
            
            if (inventory.MinStock.HasValue && adjustmentDto.NewQuantity < inventory.MinStock.Value)
            {
                alertInfo = InventoryAlertInfo.LowStock(
                    inventory.Product.Name,
                    inventory.Location.Name,
                    adjustmentDto.NewQuantity,
                    inventory.MinStock.Value);
            }
            else if (inventory.MaxStock.HasValue && adjustmentDto.NewQuantity > inventory.MaxStock.Value)
            {
                alertInfo = InventoryAlertInfo.OverStock(
                    inventory.Product.Name,
                    inventory.Location.Name,
                    adjustmentDto.NewQuantity,
                    inventory.MaxStock.Value);
            }

            // Send email alert if alert was generated
            if (alertInfo != null)
            {
                // Fire and forget email sending to avoid blocking the main operation
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _inventoryAlertEmailService.SendInventoryAlertAsync(alertInfo, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending inventory alert email for product {ProductName} in location {LocationName}",
                            alertInfo.ProductName, alertInfo.LocationName);
                    }
                }, cancellationToken);
            }

            return MovementOperationResult.Successful(_mapper.Map<InventoryMovementDto>(movement), alertInfo);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Concurrency error creating inventory adjustment");
            return MovementOperationResult.Failure(L["The inventory was modified by another process"]);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error creating inventory adjustment");
            return MovementOperationResult.Failure(ex.Message);
        }
    }

    #region Helper Methods for Unit Conversion

    /// <summary>
    /// Gets the available individual units (e.g., liters) from container units stored in inventory
    /// </summary>
    /// <param name="inventory">Inventory record with Product included</param>
    /// <returns>Available individual units</returns>
    private decimal GetAvailableIndividualUnits(App.Models.Shop.Inventory inventory)
    {
        // If product allows partial sales and has content, calculate individual units
        if (inventory.Product.IsPartialSaleAllowed && inventory.Product.Content > 0)
        {
            // inventory.Quantity = containers, Product.Content = units per container
            return inventory.Quantity * inventory.Product.Content;
        }

        // For products without partial sales, quantity is already in individual units
        return inventory.Quantity;
    }

    /// <summary>
    /// Converts individual units (e.g., liters) to container units for storage
    /// </summary>
    /// <param name="product">Product with IsPartialSaleAllowed and Content</param>
    /// <param name="individualUnits">Individual units to convert</param>
    /// <returns>Container units</returns>
    private decimal ConvertToContainerUnits(App.Models.Shop.Product product, decimal individualUnits)
    {
        // If product allows partial sales and has content, convert to container units
        if (product.IsPartialSaleAllowed && product.Content > 0)
        {
            return individualUnits / product.Content;
        }

        // For products without partial sales, individual units are the same as container units
        return individualUnits;
    }

    #endregion
}