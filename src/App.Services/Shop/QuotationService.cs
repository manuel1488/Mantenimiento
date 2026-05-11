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
using App.Services.Resources.PdfTemplates;
using App.Services.Settings;
using Scriban;

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
    private readonly IQuotationSettingsService _quotationSettingsService;

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
        IDocumentSequenceService documentSequenceService,
        IQuotationSettingsService quotationSettingsService)
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
        _quotationSettingsService = quotationSettingsService;
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

        var rawItems = await query
            .OrderByDescending(q => q.QuoteDate)
            .ThenByDescending(q => q.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .GroupJoin(
                context.Users.IgnoreQueryFilters(),
                q => q.CreatedBy,
                u => u.Id,
                (q, users) => new { Quotation = q, CreatedByName = users.Select(u => u.FullName).FirstOrDefault() })
            .ToListAsync();

        var items = rawItems.Select(x => x.Quotation).ToList();
        var dtos = _mapper.Map<IList<QuotationDto>>(items);

        foreach (var (raw, dto) in rawItems.Zip(dtos))
            dto.CreatedBy = raw.CreatedByName ?? dto.CreatedBy;

        // Populate converted sale/remission IDs without requiring an inverse FK
        var convertedIds = items
            .Where(q => q.Status is QuotationStatus.ConvertedToSale or QuotationStatus.ConvertedToRemission)
            .Select(q => q.Id)
            .ToList();

        if (convertedIds.Count > 0)
        {
            var saleLinks = await context.Sales
                .Where(s => s.QuotationId.HasValue && convertedIds.Contains(s.QuotationId.Value))
                .Select(s => new { s.QuotationId, s.Id })
                .ToListAsync();

            var remissionLinks = await context.Remissions
                .Where(r => r.QuotationId.HasValue && convertedIds.Contains(r.QuotationId.Value))
                .Select(r => new { r.QuotationId, r.Id })
                .ToListAsync();

            foreach (var dto in dtos)
            {
                dto.ConvertedSaleId = saleLinks.FirstOrDefault(s => s.QuotationId == dto.Id)?.Id;
                dto.ConvertedRemissionId = remissionLinks.FirstOrDefault(r => r.QuotationId == dto.Id)?.Id;
            }
        }

        return (totalCount, dtos);
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
                    DiscountAmount = detailDto.DiscountAmount,
                    TaxRate = taxRate
                });

                var lineDiscountAmount = detailDto.DiscountAmount ?? lineCalc.DiscountAmount;
                var lineTotal = Math.Round(lineCalc.TaxBase + lineCalc.TaxAmount, 2);

                var detail = new QuotationDetail
                {
                    ProductId = detailDto.ProductId,
                    ProductName = product.Name,
                    ProductCode = product.Code ?? string.Empty,
                    Quantity = detailDto.Quantity,
                    UnitPrice = detailDto.UnitPrice,
                    DiscountPercentage = detailDto.DiscountAmount.HasValue ? 0 : detailDto.DiscountPercentage,
                    DiscountAmount = lineDiscountAmount,
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

            foreach (var detail in quotation.Details)
            {
                detail.DeletedBy = currentUser;
                detail.DeletedAt = now;
            }
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
                    DiscountAmount = detailDto.DiscountAmount,
                    TaxRate = taxRate
                });

                var lineDiscountAmount = detailDto.DiscountAmount ?? lineCalc.DiscountAmount;
                var lineTotal = Math.Round(lineCalc.TaxBase + lineCalc.TaxAmount, 2);

                var detail = new QuotationDetail
                {
                    ProductId = detailDto.ProductId,
                    ProductName = product.Name,
                    ProductCode = product.Code ?? string.Empty,
                    Quantity = detailDto.Quantity,
                    UnitPrice = detailDto.UnitPrice,
                    DiscountPercentage = detailDto.DiscountAmount.HasValue ? 0 : detailDto.DiscountPercentage,
                    DiscountAmount = lineDiscountAmount,
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

    public async Task<Result> UpdateStatusAsync(long id, QuotationStatus status, string? reason = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var quotation = await context.Quotations
                .FirstOrDefaultAsync(q => q.Id == id && q.IsDeleted == 0);

            if (quotation is null)
                return Result.Failure(_localizer["Quotation not found"]);

            var validTransitions = new Dictionary<QuotationStatus, QuotationStatus[]>
            {
                [QuotationStatus.Draft]   = [QuotationStatus.Pending],
                [QuotationStatus.Pending] = [QuotationStatus.Accepted, QuotationStatus.Rejected, QuotationStatus.Expired],
            };

            if (!validTransitions.TryGetValue(quotation.Status, out var allowed) || !allowed.Contains(status))
                return Result.Failure(_localizer["This quotation is already closed and cannot be changed"]);

            quotation.Status = status;
            if (status == QuotationStatus.Rejected && !string.IsNullOrWhiteSpace(reason))
                quotation.RejectionReason = reason;
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

            if (quotation.Status == QuotationStatus.Draft)
                quotation.Status = QuotationStatus.Pending;
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
                .ThenInclude(c => c.FiscalProfile)
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
        var quotationSettings = await _quotationSettingsService.GetSettingsAsync();

        var (logoBytes, logoMime) = await _emailTemplateService.GetStaticFileBytesAsync("images/logo.webp");
        var logoBase64 = logoBytes.Length > 0
            ? $"data:{logoMime};base64,{Convert.ToBase64String(logoBytes)}"
            : string.Empty;

        var c = quotation.Customer;

        // Build commercial address
        var commercialParts = new[]
        {
            string.IsNullOrWhiteSpace(c.Street) ? null
                : c.Street + (!string.IsNullOrWhiteSpace(c.ExteriorNumber) ? $" #{c.ExteriorNumber}" : string.Empty),
            c.Neighborhood, c.City, c.State, c.PostalCode
        }.Where(p => !string.IsNullOrWhiteSpace(p));
        var commercialAddress = string.Join(", ", commercialParts);

        // Build fiscal address
        string fiscalAddress = string.Empty;
        if (c.FiscalProfile != null)
        {
            var fp = c.FiscalProfile;
            var fiscalParts = new[]
            {
                string.IsNullOrWhiteSpace(fp.Street) ? null
                    : fp.Street
                      + (!string.IsNullOrWhiteSpace(fp.ExteriorNumber) ? $" #{fp.ExteriorNumber}" : string.Empty)
                      + (!string.IsNullOrWhiteSpace(fp.InteriorNumber) ? $" Int. {fp.InteriorNumber}" : string.Empty),
                fp.Neighborhood, fp.City, fp.State, fp.PostalCode
            }.Where(p => !string.IsNullOrWhiteSpace(p));
            fiscalAddress = string.Join(", ", fiscalParts);
        }

        // ── Compute booleans used in the template ──────────────────────────
        var details = _mapper.Map<List<QuotationDetailDto>>(quotation.Details);
        bool hasDiscounts = details.Any(d => d.DiscountAmount > 0);
        bool hasFiscalData = c.FiscalProfile != null && !string.IsNullOrEmpty(c.FiscalProfile.TaxId);
        string sym = "$"; // default; QuotationPdfDto.CurrencySymbol default

        bool showBank = quotationSettings.ShowBankDetails && (
            !string.IsNullOrWhiteSpace(quotationSettings.BankBeneficiary) ||
            !string.IsNullOrWhiteSpace(quotationSettings.BankName) ||
            !string.IsNullOrWhiteSpace(quotationSettings.BankClabeNumber) ||
            !string.IsNullOrWhiteSpace(quotationSettings.BankAccountNumber));

        bool showContact = quotationSettings.ShowContactInfo && (
            !string.IsNullOrWhiteSpace(quotationSettings.ContactWebsite) ||
            !string.IsNullOrWhiteSpace(quotationSettings.ContactPhone) ||
            !string.IsNullOrWhiteSpace(quotationSettings.ContactEmail) ||
            !string.IsNullOrWhiteSpace(quotationSettings.ContactWhatsapp) ||
            !string.IsNullOrWhiteSpace(quotationSettings.ContactFacebook) ||
            !string.IsNullOrWhiteSpace(quotationSettings.ContactInstagram));

        // ── Scriban data model ─────────────────────────────────────────────
        var data = new
        {
            // Document
            quotation_number = quotation.QuotationNumber,
            quote_date       = quotation.QuoteDate.ToString("dd/MM/yyyy"),
            valid_until      = quotation.ValidUntil.ToString("dd/MM/yyyy"),
            notes            = quotation.Notes ?? string.Empty,
            has_notes        = !string.IsNullOrEmpty(quotation.Notes),

            // Totals
            subtotal        = $"{sym}{quotation.Subtotal:N2}",
            has_discount    = quotation.DiscountAmount > 0,
            discount_amount = $"{sym}{quotation.DiscountAmount:N2}",
            tax_amount      = $"{sym}{quotation.TaxAmount:N2}",
            total           = $"{sym}{quotation.Total:N2}",

            // Line items
            has_discounts = hasDiscounts,
            details = details.Select((d, i) => new
            {
                index          = i + 1,
                product_code   = d.ProductCode ?? string.Empty,
                product_name   = d.ProductName ?? string.Empty,
                quantity       = d.Quantity.ToString("G29"),
                unit_price     = $"{sym}{d.UnitPrice:N2}",
                discount_amount = d.DiscountAmount > 0 ? $"-{sym}{d.DiscountAmount:N2}" : string.Empty,
                tax_amount     = $"{sym}{d.TaxAmount:N2}",
                total          = $"{sym}{d.Total:N2}"
            }).ToList(),

            // Company
            company_name = companySettings?.CompanyName ?? "Cleeny",
            has_logo     = !string.IsNullOrEmpty(logoBase64),
            logo_base64  = logoBase64,

            // Customer — commercial
            customer_name         = c.Name,
            has_contact_name      = !string.IsNullOrWhiteSpace(c.ContactName),
            customer_contact_name = c.ContactName ?? string.Empty,
            has_customer_email    = !string.IsNullOrEmpty(c.FiscalProfile?.FiscalEmail ?? c.Email),
            customer_email        = c.FiscalProfile?.FiscalEmail ?? c.Email ?? string.Empty,
            has_customer_phone    = !string.IsNullOrEmpty(c.Phone),
            customer_phone        = c.Phone ?? string.Empty,
            has_customer_address  = !string.IsNullOrWhiteSpace(commercialAddress),
            customer_address      = commercialAddress,

            // Customer — fiscal
            customer_has_fiscal_data = hasFiscalData,
            customer_tax_id          = c.FiscalProfile?.TaxId ?? string.Empty,
            show_legal_name          = !string.IsNullOrEmpty(c.FiscalProfile?.LegalName) && c.FiscalProfile?.LegalName != c.Name,
            customer_legal_name      = c.FiscalProfile?.LegalName ?? string.Empty,
            has_fiscal_regime        = !string.IsNullOrEmpty(c.FiscalProfile?.FiscalRegime),
            customer_fiscal_regime   = c.FiscalProfile?.FiscalRegime ?? string.Empty,

            // Payment terms
            has_payment_terms  = !string.IsNullOrWhiteSpace(quotationSettings.PaymentTermsText),
            payment_terms_text = quotationSettings.PaymentTermsText ?? string.Empty,

            // Bank
            show_bank_details      = showBank,
            bank_beneficiary       = quotationSettings.BankBeneficiary ?? string.Empty,
            bank_rfc               = quotationSettings.BankRfc ?? string.Empty,
            bank_name              = quotationSettings.BankName ?? string.Empty,
            bank_account_number    = quotationSettings.BankAccountNumber ?? string.Empty,
            bank_clabe_number      = quotationSettings.BankClabeNumber ?? string.Empty,
            bank_swift             = quotationSettings.BankSwift ?? string.Empty,
            has_bank_beneficiary   = !string.IsNullOrWhiteSpace(quotationSettings.BankBeneficiary),
            has_bank_rfc           = !string.IsNullOrWhiteSpace(quotationSettings.BankRfc),
            has_bank_name          = !string.IsNullOrWhiteSpace(quotationSettings.BankName),
            has_bank_account_number = !string.IsNullOrWhiteSpace(quotationSettings.BankAccountNumber),
            has_bank_clabe         = !string.IsNullOrWhiteSpace(quotationSettings.BankClabeNumber),
            has_bank_swift         = !string.IsNullOrWhiteSpace(quotationSettings.BankSwift),

            // Contact
            show_contact_info      = showContact,
            contact_website        = quotationSettings.ContactWebsite ?? string.Empty,
            contact_phone          = quotationSettings.ContactPhone ?? string.Empty,
            contact_email          = quotationSettings.ContactEmail ?? string.Empty,
            contact_whatsapp       = quotationSettings.ContactWhatsapp ?? string.Empty,
            contact_facebook       = quotationSettings.ContactFacebook ?? string.Empty,
            contact_instagram      = quotationSettings.ContactInstagram ?? string.Empty,
            has_contact_website    = !string.IsNullOrWhiteSpace(quotationSettings.ContactWebsite),
            has_contact_phone      = !string.IsNullOrWhiteSpace(quotationSettings.ContactPhone),
            has_contact_email      = !string.IsNullOrWhiteSpace(quotationSettings.ContactEmail),
            has_contact_whatsapp   = !string.IsNullOrWhiteSpace(quotationSettings.ContactWhatsapp),
            has_contact_facebook   = !string.IsNullOrWhiteSpace(quotationSettings.ContactFacebook),
            has_contact_instagram  = !string.IsNullOrWhiteSpace(quotationSettings.ContactInstagram),

            // Localised labels
            label_quotation           = _localizer["Quotation"].Value,
            label_date                = _localizer["Date"].Value,
            label_valid_until         = _localizer["Valid Until"].Value,
            label_quotation_number    = _localizer["Quotation #"].Value,
            label_customer            = _localizer["Customer"].Value,
            label_fiscal_data         = _localizer["Fiscal Data"].Value,
            label_tax_id              = _localizer["Tax ID"].Value,
            label_fiscal_regime       = _localizer["Fiscal Regime"].Value,
            label_code                = _localizer["Code"].Value,
            label_product             = _localizer["Product"].Value,
            label_qty                 = _localizer["Qty"].Value,
            label_unit_price          = _localizer["Unit Price"].Value,
            label_discount            = _localizer["Discount"].Value,
            label_tax                 = _localizer["Tax"].Value,
            label_total               = _localizer["Total"].Value,
            label_subtotal            = _localizer["Subtotal"].Value,
            label_notes_conditions    = _localizer["Notes & Conditions"].Value,
            label_payment_terms       = _localizer["Payment Terms"].Value,
            label_wire_transfer_details = _localizer["Wire Transfer Details"].Value,
            label_beneficiary         = _localizer["Beneficiary"].Value,
            label_rfc                 = _localizer["RFC"].Value,
            label_bank                = _localizer["Bank"].Value,
            label_account_number      = _localizer["Account Number"].Value,
            label_end_of_document     = _localizer["End of Document"].Value,
            label_valid_until_footer  = _localizer["This quotation is valid until"].Value,
        };

        // ── Choose body: custom HTML from DB or embedded default ───────────
        var bodyHtml = !string.IsNullOrWhiteSpace(quotationSettings.HtmlBody)
            ? quotationSettings.HtmlBody
            : DefaultQuotationTemplate.Html;

        var css = DefaultQuotationTemplate.Css
                  + (!string.IsNullOrWhiteSpace(quotationSettings.CustomCss)
                      ? "\n" + quotationSettings.CustomCss
                      : string.Empty);

        // ── Render Scriban template ────────────────────────────────────────
        var scribanTemplate = Scriban.Template.Parse(bodyHtml);
        var renderedBody = await scribanTemplate.RenderAsync(data, member => member.Name);

        var fullHtml = $"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8" />
                <title>{_localizer["Quotation"].Value} {quotation.QuotationNumber}</title>
                <style>{css}</style>
            </head>
            <body>
            {renderedBody}
            </body>
            </html>
            """;

        return await _pdfService.GeneratePdfFromHtmlAsync(fullHtml);
    }

}
