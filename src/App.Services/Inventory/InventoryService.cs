using App.Core.Constants;
using App.Core.DTOs.Inventory;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Services.Shop;
using App.Shared.Services;

using AutoMapper;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Inventory;

public class InventoryService : IContextualInventoryService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<InventoryService> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStringLocalizer<InventoryService> L;
    private readonly IDateTime _dateTime;
    private readonly IMapper _mapper;
    private readonly IInventoryAlertEmailService _inventoryAlertEmailService;
    private readonly ICompanySettingsService _companySettingsService;
    private readonly IPdfService _pdfService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IDocumentSequenceService _documentSequenceService;

    public InventoryService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<InventoryService> logger,
        ICurrentUserService currentUserService,
        IStringLocalizer<InventoryService> localizer,
        IDateTime dateTime,
        IInventoryAlertEmailService inventoryAlertEmailService,
        ICompanySettingsService companySettingsService,
        IPdfService pdfService,
        IEmailTemplateService emailTemplateService,
        IDocumentSequenceService documentSequenceService)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _currentUserService = currentUserService;
        L = localizer;
        _dateTime = dateTime;
        _inventoryAlertEmailService = inventoryAlertEmailService;
        _companySettingsService = companySettingsService;
        _pdfService = pdfService;
        _emailTemplateService = emailTemplateService;
        _documentSequenceService = documentSequenceService;
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

        return inventory.GetAvailableIndividualUnits() >= quantity;
    }

    public async Task<MovementOperationResult> CreateMovementAsync(
        CreateInventoryMovementDto createDto,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await CreateMovementCoreAsync(createDto, context, cancellationToken);
                if (result.Success)
                    await transaction.CommitAsync(cancellationToken);
                else
                    await transaction.RollbackAsync(cancellationToken);
                return result;
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
        });
    }

    public async Task<MovementOperationResult> CreateMovementAsync(
        CreateInventoryMovementDto createDto,
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        return await CreateMovementCoreAsync(createDto, context, cancellationToken);
    }

    private async Task<MovementOperationResult> CreateMovementCoreAsync(
        CreateInventoryMovementDto createDto,
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        var inventory = await context.Inventory
            .Include(x => x.Product)
            .Include(x => x.Location)
            .Where(x => x.ProductId == createDto.ProductId &&
                    x.LocationId == createDto.LocationId &&
                    x.Product.IsActive &&
                    x.Location.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        var isAddition = createDto.MovementType is
            InventoryMovementType.StockIn or
            InventoryMovementType.Purchase or
            InventoryMovementType.Return;

        if (inventory == null)
        {
            if (!isAddition)
                return MovementOperationResult.Failure(L["Invalid product or location"]);

            var product = await context.Products
                .FirstOrDefaultAsync(p => p.Id == createDto.ProductId && p.IsActive, cancellationToken);
            var location = await context.Locations
                .FirstOrDefaultAsync(l => l.Id == createDto.LocationId && l.IsActive, cancellationToken);

            if (product == null || location == null)
                return MovementOperationResult.Failure(L["Invalid product or location"]);

            inventory = new App.Models.Shop.Inventory
            {
                ProductId = createDto.ProductId,
                LocationId = createDto.LocationId,
                Quantity = 0,
                Product = product,
                Location = location,
                CreatedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown",
                CreatedAt = _dateTime.Now
            };
            context.Inventory.Add(inventory);
            await context.SaveChangesAsync(cancellationToken);
        }

        decimal quantityToMove = createDto.Quantity;
        decimal individualUnitsToMove = createDto.Quantity;

        if (createDto.MovementType == InventoryMovementType.Sale && inventory.Product.IsPartialSaleAllowed)
            quantityToMove = ConvertToContainerUnits(inventory.Product, createDto.Quantity);
        else if (createDto.MovementType != InventoryMovementType.Sale && inventory.Product.IsPartialSaleAllowed)
            individualUnitsToMove = createDto.Quantity * inventory.Product.Content;

        decimal currentQuantity = inventory.Quantity;
        decimal currentIndividualUnits = inventory.GetAvailableIndividualUnits();

        if (!isAddition && currentQuantity < quantityToMove)
            return MovementOperationResult.Failure(L["Insufficient stock"]);

        decimal newQuantityBalance, newIndividualBalance;

        if (createDto.MovementType == InventoryMovementType.Adjustment)
        {
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

        if (newQuantityBalance < 0 || newIndividualBalance < 0)
            return MovementOperationResult.Failure(L["Movement would result in negative stock"]);

        var movement = new InventoryMovement
        {
            ProductId = createDto.ProductId,
            LocationId = createDto.LocationId,
            MovementType = createDto.MovementType,
            MovementSubType = createDto.MovementSubType,
            Quantity = quantityToMove,
            IndividualUnits = individualUnitsToMove,
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
            CreatedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown",
            CreatedAt = _dateTime.Now
        };

        context.InventoryMovements.Add(movement);
        inventory.Quantity = newQuantityBalance;
        inventory.ModifiedBy = await _currentUserService.GetFullNameAsync();

        await context.SaveChangesAsync(cancellationToken);

        InventoryAlertInfo? alertInfo = null;
        if (inventory.MinStock.HasValue && newQuantityBalance < inventory.MinStock.Value)
        {
            alertInfo = InventoryAlertInfo.LowStock(
                inventory.Product.Name, inventory.Location.Name,
                newQuantityBalance, inventory.MinStock.Value);
        }
        else if (inventory.MaxStock.HasValue && newQuantityBalance > inventory.MaxStock.Value)
        {
            alertInfo = InventoryAlertInfo.OverStock(
                inventory.Product.Name, inventory.Location.Name,
                newQuantityBalance, inventory.MaxStock.Value);
        }

        if (alertInfo != null)
        {
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


    public async Task<MovementOperationResult> CreateTransferAsync(
        CreateInventoryTransferDto transferDto,
        CancellationToken cancellationToken = default)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var strategy = _context.Database.CreateExecutionStrategy();
        var result = await strategy.ExecuteAsync(async () =>
        {
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
                        CreatedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown",
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
                    CreatedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown",
                    CreatedAt = _dateTime.Now
                };

                _context.InventoryMovements.Add(movement);

                // Actualizar cantidades
                decimal newSourceStock = sourceInventory.Quantity - transferDto.Quantity;
                decimal newDestinationStock = destinationInventory.Quantity + transferDto.Quantity;

                sourceInventory.Quantity = newSourceStock;
                sourceInventory.ModifiedBy = await _currentUserService.GetFullNameAsync();
                destinationInventory.Quantity = newDestinationStock;
                destinationInventory.ModifiedBy = await _currentUserService.GetFullNameAsync();

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
        });

        // Sent once, outside the retry delegate, so a transient-fault retry can't duplicate the alert email.
        if (result.Success && result.Alert != null)
        {
            var alertInfo = result.Alert;
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

        return result;
    }

    public async Task<InventoryOperationResult<BulkTransferResultDto>> CreateBulkTransferAsync(
        CreateBulkInventoryTransferDto transferDto,
        CancellationToken cancellationToken = default)
    {
        if (transferDto.LocationId == transferDto.DestinationLocationId)
        {
            return InventoryOperationResult<BulkTransferResultDto>.Error(
                L["Source and destination locations must be different"]);
        }

        if (transferDto.Lines.Count == 0)
        {
            return InventoryOperationResult<BulkTransferResultDto>.Error(
                L["At least one line is required"]);
        }

        if (transferDto.Lines.Select(l => l.ProductId).Distinct().Count() != transferDto.Lines.Count)
        {
            return InventoryOperationResult<BulkTransferResultDto>.Error(
                L["Duplicate products are not allowed in the same transfer"]);
        }

        await using var _context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var location = await _context.Locations
            .FirstOrDefaultAsync(x => x.Id == transferDto.LocationId && x.IsActive, cancellationToken);
        var destinationLocation = await _context.Locations
            .FirstOrDefaultAsync(x => x.Id == transferDto.DestinationLocationId && x.IsActive, cancellationToken);

        if (location == null || destinationLocation == null)
        {
            return InventoryOperationResult<BulkTransferResultDto>.Error(L["Invalid or inactive location"]);
        }

        var productIds = transferDto.Lines.Select(l => l.ProductId).ToList();

        var inventories = await _context.Inventory
            .Include(x => x.Product)
            .Include(x => x.Location)
            .Where(x => productIds.Contains(x.ProductId) &&
                (x.LocationId == transferDto.LocationId || x.LocationId == transferDto.DestinationLocationId))
            .ToListAsync(cancellationToken);

        var lineErrors = new List<BulkTransferLineResultDto>();
        var validLines = new List<(BulkTransferLineDto Line, App.Models.Shop.Inventory Source)>();

        foreach (var line in transferDto.Lines)
        {
            var source = inventories.FirstOrDefault(x =>
                x.ProductId == line.ProductId && x.LocationId == transferDto.LocationId);

            if (source == null || !source.Product.IsActive)
            {
                lineErrors.Add(new BulkTransferLineResultDto
                {
                    ProductId = line.ProductId,
                    ProductCode = source?.Product.Code ?? line.ProductId.ToString(),
                    ProductName = source?.Product.Name ?? "-",
                    Quantity = line.Quantity,
                    Success = false,
                    Error = L["Invalid product or location"]
                });
                continue;
            }

            if (line.Quantity <= 0)
            {
                lineErrors.Add(new BulkTransferLineResultDto
                {
                    ProductId = line.ProductId,
                    ProductCode = source.Product.Code,
                    ProductName = source.Product.Name,
                    Quantity = line.Quantity,
                    Success = false,
                    Error = L["Quantity must be greater than zero"]
                });
                continue;
            }

            if (source.Quantity < line.Quantity)
            {
                lineErrors.Add(new BulkTransferLineResultDto
                {
                    ProductId = line.ProductId,
                    ProductCode = source.Product.Code,
                    ProductName = source.Product.Name,
                    Quantity = line.Quantity,
                    Success = false,
                    Error = L["Insufficient stock"]
                });
                continue;
            }

            validLines.Add((line, source));
        }

        if (lineErrors.Count > 0)
        {
            // All-or-nothing: at least one line failed validation, so nothing is committed.
            return InventoryOperationResult<BulkTransferResultDto>.Error(
                L["One or more lines failed validation; no transfer was applied"],
                new BulkTransferResultDto
                {
                    LocationId = transferDto.LocationId,
                    LocationName = location.Name,
                    DestinationLocationId = transferDto.DestinationLocationId,
                    DestinationLocationName = destinationLocation.Name,
                    TransferType = transferDto.TransferType,
                    Reason = transferDto.Reason,
                    Reference = transferDto.Reference,
                    Lines = lineErrors
                });
        }

        var batchId = Guid.NewGuid();
        var movementDate = _dateTime.Now;
        var createdBy = await _currentUserService.GetFullNameAsync() ?? "Unknown";

        var strategy = _context.Database.CreateExecutionStrategy();
        var result = await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var batchNumber = await _documentSequenceService.GetNextNumberAsync(
                    _context, "InventoryTransferBatch", "TRF", movementDate.Year);

                var destinationByProduct = inventories
                    .Where(x => x.LocationId == transferDto.DestinationLocationId)
                    .ToDictionary(x => x.ProductId);

                var lineResults = new List<BulkTransferLineResultDto>();

                foreach (var (line, source) in validLines)
                {
                    if (!destinationByProduct.TryGetValue(line.ProductId, out var destination))
                    {
                        destination = new App.Models.Shop.Inventory
                        {
                            ProductId = line.ProductId,
                            LocationId = transferDto.DestinationLocationId,
                            Quantity = 0,
                            CreatedBy = createdBy,
                            CreatedAt = movementDate
                        };
                        _context.Inventory.Add(destination);
                        destinationByProduct[line.ProductId] = destination;
                    }

                    var previousSourceBalance = source.Quantity;
                    var newSourceBalance = source.Quantity - line.Quantity;
                    var newDestinationBalance = destination.Quantity + line.Quantity;

                    source.Quantity = newSourceBalance;
                    source.ModifiedBy = createdBy;
                    destination.Quantity = newDestinationBalance;
                    destination.ModifiedBy = createdBy;

                    var movement = new InventoryMovement
                    {
                        ProductId = line.ProductId,
                        LocationId = transferDto.LocationId,
                        DestinationLocationId = transferDto.DestinationLocationId,
                        MovementType = InventoryMovementType.Transfer,
                        MovementSubType = transferDto.TransferType,
                        Quantity = line.Quantity,
                        Reference = transferDto.Reference,
                        Reason = transferDto.Reason,
                        PreviousBalance = previousSourceBalance,
                        NewBalance = newSourceBalance,
                        MovementDate = movementDate,
                        BatchId = batchId,
                        BatchNumber = batchNumber,
                        CreatedBy = createdBy,
                        CreatedAt = movementDate
                    };

                    _context.InventoryMovements.Add(movement);

                    lineResults.Add(new BulkTransferLineResultDto
                    {
                        ProductId = line.ProductId,
                        ProductCode = source.Product.Code,
                        ProductName = source.Product.Name,
                        Quantity = line.Quantity,
                        PreviousBalance = previousSourceBalance,
                        NewBalance = newSourceBalance,
                        Success = true
                    });
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return InventoryOperationResult<BulkTransferResultDto>.Ok(new BulkTransferResultDto
                {
                    BatchId = batchId,
                    BatchNumber = batchNumber,
                    LocationId = transferDto.LocationId,
                    LocationName = location.Name,
                    DestinationLocationId = transferDto.DestinationLocationId,
                    DestinationLocationName = destinationLocation.Name,
                    TransferType = transferDto.TransferType,
                    Reason = transferDto.Reason,
                    Reference = transferDto.Reference,
                    MovementDate = movementDate,
                    CreatedBy = createdBy,
                    Lines = lineResults
                });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Concurrency error creating bulk transfer");
                return InventoryOperationResult<BulkTransferResultDto>.Error(
                    L["The inventory was modified by another process"]);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error creating bulk transfer");
                return InventoryOperationResult<BulkTransferResultDto>.Error(ex.Message);
            }
        });

        return result;
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
            CreatedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown",
            CreatedAt = _dateTime.Now
        };

        context.InventoryMovements.Add(movement);
        await context.SaveChangesAsync(cancellationToken);

        return movement;
    }

    public async Task<byte[]> GenerateBulkTransferPdfAsync(
        Guid batchId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var movements = await context.InventoryMovements
            .Include(x => x.Product)
            .Include(x => x.Location)
            .Include(x => x.DestinationLocation)
            .Where(x => x.BatchId == batchId)
            .OrderBy(x => x.Id)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (movements.Count == 0)
        {
            throw new InvalidOperationException($"Bulk transfer batch {batchId} not found");
        }

        var first = movements[0];
        var companySettings = await _companySettingsService.GetSettingsAsync();
        var (logoBytes, logoMime) = await _emailTemplateService.GetStaticFileBytesAsync("images/logo.webp");
        var logoBase64 = logoBytes.Length > 0
            ? $"data:{logoMime};base64,{Convert.ToBase64String(logoBytes)}"
            : string.Empty;

        var transferTypeDisplay = first.MovementSubType switch
        {
            InventoryMovementSubType.RushTransfer => L["Rush Transfer"],
            InventoryMovementSubType.Rebalancing => L["Stock Rebalancing"],
            _ => L["Standard Transfer"]
        };

        var model = new BulkTransferPdfDto
        {
            CompanyName = companySettings?.CompanyName ?? "Cleeny",
            LogoBase64 = logoBase64,
            BatchId = batchId,
            BatchNumber = first.BatchNumber ?? batchId.ToString(),
            LocationName = first.Location.Name,
            DestinationLocationName = first.DestinationLocation?.Name ?? "-",
            TransferTypeDisplay = transferTypeDisplay,
            Reason = first.Reason,
            Reference = first.Reference,
            MovementDate = first.MovementDate,
            CreatedBy = first.CreatedBy,
            Lines = movements.Select(m => new BulkTransferPdfLineDto
            {
                ProductCode = m.Product.Code,
                ProductName = m.Product.Name,
                Quantity = m.Quantity,
                PreviousBalance = m.PreviousBalance,
                NewBalance = m.NewBalance
            }).ToList()
        };

        return await _pdfService.GeneratePdfFromViewAsync(
            "~/Views/InventoryTransfers/BulkTransferDocument.cshtml", model, cancellationToken);
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
                // Auto-create inventory at 0 so min/max can be set before any stock movement
                var product = await _context.Products.Include(p => p.UnitMeasure)
                    .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive, cancellationToken);
                var location = await _context.Locations
                    .FirstOrDefaultAsync(l => l.Id == locationId && l.IsActive, cancellationToken);

                if (product == null || location == null)
                    throw new InvalidOperationException(
                        L["Inventory not found for product {0} in location {1}", productId, locationId]);

                var now = _dateTime.Now;
                var user = await _currentUserService.GetFullNameAsync() ?? "Unknown";
                inventory = new App.Models.Shop.Inventory
                {
                    ProductId = productId,
                    LocationId = locationId,
                    Quantity = 0,
                    Product = product,
                    Location = location,
                    CreatedBy = user,
                    CreatedAt = now,
                    ModifiedBy = user,
                    ModifiedAt = now
                };
                _context.Inventory.Add(inventory);
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
            inventory.ModifiedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown";
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
            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
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

                    if (!product.RequiresInventory)
                    {
                        return InventoryOperationResult<InventoryMovementDto>.Error(
                            L["This product does not require inventory tracking"]);
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
                        MinStock = loadDto.MinStock,
                        MaxStock = loadDto.MaxStock,
                        CreatedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown",
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
            });
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
                    if (item.Quantity < 0)
                    {
                        results.Add(new BulkInventoryLoadResultDto
                        {
                            ProductCode = item.ProductCode,
                            Quantity = item.Quantity,
                            Success = false,
                            Error = L["Quantity cannot be negative"]
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

            // Process valid items in a transaction (even if some items had validation errors).
            // Entries go into a fresh local list (not the outer `results`) so a transient-fault
            // retry re-runs this block without duplicating already-recorded entries.
            var strategy = _context.Database.CreateExecutionStrategy();
            var entryResults = await strategy.ExecuteAsync(async () =>
            {
                var localResults = new List<BulkInventoryLoadResultDto>();
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
                            MinStock = item.MinStock,
                            MaxStock = item.MaxStock,
                            CreatedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown",
                            CreatedAt = _dateTime.Now
                        };

                        _context.Inventory.Add(inventory);
                        await _context.SaveChangesAsync(cancellationToken);

                        // Create initial movement only if quantity > 0
                        // (DB constraint requires Quantity > 0 on movements)
                        if (item.Quantity > 0)
                        {
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
                                CreatedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown",
                                CreatedAt = _dateTime.Now
                            };

                            _context.InventoryMovements.Add(movement);
                            await _context.SaveChangesAsync(cancellationToken);
                        }

                        localResults.Add(new BulkInventoryLoadResultDto
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
                    return localResults;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            results.AddRange(entryResults);
            var hasErrors = results.Any(r => !r.Success);
            return hasErrors
                ? InventoryOperationResult<List<BulkInventoryLoadResultDto>>.Error(
                    L["Some items could not be processed"], results)
                : InventoryOperationResult<List<BulkInventoryLoadResultDto>>.Ok(results);
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
            CreatedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown",
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
        var strategy = _context.Database.CreateExecutionStrategy();
        var result = await strategy.ExecuteAsync(async () =>
        {
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

                if (adjustment == 0)
                    return MovementOperationResult.Failure(L["New quantity is the same as current stock"]);

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
                    CreatedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown",
                    CreatedAt = _dateTime.Now
                };

                _context.InventoryMovements.Add(movement);

                // Actualizar inventario
                inventory.Quantity = adjustmentDto.NewQuantity;
                inventory.ModifiedBy = await _currentUserService.GetFullNameAsync();
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
        });

        // Sent once, outside the retry delegate, so a transient-fault retry can't duplicate the alert email.
        if (result.Success && result.Alert != null)
        {
            var alertInfo = result.Alert;
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

        return result;
    }

    #region Helper Methods for Unit Conversion

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