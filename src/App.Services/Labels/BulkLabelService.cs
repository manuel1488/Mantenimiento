using AutoMapper;

using App.Core.Common;
using App.Core.DTOs.Label;
using App.Core.Interfaces;
using App.Core.Interfaces.Settings;
using App.Core.Interfaces.Shop;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Services.Settings;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Labels;

public class BulkLabelService : IBulkLabelService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IPdfService _pdfService;
    private readonly ITicketService _ticketService;
    private readonly ILabelSettingsService _labelSettingsService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly IMapper _mapper;
    private readonly ILogger<BulkLabelService> _logger;
    private readonly IStringLocalizer<BulkLabelService> _localizer;
    private readonly BarcodeGeneratorService _barcodeGenerator;
    private readonly ITaxRateService _taxRateService;
    private readonly IProductWholesalePriceService _wholesalePriceService;

    public BulkLabelService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IPdfService pdfService,
        ITicketService ticketService,
        ILabelSettingsService labelSettingsService,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        IMapper mapper,
        ILogger<BulkLabelService> logger,
        IStringLocalizer<BulkLabelService> localizer,
        BarcodeGeneratorService barcodeGenerator,
        ITaxRateService taxRateService,
        IProductWholesalePriceService wholesalePriceService)
    {
        _contextFactory = contextFactory;
        _pdfService = pdfService;
        _ticketService = ticketService;
        _labelSettingsService = labelSettingsService;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        _mapper = mapper;
        _logger = logger;
        _localizer = localizer;
        _barcodeGenerator = barcodeGenerator;
        _taxRateService = taxRateService;
        _wholesalePriceService = wholesalePriceService;
    }

    public async Task<Result<BulkLabelJobDto>> CreateAsync(
        CreateBulkLabelJobDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var product = await context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == dto.ProductId, cancellationToken);

            if (product == null)
                return Result<BulkLabelJobDto>.Failure(_localizer["Product not found"]);

            var currentUser = _currentUserService.UserId ?? "System";
            var now = _dateTime.Now;

            // Calculate effective unit price — apply wholesale tier if applicable
            var effectiveUnitPrice = product.Price;
            var wholesaleResult = await _wholesalePriceService.GetWholesalePricesForProductAsync(dto.ProductId);
            if (wholesaleResult.IsSuccess && wholesaleResult.Value.Count > 0)
            {
                var applicable = wholesaleResult.Value
                    .Where(w => w.IsActive && dto.Quantity >= w.MinQuantity)
                    .Where(w => w.FixedPrice is > 0 || w.DiscountPercentage > 0)
                    .OrderByDescending(w => w.MinQuantity)
                    .FirstOrDefault();

                if (applicable != null)
                {
                    effectiveUnitPrice = applicable.FixedPrice is > 0
                        ? applicable.FixedPrice.Value
                        : Math.Round(product.Price * (1 - applicable.DiscountPercentage / 100), 4);
                }
            }

            var effectiveTotalPrice = Math.Round(dto.Quantity * effectiveUnitPrice, 2);

            var taxRate = 0m;
            var taxAmount = 0m;
            if (product.IsTaxable)
            {
                taxRate = await _taxRateService.GetEffectiveRateAsync("MX", effectiveDate: now);
                taxAmount = Math.Round(effectiveTotalPrice * taxRate, 2);
            }

            var entity = new BulkLabelJob
            {
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                UnitMeasureCode = dto.UnitMeasureCode,
                UnitPrice = effectiveUnitPrice,
                TotalPrice = effectiveTotalPrice,
                TaxRate = taxRate,
                TaxAmount = taxAmount,
                LabelCount = dto.LabelCount,
                BatchNumber = dto.BatchNumber,
                Notes = dto.Notes,
                CreatedBy = currentUser,
                CreatedAt = now,
                ModifiedBy = currentUser,
                ModifiedAt = now
            };

            context.BulkLabelJobs.Add(entity);
            await context.SaveChangesAsync(cancellationToken);

            var resultDto = _mapper.Map<BulkLabelJobDto>(entity);
            resultDto.ProductName = product.Name;
            resultDto.ProductCode = product.Code;

            return Result<BulkLabelJobDto>.Success(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bulk label job for product {ProductId}", dto.ProductId);
            return Result<BulkLabelJobDto>.Failure(_localizer["Error creating label job"]);
        }
    }

    public async Task<Result<BulkLabelJobDto>> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var entity = await context.BulkLabelJobs
                .AsNoTracking()
                .Include(j => j.Product)
                .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

            if (entity == null)
                return Result<BulkLabelJobDto>.Failure(_localizer["Label job not found"]);

            var dto = _mapper.Map<BulkLabelJobDto>(entity);
            return Result<BulkLabelJobDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bulk label job {Id}", id);
            return Result<BulkLabelJobDto>.Failure(_localizer["Error retrieving label job"]);
        }
    }

    public async Task<Result<List<BulkLabelJobDto>>> GetRecentAsync(
        int count = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var entities = await context.BulkLabelJobs
                .AsNoTracking()
                .Include(j => j.Product)
                .OrderByDescending(j => j.CreatedAt)
                .Take(count)
                .ToListAsync(cancellationToken);

            var dtos = _mapper.Map<List<BulkLabelJobDto>>(entities);
            return Result<List<BulkLabelJobDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent bulk label jobs");
            return Result<List<BulkLabelJobDto>>.Failure(_localizer["Error retrieving label history"]);
        }
    }

    public async Task<Result<byte[]>> GetLabelPdfAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var entity = await context.BulkLabelJobs
                .AsNoTracking()
                .Include(j => j.Product)
                .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

            if (entity == null)
                return Result<byte[]>.Failure(_localizer["Label job not found"]);

            var dto = new CreateBulkLabelJobDto
            {
                ProductId = entity.ProductId,
                Quantity = entity.Quantity,
                UnitMeasureCode = entity.UnitMeasureCode,
                UnitPrice = entity.UnitPrice,
                TotalPrice = entity.TotalPrice,
                LabelCount = entity.LabelCount,
                BatchNumber = entity.BatchNumber
            };

            return await GenerateLabelPdfInternalAsync(dto, entity.Product.Name, entity.Product.Code, entity.CreatedAt, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating label PDF for job {Id}", id);
            return Result<byte[]>.Failure(_localizer["Error generating label PDF"]);
        }
    }

    public async Task<Result<byte[]>> PreviewLabelPdfAsync(
        CreateBulkLabelJobDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var product = await context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == dto.ProductId, cancellationToken);

            if (product == null)
                return Result<byte[]>.Failure(_localizer["Product not found"]);

            return await GenerateLabelPdfInternalAsync(dto, product.Name, product.Code, _dateTime.Now, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating label PDF preview for product {ProductId}", dto.ProductId);
            return Result<byte[]>.Failure(_localizer["Error generating label preview"]);
        }
    }

    // --- Private helpers ---

    private async Task<Result<byte[]>> GenerateLabelPdfInternalAsync(
        CreateBulkLabelJobDto dto,
        string productName,
        string productCode,
        DateTime labelDate,
        CancellationToken cancellationToken)
    {
        var settingsResult = await _labelSettingsService.GetSettingsAsync(cancellationToken);
        var widthMm = settingsResult.IsSuccess ? settingsResult.Value.WidthMm : 62;
        var heightMm = settingsResult.IsSuccess ? settingsResult.Value.HeightMm : 28;

        var barcodeBase64 = _barcodeGenerator.GenerateBarcodeBase64(
            productId: dto.ProductId,
            quantity: dto.Quantity,
            totalPrice: dto.TotalPrice);

        var humanReadable = BuildHumanReadableText(dto, productCode);

        var viewModel = new BulkLabelViewModel
        {
            ProductCode = productCode,
            BarcodeBase64 = barcodeBase64,
            BarcodeHumanReadable = humanReadable,
            LabelWidthMm = widthMm,
            LabelHeightMm = heightMm
        };

        var pdfBytes = await _pdfService.GenerateThermalTicketPdfFromViewAsync(
            "/Views/Labels/BulkProductLabel.cshtml",
            viewModel,
            widthMm,
            cancellationToken);

        return Result<byte[]>.Success(pdfBytes);
    }

    private static string BuildHumanReadableText(CreateBulkLabelJobDto dto, string productCode)
    {
        var qtyMillis = ((long)Math.Round(dto.Quantity * 1000)).ToString("D6");
        var priceCents = ((long)Math.Round(dto.TotalPrice * 100)).ToString();
        return $"{productCode}|{qtyMillis}|{priceCents}";
    }
}
