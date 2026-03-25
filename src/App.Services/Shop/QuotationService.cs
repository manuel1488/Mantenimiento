using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using App.Core.Common;
using App.Core.DTOs.Shop;
using App.Core.DTOs.Shop.Calculation;
using App.Core.Enums.Shop;
using App.Core.Interfaces;
using App.Core.Interfaces.Shop;
using App.Core.Models.Email;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Shared.Services;
using App.Services.Settings;

namespace App.Services.Shop;

public class QuotationService : IQuotationService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<QuotationService> _logger;
    private readonly IStringLocalizer<QuotationService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly ITaxRateService _taxRateService;
    private readonly ICompanySettingsService _companySettingsService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IPdfService _pdfService;
    private readonly IPricingCalculationService _pricingService;
    private readonly IDocumentSequenceService _documentSequenceService;

    public QuotationService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<QuotationService> logger,
        IStringLocalizer<QuotationService> localizer,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        ITaxRateService taxRateService,
        ICompanySettingsService companySettingsService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IPdfService pdfService,
        IPricingCalculationService pricingService,
        IDocumentSequenceService documentSequenceService)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _localizer = localizer;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        _taxRateService = taxRateService;
        _companySettingsService = companySettingsService;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _pdfService = pdfService;
        _pricingService = pricingService;
        _documentSequenceService = documentSequenceService;
    }

    public async Task<(int TotalCount, IList<QuotationDto> Items)> GetQuotationsAsync(
        int page = 1,
        int pageSize = 10,
        string? search = null,
        long? customerId = null,
        string? status = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.Quotations
            .AsNoTracking()
            .Include(q => q.Customer)
            .Include(q => q.Details)
            .Where(q => q.IsDeleted == 0);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLower();
            query = query.Where(q =>
                q.QuotationNumber.ToLower().Contains(lower) ||
                q.Customer.Name.ToLower().Contains(lower));
        }

        if (customerId.HasValue)
            query = query.Where(q => q.CustomerId == customerId.Value);

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<QuotationStatus>(status, true, out var parsedStatus))
            query = query.Where(q => q.Status == parsedStatus);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(q => q.QuoteDate)
            .ThenByDescending(q => q.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (totalCount, _mapper.Map<IList<QuotationDto>>(items));
    }

    public async Task<QuotationDto?> GetByIdAsync(long id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var quotation = await context.Quotations
            .AsNoTracking()
            .Include(q => q.Customer)
            .Include(q => q.Details)
                .ThenInclude(d => d.Product)
            .Where(q => q.IsDeleted == 0 && q.Id == id)
            .FirstOrDefaultAsync();

        return quotation is null ? null : _mapper.Map<QuotationDto>(quotation);
    }

    public async Task<Result<QuotationDto>> CreateAsync(CreateQuotationDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var customer = await context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == dto.CustomerId && c.IsDeleted == 0);

            if (customer is null)
                return Result<QuotationDto>.Failure(_localizer["Customer not found"]);

            var now = _dateTime.Now;
            var currentUser = _currentUserService.UserId ?? "System";

            var companySettings = await _companySettingsService.GetSettingsAsync();
            var countryCode = companySettings?.CountryCode ?? "MX";
            var taxRate = await _taxRateService.GetEffectiveRateAsync(countryCode);

            var quotationNumber = await _documentSequenceService.GetNextNumberAsync("Quotation", "COT", now.Year);

            var quotation = new Quotation
            {
                QuotationNumber = quotationNumber,
                CustomerId = dto.CustomerId,
                QuoteDate = dto.QuoteDate ?? now,
                ValidUntil = dto.ValidUntil ?? now.AddDays(15),
                Status = QuotationStatus.Draft,
                Notes = dto.Notes,
                DiscountPercentage = dto.DiscountPercentage,
                CreatedBy = currentUser,
                CreatedAt = now,
                ModifiedBy = currentUser,
                ModifiedAt = now
            };

            var productIds = dto.Details.Select(d => d.ProductId).ToList();
            var products = await context.Products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.Id) && p.IsDeleted == 0)
                .ToDictionaryAsync(p => p.Id);

            var documentLines = new List<DocumentLineInput>();

            foreach (var detailDto in dto.Details)
            {
                if (!products.TryGetValue(detailDto.ProductId, out var product))
                    return Result<QuotationDto>.Failure(
                        string.Format(_localizer["Product {0} not found"], detailDto.ProductId));

                var lineCalc = _pricingService.CalculateLine(new LineCalculationInput
                {
                    Quantity = detailDto.Quantity,
                    UnitPrice = detailDto.UnitPrice,
                    DiscountPercentage = detailDto.DiscountPercentage,
                    TaxRate = taxRate
                });

                var lineDiscount = Math.Round(lineCalc.DiscountAmount, 2);
                var lineTotal = Math.Round(lineCalc.TaxBase + lineCalc.TaxAmount, 2);

                var detail = new QuotationDetail
                {
                    ProductId = detailDto.ProductId,
                    ProductName = product.Name,
                    ProductCode = product.Code ?? string.Empty,
                    Quantity = detailDto.Quantity,
                    UnitPrice = detailDto.UnitPrice,
                    DiscountPercentage = detailDto.DiscountPercentage,
                    DiscountAmount = lineDiscount,
                    TaxRate = taxRate,
                    TaxAmount = lineCalc.TaxAmount,
                    Subtotal = lineCalc.TaxBase,
                    Total = lineTotal,
                    CreatedBy = currentUser,
                    CreatedAt = now,
                    ModifiedBy = currentUser,
                    ModifiedAt = now
                };

                quotation.Details.Add(detail);
                documentLines.Add(new DocumentLineInput
                {
                    Subtotal = lineCalc.Subtotal,
                    DiscountAmount = lineCalc.DiscountAmount,
                    IsTaxable = true,
                    TaxAmount = lineCalc.TaxAmount,
                    TaxBase = lineCalc.TaxBase
                });
            }

            // Calculate document-level totals
            var docCalc = await _pricingService.CalculateDocumentAsync(new DocumentCalculationInput
            {
                Lines = documentLines,
                GlobalDiscountPercentage = dto.DiscountPercentage,
                TaxRate = taxRate,
                ApplyRounding = false
            });

            quotation.Subtotal = docCalc.Subtotal - docCalc.TotalDiscountAmount;
            quotation.DiscountAmount = docCalc.TotalDiscountAmount;
            quotation.TaxAmount = docCalc.TaxAmount;
            quotation.Total = docCalc.Total;

            context.Quotations.Add(quotation);
            await context.SaveChangesAsync();

            var created = await context.Quotations
                .Include(q => q.Customer)
                .Include(q => q.Details)
                .FirstAsync(q => q.Id == quotation.Id);

            return Result<QuotationDto>.Success(_mapper.Map<QuotationDto>(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating quotation");
            return Result<QuotationDto>.Failure(_localizer["Error creating quotation"]);
        }
    }

    public async Task<Result<QuotationDto>> UpdateAsync(long id, UpdateQuotationDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var quotation = await context.Quotations
                .Include(q => q.Details)
                .FirstOrDefaultAsync(q => q.Id == id && q.IsDeleted == 0);

            if (quotation is null)
                return Result<QuotationDto>.Failure(_localizer["Quotation not found"]);

            if (quotation.Status != QuotationStatus.Draft)
                return Result<QuotationDto>.Failure(_localizer["Only draft quotations can be edited"]);

            var customer = await context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == dto.CustomerId && c.IsDeleted == 0);

            if (customer is null)
                return Result<QuotationDto>.Failure(_localizer["Customer not found"]);

            var now = _dateTime.Now;
            var currentUser = _currentUserService.UserId ?? "System";

            var companySettings = await _companySettingsService.GetSettingsAsync();
            var countryCode = companySettings?.CountryCode ?? "MX";
            var taxRate = await _taxRateService.GetEffectiveRateAsync(countryCode);

            quotation.CustomerId = dto.CustomerId;
            quotation.QuoteDate = dto.QuoteDate ?? now;
            quotation.ValidUntil = dto.ValidUntil ?? now.AddDays(15);
            quotation.Notes = dto.Notes;
            quotation.DiscountPercentage = dto.DiscountPercentage;
            quotation.ModifiedBy = currentUser;
            quotation.ModifiedAt = now;

            // Clear stored PDF so it regenerates with updated data
            quotation.PdfData = null;
            quotation.PdfGeneratedAt = null;

            context.QuotationDetails.RemoveRange(quotation.Details);
            quotation.Details.Clear();

            var productIds = dto.Details.Select(d => d.ProductId).ToList();
            var products = await context.Products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.Id) && p.IsDeleted == 0)
                .ToDictionaryAsync(p => p.Id);

            var documentLines = new List<DocumentLineInput>();

            foreach (var detailDto in dto.Details)
            {
                if (!products.TryGetValue(detailDto.ProductId, out var product))
                    return Result<QuotationDto>.Failure(
                        string.Format(_localizer["Product {0} not found"], detailDto.ProductId));

                var lineCalc = _pricingService.CalculateLine(new LineCalculationInput
                {
                    Quantity = detailDto.Quantity,
                    UnitPrice = detailDto.UnitPrice,
                    DiscountPercentage = detailDto.DiscountPercentage,
                    TaxRate = taxRate
                });

                var lineDiscount = Math.Round(lineCalc.DiscountAmount, 2);
                var lineTotal = Math.Round(lineCalc.TaxBase + lineCalc.TaxAmount, 2);

                var detail = new QuotationDetail
                {
                    ProductId = detailDto.ProductId,
                    ProductName = product.Name,
                    ProductCode = product.Code ?? string.Empty,
                    Quantity = detailDto.Quantity,
                    UnitPrice = detailDto.UnitPrice,
                    DiscountPercentage = detailDto.DiscountPercentage,
                    DiscountAmount = lineDiscount,
                    TaxRate = taxRate,
                    TaxAmount = lineCalc.TaxAmount,
                    Subtotal = lineCalc.TaxBase,
                    Total = lineTotal,
                    CreatedBy = currentUser,
                    CreatedAt = now,
                    ModifiedBy = currentUser,
                    ModifiedAt = now
                };

                quotation.Details.Add(detail);
                documentLines.Add(new DocumentLineInput
                {
                    Subtotal = lineCalc.Subtotal,
                    DiscountAmount = lineCalc.DiscountAmount,
                    IsTaxable = true,
                    TaxAmount = lineCalc.TaxAmount,
                    TaxBase = lineCalc.TaxBase
                });
            }

            var docCalc = await _pricingService.CalculateDocumentAsync(new DocumentCalculationInput
            {
                Lines = documentLines,
                GlobalDiscountPercentage = dto.DiscountPercentage,
                TaxRate = taxRate,
                ApplyRounding = false
            });

            quotation.Subtotal = docCalc.Subtotal - docCalc.TotalDiscountAmount;
            quotation.DiscountAmount = docCalc.TotalDiscountAmount;
            quotation.TaxAmount = docCalc.TaxAmount;
            quotation.Total = docCalc.Total;

            await context.SaveChangesAsync();

            var updated = await context.Quotations
                .Include(q => q.Customer)
                .Include(q => q.Details)
                .FirstAsync(q => q.Id == id);

            return Result<QuotationDto>.Success(_mapper.Map<QuotationDto>(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating quotation {Id}", id);
            return Result<QuotationDto>.Failure(_localizer["Error updating quotation"]);
        }
    }

    public async Task<Result> DeleteAsync(long id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var quotation = await context.Quotations
                .FirstOrDefaultAsync(q => q.Id == id && q.IsDeleted == 0);

            if (quotation is null)
                return Result.Failure(_localizer["Quotation not found"]);

            quotation.IsDeleted = 1;
            quotation.DeletedBy = _currentUserService.UserId ?? "System";
            quotation.DeletedAt = _dateTime.Now;

            await context.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting quotation {Id}", id);
            return Result.Failure(_localizer["Error deleting quotation"]);
        }
    }

    public async Task<Result> UpdateStatusAsync(long id, QuotationStatus status)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var quotation = await context.Quotations
                .FirstOrDefaultAsync(q => q.Id == id && q.IsDeleted == 0);

            if (quotation is null)
                return Result.Failure(_localizer["Quotation not found"]);

            quotation.Status = status;
            quotation.ModifiedBy = _currentUserService.UserId ?? "System";
            quotation.ModifiedAt = _dateTime.Now;

            await context.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for quotation {Id}", id);
            return Result.Failure(_localizer["Error updating quotation status"]);
        }
    }

    public async Task<Result> SendByEmailAsync(long id, string? emailOverride = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var quotation = await context.Quotations
                .Include(q => q.Customer)
                .Include(q => q.Details)
                .FirstOrDefaultAsync(q => q.Id == id && q.IsDeleted == 0);

            if (quotation is null)
                return Result.Failure(_localizer["Quotation not found"]);

            var toEmail = emailOverride ?? quotation.Customer.Email;
            if (string.IsNullOrWhiteSpace(toEmail))
                return Result.Failure(_localizer["No email address available for this customer"]);

            // Use stored PDF or generate and store it
            byte[] pdfBytes;
            if (quotation.PdfData != null && quotation.PdfGeneratedAt.HasValue)
            {
                pdfBytes = quotation.PdfData;
            }
            else
            {
                pdfBytes = await GeneratePdfBytesAsync(quotation);
                quotation.PdfData = pdfBytes;
                quotation.PdfGeneratedAt = _dateTime.Now;
            }

            var companySettings = await _companySettingsService.GetSettingsAsync();
            var companyName = companySettings?.CompanyName ?? "Cleeny";

            var templateData = new
            {
                customer_name = quotation.Customer.Name,
                quotation_number = quotation.QuotationNumber,
                quote_date = quotation.QuoteDate.ToString("dd/MM/yyyy"),
                valid_until = quotation.ValidUntil.ToString("dd/MM/yyyy"),
                subtotal = quotation.Subtotal.ToString("C2"),
                discount_amount = quotation.DiscountAmount.ToString("C2"),
                tax_amount = quotation.TaxAmount.ToString("C2"),
                total = quotation.Total.ToString("C2"),
                notes = quotation.Notes ?? string.Empty,
                company_name = companyName,
                items = quotation.Details.Select(d => new
                {
                    code = d.ProductCode,
                    name = d.ProductName,
                    quantity = d.Quantity.ToString("G29"),
                    unit_price = d.UnitPrice.ToString("C2"),
                    total = d.Total.ToString("C2")
                }).ToList()
            };

            var body = await _emailTemplateService.GetTemplateAsync("quotation", templateData);

            var message = new EmailMessage
            {
                To = toEmail,
                Subject = $"{_localizer["Quotation"]} {quotation.QuotationNumber} - {companyName}",
                Body = body,
                IsHtml = true,
                Attachments =
                [
                    new EmailAttachment
                    {
                        FileName = $"quotation_{quotation.QuotationNumber}.pdf",
                        Content = pdfBytes,
                        ContentType = "application/pdf"
                    }
                ]
            };

            var emailResult = await _emailService.SendAsync(message);
            if (!emailResult.Success)
                return Result.Failure(emailResult.Error ?? _localizer["Failed to send email"]);

            quotation.Status = QuotationStatus.Sent;
            quotation.SentAt = _dateTime.Now;
            quotation.SentToEmail = toEmail;
            quotation.ModifiedBy = _currentUserService.UserId ?? "System";
            quotation.ModifiedAt = _dateTime.Now;

            await context.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending quotation {Id} by email", id);
            return Result.Failure(_localizer["Error sending quotation by email"]);
        }
    }

    public async Task<byte[]> GeneratePdfAsync(long id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var quotation = await context.Quotations
            .Include(q => q.Customer)
            .Include(q => q.Details)
            .FirstOrDefaultAsync(q => q.Id == id && q.IsDeleted == 0)
            ?? throw new InvalidOperationException($"Quotation {id} not found");

        // Return stored PDF if available (immutable snapshot)
        if (quotation.PdfData != null && quotation.PdfGeneratedAt.HasValue)
            return quotation.PdfData;

        // Generate, store, and return
        var pdfBytes = await GeneratePdfBytesAsync(quotation);
        quotation.PdfData = pdfBytes;
        quotation.PdfGeneratedAt = _dateTime.Now;
        await context.SaveChangesAsync();

        return pdfBytes;
    }

    private async Task<byte[]> GeneratePdfBytesAsync(Quotation quotation)
    {
        var companySettings = await _companySettingsService.GetSettingsAsync();

        var (logoBytes, logoMime) = await _emailTemplateService.GetStaticFileBytesAsync("images/logo.webp");
        var logoBase64 = logoBytes.Length > 0
            ? $"data:{logoMime};base64,{Convert.ToBase64String(logoBytes)}"
            : string.Empty;

        var c = quotation.Customer;

        // Build address string from available fields
        var addressParts = new[]
        {
            string.IsNullOrWhiteSpace(c.Street) ? null
                : c.Street + (!string.IsNullOrWhiteSpace(c.ExteriorNumber) ? $" #{c.ExteriorNumber}" : string.Empty),
            c.Neighborhood,
            c.City,
            c.State,
            c.PostalCode
        }.Where(p => !string.IsNullOrWhiteSpace(p));
        var address = string.Join(", ", addressParts);

        var model = new QuotationPdfDto
        {
            QuotationNumber = quotation.QuotationNumber,
            CustomerName = c.Name,
            CustomerLegalName = c.LegalName,
            CustomerEmail = c.Email,
            CustomerPhone = c.Phone,
            CustomerAddress = string.IsNullOrWhiteSpace(address) ? null : address,
            CustomerTaxId = c.TaxId,
            CustomerFiscalRegime = c.FiscalRegime,
            CustomerHasFiscalData = c.HasFiscalData,
            QuoteDate = quotation.QuoteDate,
            ValidUntil = quotation.ValidUntil,
            Notes = quotation.Notes,
            Subtotal = quotation.Subtotal,
            DiscountPercentage = quotation.DiscountPercentage,
            DiscountAmount = quotation.DiscountAmount,
            TaxAmount = quotation.TaxAmount,
            Total = quotation.Total,
            Details = _mapper.Map<List<QuotationDetailDto>>(quotation.Details),
            CompanyName = companySettings?.CompanyName ?? "Cleeny",
            LogoBase64 = logoBase64
        };

        return await _pdfService.GeneratePdfFromViewAsync("~/Views/Quotations/QuotationDocument.cshtml", model);
    }

}
