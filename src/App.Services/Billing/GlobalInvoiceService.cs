using App.Core.Common;
using App.Core.Constants;
using App.Core.DTOs.Billing;
using App.Core.Enums.Billing;
using App.Core.Interfaces;
using App.Core.Interfaces.Billing;
using App.Core.Models.Cfdi.V40;
using App.Core.Options;
using App.Models.Billing;
using App.Models.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using App.Services.Settings;
using App.Shared.Services;

namespace App.Services.Billing;

public class GlobalInvoiceService : IGlobalInvoiceService
{
    // SAT constants for public general invoices
    private const string PublicRfc = "XAXX010101000";
    private const string PublicName = "PUBLICO EN GENERAL";
    private const string PublicFiscalRegime = "616";
    private const string PublicCfdiUse = "S01";
    private const string PublicPaymentMethod = "PUE";
    private const string GlobalProductCode = "01010101";
    private const string GlobalUnitCode = "ACT";
    private const string GlobalDescription = "Venta al público en general";
    private const string IvaCode = "002";
    private const string IvaFactorType = "Tasa";

    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMexicoCfdiXmlService _xmlService;
    private readonly IMexicoCsdSigningService _signingService;
    private readonly ISwSapienService _pacService;
    private readonly IMexicoPacSettingsService _pacSettingsService;
    private readonly ITaxSettingsService _taxSettingsService;
    private readonly ITaxRateService _taxRateService;
    private readonly ICompanySettingsService _companySettingsService;
    private readonly IPdfService _pdfService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IMexicoFiscalCatalogService _fiscalCatalogService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly ApplicationOptions _applicationOptions;
    private readonly IStringLocalizer<GlobalInvoiceService> _localizer;
    private readonly ILogger<GlobalInvoiceService> _logger;

    public GlobalInvoiceService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMexicoCfdiXmlService xmlService,
        IMexicoCsdSigningService signingService,
        ISwSapienService pacService,
        IMexicoPacSettingsService pacSettingsService,
        ITaxSettingsService taxSettingsService,
        ITaxRateService taxRateService,
        ICompanySettingsService companySettingsService,
        IPdfService pdfService,
        IEmailTemplateService emailTemplateService,
        IMexicoFiscalCatalogService fiscalCatalogService,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        IOptions<ApplicationOptions> applicationOptions,
        IStringLocalizer<GlobalInvoiceService> localizer,
        ILogger<GlobalInvoiceService> logger)
    {
        _contextFactory = contextFactory;
        _xmlService = xmlService;
        _signingService = signingService;
        _pacService = pacService;
        _pacSettingsService = pacSettingsService;
        _taxSettingsService = taxSettingsService;
        _taxRateService = taxRateService;
        _companySettingsService = companySettingsService;
        _pdfService = pdfService;
        _emailTemplateService = emailTemplateService;
        _fiscalCatalogService = fiscalCatalogService;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        _applicationOptions = applicationOptions.Value;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<Result<GlobalInvoicePreviewDto>> PreviewAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var tz = await _companySettingsService.GetCurrentTimeZoneAsync();
            await using var context = await _contextFactory.CreateDbContextAsync();
            var (eligible, alreadyInvoiced) = await QueryEligibleSalesAsync(context, startDate, endDate, tz);

            var sales = eligible
                .OrderByDescending(s => s.SaleDate)
                .Select(s => new GlobalInvoicePreviewSaleDto
                {
                    SaleId = s.Id,
                    SaleDateLocal = TimeZoneInfo.ConvertTimeFromUtc(s.SaleDate, tz)
                        .ToString("dd/MM/yyyy HH:mm"),
                    PaymentMethods = s.Payments.Count > 0
                        ? string.Join(", ", s.Payments
                            .Select(p => p.PaymentMethod?.Name ?? "—")
                            .Distinct())
                        : "—",
                    Subtotal = s.Subtotal,
                    TaxAmount = s.TaxAmount,
                    Total = s.Total
                })
                .ToList();

            // Apply the same aggregated tax recalculation used by BuildComprobante so that
            // the preview totals match the CFDI totals exactly (SAT CFDI40119 / CFDI40221).
            var subtotal       = Math.Round(eligible.Sum(s => s.Subtotal), 2);
            var discountAmount = Math.Round(eligible.Sum(s => s.DiscountAmount), 2);

            var taxableSales      = eligible.Where(s => s.TaxAmount > 0).ToList();
            var taxableSubtotal   = Math.Round(taxableSales.Sum(s => s.Subtotal), 2);
            var taxableDiscount   = Math.Round(taxableSales.Sum(s => s.DiscountAmount), 2);

            decimal taxAmount;
            if (taxableSales.Count > 0)
            {
                var taxSettings = await _taxSettingsService.GetSettingsAsync();
                var taxRate     = Math.Round(await _taxRateService.GetEffectiveRateAsync(taxSettings.CountryCode), 6);
                var taxBase     = Math.Round(taxableSubtotal - taxableDiscount, 6);
                taxAmount       = Math.Round(Math.Round(taxBase * taxRate, 6), 2);
            }
            else
            {
                taxAmount = 0m;
            }

            var total = Math.Round(subtotal - discountAmount + taxAmount, 2);

            return Result<GlobalInvoicePreviewDto>.Success(new GlobalInvoicePreviewDto
            {
                SaleCount = eligible.Count,
                Subtotal = subtotal,
                DiscountAmount = discountAmount,
                TaxAmount = taxAmount,
                Total = total,
                AlreadyInvoicedCount = alreadyInvoiced,
                Sales = sales
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing global invoice for range {Start} - {End}", startDate, endDate);
            return Result<GlobalInvoicePreviewDto>.Failure(_localizer["Error generating preview"]);
        }
    }

    public async Task<Result<GlobalInvoiceDto>> CreateAndStampAsync(CreateGlobalInvoiceDto dto)
    {
        try
        {
            _logger.LogInformation("Creating global invoice by {User} for range {Start} - {End}",
                await _currentUserService.GetUserNameAsync() ?? await _currentUserService.GetUserIdAsync(), dto.StartDate, dto.EndDate);

            // 1. Load PAC settings
            var pacSettings = await _pacSettingsService.GetAsync();
            if (pacSettings == null || !pacSettings.IsConfigured)
                return Result<GlobalInvoiceDto>.Failure(
                    "La configuración fiscal (PAC/CSD) no está completa. Configure en Administración > Configuración Fiscal.");

            // 2. Load issuer data
            var taxSettings = await _taxSettingsService.GetSettingsAsync();
            if (taxSettings == null || string.IsNullOrEmpty(taxSettings.TaxId))
                return Result<GlobalInvoiceDto>.Failure(
                    "Los datos fiscales del emisor no están configurados. Configure en Administración > Configuración > Fiscal.");

            // 3. Query eligible sales
            var tz = await _companySettingsService.GetCurrentTimeZoneAsync();
            await using var context = await _contextFactory.CreateDbContextAsync();
            var (eligibleSales, _) = await QueryEligibleSalesAsync(context, dto.StartDate, dto.EndDate, tz);

            // If the user selected a specific subset, restrict to those IDs (still validating eligibility)
            if (dto.SelectedSaleIds != null && dto.SelectedSaleIds.Count > 0)
            {
                var selectedSet = dto.SelectedSaleIds.ToHashSet();
                eligibleSales = eligibleSales.Where(s => selectedSet.Contains(s.Id)).ToList();
            }

            if (eligibleSales.Count == 0)
                return Result<GlobalInvoiceDto>.Failure("No hay ventas elegibles en el rango de fechas seleccionado.");

            // 4. Get next folio
            var folio = await GetNextFolioAsync(context, pacSettings.GlobalInvoiceStartFolio);
            var serie = pacSettings.GlobalInvoiceSerie ?? "G";
            var folioLength = pacSettings.GlobalInvoiceFolioLength;

            // 5. Derive InformacionGlobal fields
            var periodMonth = dto.StartDate.Month.ToString("D2");
            var periodYear = dto.StartDate.Year;
            var periodicidadCode = dto.Periodicity switch
            {
                GlobalInvoicePeriodicity.Daily => "01",
                GlobalInvoicePeriodicity.Weekly => "02",
                GlobalInvoicePeriodicity.Biweekly => "03",
                GlobalInvoicePeriodicity.Monthly => "04",
                _ => "04"
            };

            // 6. Aggregate totals
            var subtotal = Math.Round(eligibleSales.Sum(s => s.Subtotal), 2);
            var discountAmount = Math.Round(eligibleSales.Sum(s => s.DiscountAmount), 2);
            var taxAmount = Math.Round(eligibleSales.Sum(s => s.TaxAmount), 2);
            var total = Math.Round(eligibleSales.Sum(s => s.Total), 2);

            // 7. Split into taxable (IVA > 0) and exempt (IVA = 0) groups.
            // A blended/derived rate is invalid for CFDI — TasaOCuota must be the exact
            // configured rate. Each group becomes its own Concepto in the XML.
            var taxableSales = eligibleSales.Where(s => s.TaxAmount > 0).ToList();
            var exemptSales  = eligibleSales.Where(s => s.TaxAmount == 0).ToList();

            var taxableSubtotal  = Math.Round(taxableSales.Sum(s => s.Subtotal), 2);
            var taxableDiscount  = Math.Round(taxableSales.Sum(s => s.DiscountAmount), 2);
            var taxableTaxAmount = Math.Round(taxableSales.Sum(s => s.TaxAmount), 2);
            var exemptSubtotal   = Math.Round(exemptSales.Sum(s => s.Subtotal), 2);
            var exemptDiscount   = Math.Round(exemptSales.Sum(s => s.DiscountAmount), 2);

            // Use the configured tax rate, not a derived/blended one
            var taxRate = taxableSales.Count > 0
                ? Math.Round(await _taxRateService.GetEffectiveRateAsync(taxSettings.CountryCode), 6)
                : 0m;

            // 8. Create draft entity
            var globalInvoice = new GlobalInvoice
            {
                Serie = serie,
                Folio = folio,
                Periodicity = dto.Periodicity,
                StartDate = _dateTime.ToUtc(dto.StartDate.Date, tz),
                EndDate = _dateTime.ToUtc(dto.EndDate.Date, tz),
                PeriodMonth = periodMonth,
                PeriodYear = periodYear,
                PaymentForm = dto.PaymentForm,
                SaleCount = eligibleSales.Count,
                Subtotal = subtotal,
                DiscountAmount = discountAmount,
                TaxAmount = taxAmount,
                Total = total,
                Status = GlobalInvoiceStatus.Draft,
                IssuerRfc = taxSettings.TaxId,
                IssuerLegalName = taxSettings.BusinessName ?? string.Empty,
                IssuerFiscalRegime = taxSettings.FiscalRegime ?? string.Empty,
                IssuerPostalCode = taxSettings.PostalCode ?? string.Empty,
                CreatedBy = await _currentUserService.GetUserIdAsync(),
                CreatedAt = _dateTime.Now,
                ModifiedBy = await _currentUserService.GetUserIdAsync(),
                ModifiedAt = _dateTime.Now
            };

            context.GlobalInvoices.Add(globalInvoice);
            await context.SaveChangesAsync();

            // 9. Link sales
            var saleLinks = eligibleSales.Select(s => new GlobalInvoiceSale
            {
                GlobalInvoiceId = globalInvoice.Id,
                SaleId = s.Id
            }).ToList();
            context.GlobalInvoiceSales.AddRange(saleLinks);
            await context.SaveChangesAsync();

            try
            {
                // 10. Resolve issuer timezone for CFDI Fecha
                TimeZoneInfo issuerTimeZone;
                if (!string.IsNullOrEmpty(taxSettings.PostalCodeIanaTimeZoneId))
                    issuerTimeZone = TimeZoneInfo.FindSystemTimeZoneById(taxSettings.PostalCodeIanaTimeZoneId);
                else
                {
                    _logger.LogWarning("Postal code timezone not configured — falling back to company timezone for global invoice Fecha");
                    issuerTimeZone = await _companySettingsService.GetCurrentTimeZoneAsync();
                }
                var issueDate = TimeZoneInfo.ConvertTimeFromUtc(_dateTime.Now, issuerTimeZone);

                // 11. Build Comprobante
                var receiver = await GetPublicGeneralReceiverAsync();
                var comprobante = BuildComprobante(
                    globalInvoice, serie, folio, folioLength,
                    subtotal, discountAmount, taxAmount, total,
                    taxableSubtotal, taxableDiscount, taxableTaxAmount, taxRate,
                    exemptSubtotal, exemptDiscount,
                    dto.PaymentForm, periodicidadCode, periodMonth, periodYear.ToString(),
                    issueDate,
                    receiver.Name, receiver.FiscalRegime, receiver.CfdiUse);

                // Sync Total on the entity with the fiscal value computed inside BuildComprobante
                // (SAT CFDI40119: Total = SubTotal - Descuento + TotalImpuestosTrasladados).
                globalInvoice.Total = comprobante.Total;

                // 12. Generate XML
                var xmlResult = await _xmlService.GenerateXmlAsync(comprobante);
                if (!xmlResult.IsSuccess)
                    return await MarkStampErrorAsync(context, globalInvoice, xmlResult.Error!);

                // 13. Sign
                var certResult = await _pacSettingsService.GetCsdCertificateBytesAsync();
                var keyResult = await _pacSettingsService.GetCsdPrivateKeyBytesAsync();
                var pwdResult = await _pacSettingsService.GetCsdPasswordAsync();

                if (!certResult.IsSuccess) return await MarkStampErrorAsync(context, globalInvoice, certResult.Error!);
                if (!keyResult.IsSuccess) return await MarkStampErrorAsync(context, globalInvoice, keyResult.Error!);
                if (!pwdResult.IsSuccess) return await MarkStampErrorAsync(context, globalInvoice, pwdResult.Error!);

                var signedXmlResult = await _signingService.SignXmlAsync(
                    xmlResult.Value!, certResult.Value!, keyResult.Value!, pwdResult.Value!);

                if (!signedXmlResult.IsSuccess)
                    return await MarkStampErrorAsync(context, globalInvoice, signedXmlResult.Error!);

                // 14. Stamp
                var stampResult = await _pacService.StampAsync(signedXmlResult.Value!);
                if (!stampResult.IsSuccess)
                    return await MarkStampErrorAsync(context, globalInvoice, stampResult.Error!);

                var stamp = stampResult.Value!;

                // 15. Update with stamp data
                globalInvoice.Uuid = stamp.Uuid;
                globalInvoice.StampDate = _dateTime.Now;
                globalInvoice.Status = GlobalInvoiceStatus.Stamped;
                globalInvoice.XmlContent = stamp.Cfdi ?? signedXmlResult.Value!;
                globalInvoice.NoCertificadoSat = stamp.NoCertificadoSat;
                globalInvoice.NoCertificadoCfdi = stamp.NoCertificadoCfdi;
                globalInvoice.SelloSat = stamp.SelloSat;
                globalInvoice.SelloCfdi = stamp.SelloCfdi;
                globalInvoice.CadenaOriginalSat = stamp.CadenaOriginalSat;
                globalInvoice.StampError = null;
                globalInvoice.ModifiedBy = await _currentUserService.GetUserIdAsync();
                globalInvoice.ModifiedAt = _dateTime.Now;
                await context.SaveChangesAsync();

                _logger.LogInformation("Global invoice {Id} stamped successfully. UUID: {Uuid}",
                    globalInvoice.Id, globalInvoice.Uuid);

                return Result<GlobalInvoiceDto>.Success(MapToDto(globalInvoice));
            }
            catch (Exception stampEx)
            {
                _logger.LogError(stampEx, "Unexpected error stamping global invoice {Id}", globalInvoice.Id);
                return await MarkStampErrorAsync(context, globalInvoice, stampEx.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating global invoice");
            return Result<GlobalInvoiceDto>.Failure(_localizer["Error creating global invoice"]);
        }
    }

    public async Task<Result<List<GlobalInvoiceListDto>>> GetAllAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var invoices = await context.GlobalInvoices
                .AsNoTracking()
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return Result<List<GlobalInvoiceListDto>>.Success(invoices.Select(MapToListDto).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving global invoices");
            return Result<List<GlobalInvoiceListDto>>.Failure(_localizer["Error retrieving invoices"]);
        }
    }

    public async Task<Result<GlobalInvoiceDto>> GetByIdAsync(long id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var invoice = await context.GlobalInvoices
                .AsNoTracking()
                .Include(i => i.GlobalInvoiceSales)
                    .ThenInclude(gs => gs.Sale)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
                return Result<GlobalInvoiceDto>.Failure(_localizer["Invoice not found"]);

            var dto = MapToDto(invoice);
            dto.Sales = invoice.GlobalInvoiceSales
                .OrderBy(gs => gs.Sale.SaleDate)
                .Select(gs => new GlobalInvoiceSaleDto
                {
                    SaleId = gs.SaleId,
                    SaleDate = gs.Sale.SaleDate,
                    Subtotal = gs.Sale.Subtotal,
                    TaxAmount = gs.Sale.TaxAmount,
                    Total = gs.Sale.Total
                }).ToList();
            return Result<GlobalInvoiceDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving global invoice {Id}", id);
            return Result<GlobalInvoiceDto>.Failure(_localizer["Error retrieving invoice"]);
        }
    }

    public async Task<Result<Dictionary<long, long>>> GetActiveSaleToInvoiceMapAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var map = await context.GlobalInvoiceSales
                .AsNoTracking()
                .Where(gs => gs.GlobalInvoice!.Status == GlobalInvoiceStatus.Stamped)
                .Select(gs => new { gs.SaleId, gs.GlobalInvoiceId })
                .ToDictionaryAsync(x => x.SaleId, x => x.GlobalInvoiceId);
            return Result<Dictionary<long, long>>.Success(map);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading global invoice sale map");
            return Result<Dictionary<long, long>>.Failure(_localizer["Error loading global invoice data"]);
        }
    }

    public async Task<Result<string>> GetXmlAsync(long id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var invoice = await context.GlobalInvoices
                .AsNoTracking()
                .Select(i => new { i.Id, i.XmlContent, i.Status })
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
                return Result<string>.Failure(_localizer["Invoice not found"]);

            if (string.IsNullOrEmpty(invoice.XmlContent))
                return Result<string>.Failure(_localizer["XML not available — invoice has not been stamped"]);

            return Result<string>.Success(invoice.XmlContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving XML for global invoice {Id}", id);
            return Result<string>.Failure(_localizer["Error retrieving XML"]);
        }
    }

    public async Task<Result> CancelAsync(long id, string reason, string? replacementUuid = null, string? notes = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var invoice = await context.GlobalInvoices.FindAsync(id);

            if (invoice == null)
                return Result.Failure(_localizer["Invoice not found"]);

            if (invoice.Status != GlobalInvoiceStatus.Stamped)
                return Result.Failure("Solo se pueden cancelar facturas timbradas.");

            var validReasons = new[] { "01", "02", "03", "04" };
            if (!validReasons.Contains(reason))
                return Result.Failure("Motivo de cancelación inválido. Use 01, 02, 03 o 04.");

            if (reason == "01" && string.IsNullOrEmpty(replacementUuid))
                return Result.Failure("El motivo 01 requiere el UUID de la factura sustituta.");

            // Mark pending before calling PAC
            invoice.Status = GlobalInvoiceStatus.Cancelled; // optimistic — revert on failure
            invoice.CancellationReason = reason;
            invoice.CancellationNotes = notes;
            invoice.ReplacementUuid = replacementUuid;
            invoice.CancellationDate = _dateTime.Now;
            invoice.CancellationStatus = "Pending";
            invoice.ModifiedBy = await _currentUserService.GetUserIdAsync();
            invoice.ModifiedAt = _dateTime.Now;
            await context.SaveChangesAsync();

            _logger.LogInformation("Sending cancellation to PAC for global invoice {Id} UUID {Uuid}", id, invoice.Uuid);

            var cancelResult = await _pacService.CancelCfdiAsync(
                invoice.Uuid!,
                invoice.IssuerRfc,
                PublicRfc,
                invoice.Total,
                reason,
                replacementUuid);

            if (!cancelResult.IsSuccess)
            {
                // Revert
                invoice.Status = GlobalInvoiceStatus.Stamped;
                invoice.CancellationReason = null;
                invoice.ReplacementUuid = null;
                invoice.CancellationDate = null;
                invoice.CancellationStatus = null;
                invoice.ModifiedAt = _dateTime.Now;
                await context.SaveChangesAsync();
                return Result.Failure(cancelResult.Error!);
            }

            var data = cancelResult.Value!;
            var uuidStatus = string.Empty;
            if (data.Uuid != null && data.Uuid.TryGetValue(invoice.Uuid!, out var code))
                uuidStatus = code;

            invoice.CancellationAcuse = data.Acuse;
            invoice.CancellationStatus = uuidStatus is "201" or "202" || data.StatusSat == "Cancelado"
                ? "Accepted"
                : "Pending";
            invoice.ModifiedAt = _dateTime.Now;
            await context.SaveChangesAsync();

            _logger.LogInformation("Global invoice {Id} cancellation result: {Status}", id, invoice.CancellationStatus);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling global invoice {Id}", id);
            return Result.Failure(_localizer["Error cancelling invoice"]);
        }
    }

    public async Task<Result<byte[]>> GetPdfAsync(long id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var invoice = await context.GlobalInvoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
                return Result<byte[]>.Failure(_localizer["Invoice not found"]);

            if (invoice.Status == GlobalInvoiceStatus.Draft)
                return Result<byte[]>.Failure(_localizer["PDF not available — invoice has not been stamped"]);

            var tz = await _companySettingsService.GetCurrentTimeZoneAsync();
            var paymentForms = await _fiscalCatalogService.GetPaymentFormsAsync();
            var paymentFormDesc = paymentForms.FirstOrDefault(p => p.Code == invoice.PaymentForm)?.Description ?? string.Empty;
            var (logoBytes, logoMime) = await _emailTemplateService.GetStaticFileBytesAsync("images/logo.webp");
            var logoBase64 = logoBytes.Length > 0
                ? $"data:{logoMime};base64,{Convert.ToBase64String(logoBytes)}"
                : $"{_applicationOptions.BaseUrl.TrimEnd('/')}/images/logo.webp";

            var receiver = await GetPublicGeneralReceiverAsync();
            var data = BuildGlobalInvoiceTemplateData(
                invoice, tz, paymentFormDesc, logoBase64,
                isCancelled: invoice.Status == GlobalInvoiceStatus.Cancelled,
                isPreview: false,
                receiver.Name, receiver.FiscalRegime, receiver.CfdiUse);

            var html = await _emailTemplateService.GetTemplateAsync("invoice-cfdi", data);

            if (invoice.Status == GlobalInvoiceStatus.Cancelled)
            {
                var cancelDate = invoice.CancellationDate.HasValue
                    ? TimeZoneInfo.ConvertTimeFromUtc(invoice.CancellationDate.Value, tz)
                        .ToString("dd/MM/yyyy HH:mm:ss")
                    : string.Empty;
                html = CfdiPdfHelper.InjectCancellationWatermark(html, cancelDate);
            }

            var pdf = await _pdfService.GeneratePdfFromHtmlAsync(html);
            return Result<byte[]>.Success(pdf);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for global invoice {Id}", id);
            return Result<byte[]>.Failure(_localizer["Error generating PDF"]);
        }
    }

    public async Task<Result<byte[]>> GetCancellationAcuseAsync(long id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var invoice = await context.GlobalInvoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
                return Result<byte[]>.Failure(_localizer["Invoice not found"]);

            if (string.IsNullOrEmpty(invoice.CancellationAcuse))
                return Result<byte[]>.Failure(_localizer["Cancellation acuse not available"]);

            return Result<byte[]>.Success(System.Text.Encoding.UTF8.GetBytes(invoice.CancellationAcuse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cancellation acuse for global invoice {Id}", id);
            return Result<byte[]>.Failure(_localizer["Error retrieving cancellation acuse"]);
        }
    }

    public async Task<Result<byte[]>> GetPdfPreviewAsync(CreateGlobalInvoiceDto dto, GlobalInvoicePreviewDto preview)
    {
        try
        {
            var tz = await _companySettingsService.GetCurrentTimeZoneAsync();
            var paymentForms = await _fiscalCatalogService.GetPaymentFormsAsync();
            var paymentFormDesc = paymentForms.FirstOrDefault(p => p.Code == dto.PaymentForm)?.Description ?? string.Empty;
            var taxSettings = await _taxSettingsService.GetSettingsAsync();
            var (logoBytes, logoMime) = await _emailTemplateService.GetStaticFileBytesAsync("images/logo.webp");
            var logoBase64 = logoBytes.Length > 0
                ? $"data:{logoMime};base64,{Convert.ToBase64String(logoBytes)}"
                : $"{_applicationOptions.BaseUrl.TrimEnd('/')}/images/logo.webp";

            string FormatLocalDate(DateTime local)
                => DateTime.SpecifyKind(local, DateTimeKind.Unspecified).ToString("dd/MM/yyyy");

            var dummyInvoice = new GlobalInvoice
            {
                Serie = "—",
                Folio = 0,
                StartDate = _dateTime.ToUtc(dto.StartDate.Date, tz),
                EndDate = _dateTime.ToUtc(dto.EndDate.Date, tz),
                PeriodMonth = dto.StartDate.Month.ToString("D2"),
                PeriodYear = dto.StartDate.Year,
                Periodicity = dto.Periodicity,
                PaymentForm = dto.PaymentForm,
                SaleCount = preview.SaleCount,
                Subtotal = preview.Subtotal,
                DiscountAmount = preview.DiscountAmount,
                TaxAmount = preview.TaxAmount,
                Total = preview.Total,
                Status = GlobalInvoiceStatus.Draft,
                IssuerRfc = taxSettings?.TaxId ?? string.Empty,
                IssuerLegalName = taxSettings?.BusinessName ?? string.Empty,
                IssuerFiscalRegime = taxSettings?.FiscalRegime ?? string.Empty,
                IssuerPostalCode = taxSettings?.PostalCode ?? string.Empty,
                CreatedBy = string.Empty,
                CreatedAt = DateTime.UtcNow,
                ModifiedBy = string.Empty,
                ModifiedAt = DateTime.UtcNow
            };

            var receiver = await GetPublicGeneralReceiverAsync();
            var data = BuildGlobalInvoiceTemplateData(
                dummyInvoice, tz, paymentFormDesc, logoBase64,
                isCancelled: false,
                isPreview: true,
                receiver.Name, receiver.FiscalRegime, receiver.CfdiUse);

            var html = await _emailTemplateService.GetTemplateAsync("invoice-cfdi", data);
            var pdf = await _pdfService.GeneratePdfFromHtmlAsync(html);
            return Result<byte[]>.Success(pdf);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF preview for global invoice");
            return Result<byte[]>.Failure(_localizer["Error generating PDF preview"]);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns (eligibleSales, alreadyInvoicedCount).
    /// Eligible = Public type, not Cancelled, no active MexicoInvoice, not in active GlobalInvoice, SaleDate in range.
    /// startDate/endDate are local calendar dates — converted to UTC using the company timezone for querying.
    /// </summary>
    private static async Task<(List<App.Models.Shop.Sale> eligible, int alreadyInvoiced)>
        QueryEligibleSalesAsync(ApplicationDbContext context, DateTime startDate, DateTime endDate, TimeZoneInfo tz)
    {
        // Convert local date boundaries to UTC for comparison against SaleDate (stored in UTC).
        // A sale at 23:00 local in UTC-6 is stored as 05:00 UTC next day — must include it.
        var startLocal = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Unspecified);
        var endLocal   = DateTime.SpecifyKind(endDate.Date.AddDays(1), DateTimeKind.Unspecified);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, tz);
        var endUtc   = TimeZoneInfo.ConvertTimeToUtc(endLocal, tz); // exclusive upper bound

        var salesInRange = await context.Sales
            .AsNoTracking()
            .Include(s => s.Payments)
                .ThenInclude(p => p.PaymentMethod)
            .Where(s =>
                s.SaleType == SaleType.Public &&
                s.Status == App.Core.Enums.Shop.SaleStatus.Created &&
                s.SaleDate >= startUtc &&
                s.SaleDate < endUtc)
            .ToListAsync();

        // Sales with an active (non-cancelled) individual invoice
        var invoicedSaleIds = await context.MexicoInvoices
            .AsNoTracking()
            .Where(i => i.Status != "Cancelled")
            .Select(i => i.SaleId)
            .ToHashSetAsync();

        // Sales already included in a successfully stamped global invoice.
        // StampError invoices do NOT block their sales — the user must be able to retry
        // by generating a new invoice for the same period.
        var globalInvoicedSaleIds = await context.GlobalInvoiceSales
            .AsNoTracking()
            .Where(gs => gs.GlobalInvoice!.Status == GlobalInvoiceStatus.Stamped)
            .Select(gs => gs.SaleId)
            .ToHashSetAsync();

        var alreadyInvoiced = salesInRange.Count(s => invoicedSaleIds.Contains(s.Id));
        var eligible = salesInRange
            .Where(s => !invoicedSaleIds.Contains(s.Id) && !globalInvoicedSaleIds.Contains(s.Id))
            .ToList();

        return (eligible, alreadyInvoiced);
    }

    private static async Task<long> GetNextFolioAsync(ApplicationDbContext context, long startFolio = 1)
    {
        var max = await context.GlobalInvoices
            .AsNoTracking()
            .MaxAsync(i => (long?)i.Folio);
        return max.HasValue ? max.Value + 1 : startFolio;
    }

    /// <summary>
    /// Reads receiver data for Público General from the catalog customer with Id=1.
    /// Falls back to SAT constants if the customer has no fiscal profile.
    /// </summary>
    private async Task<(string Name, string FiscalRegime, string CfdiUse)> GetPublicGeneralReceiverAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var profile = await context.Customers
                .AsNoTracking()
                .Where(c => c.Id == WellKnownIds.PublicGeneralCustomerId)
                .Select(c => c.FiscalProfile)
                .FirstOrDefaultAsync();

            if (profile != null)
            {
                return (
                    Name: !string.IsNullOrWhiteSpace(profile.LegalName) ? profile.LegalName : PublicName,
                    FiscalRegime: !string.IsNullOrWhiteSpace(profile.FiscalRegime) ? profile.FiscalRegime : PublicFiscalRegime,
                    CfdiUse: !string.IsNullOrWhiteSpace(profile.DefaultCfdiUse) ? profile.DefaultCfdiUse : PublicCfdiUse
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load Público General customer from catalog — using SAT defaults");
        }

        return (PublicName, PublicFiscalRegime, PublicCfdiUse);
    }

    private static Comprobante BuildComprobante(
        GlobalInvoice invoice,
        string serie, long folio, int folioLength,
        decimal subtotal, decimal discountAmount, decimal taxAmount, decimal total,
        decimal taxableSubtotal, decimal taxableDiscount, decimal taxableTaxAmount, decimal taxRate,
        decimal exemptSubtotal, decimal exemptDiscount,
        string paymentForm, string periodicidadCode, string periodMonth, string periodYear,
        DateTime issueDate,
        string receiverName, string receiverFiscalRegime, string receiverCfdiUse)
    {
        var folioStr = folioLength > 0
            ? folio.ToString().PadLeft(folioLength, '0')
            : folio.ToString();

        var conceptos = new List<Concepto>();

        // Compute taxable concepto's tax amount from the base (not from summed DB values).
        // This is the authoritative value — the comprobante-level Traslado.Importe must equal
        // the rounded sum of concept-level Traslado.Importe (SAT rule CFDI40221).
        decimal conceptoTaxRounded = 0m;

        // Concepto for taxable sales (TasaOCuota = exact configured rate, e.g. 0.160000)
        if (taxableSubtotal > 0 && taxRate > 0)
        {
            var taxBase = Math.Round(taxableSubtotal - taxableDiscount, 6);
            var conceptoTax = Math.Round(taxBase * taxRate, 6);
            conceptoTaxRounded = Math.Round(conceptoTax, 2);
            conceptos.Add(new Concepto
            {
                ClaveProdServ = GlobalProductCode,
                Cantidad = 1,
                ClaveUnidad = GlobalUnitCode,
                Descripcion = GlobalDescription,
                ValorUnitario = taxableSubtotal,
                Importe = taxableSubtotal,
                Descuento = taxableDiscount,
                ObjetoImp = "02",
                Impuestos = new ConceptoImpuestos
                {
                    Traslados = new List<ConceptoTraslado>
                    {
                        new ConceptoTraslado
                        {
                            Base = taxBase,
                            Impuesto = IvaCode,
                            TipoFactor = IvaFactorType,
                            TasaOCuota = taxRate,
                            Importe = conceptoTax
                        }
                    }
                }
            });
        }

        // Concepto for exempt/zero-tax sales
        if (exemptSubtotal > 0)
        {
            conceptos.Add(new Concepto
            {
                ClaveProdServ = GlobalProductCode,
                Cantidad = 1,
                ClaveUnidad = GlobalUnitCode,
                Descripcion = GlobalDescription,
                ValorUnitario = exemptSubtotal,
                Importe = exemptSubtotal,
                Descuento = exemptDiscount,
                ObjetoImp = "01"
            });
        }

        // Fallback: if somehow both groups are empty, create a zero concepto
        if (conceptos.Count == 0)
        {
            conceptos.Add(new Concepto
            {
                ClaveProdServ = GlobalProductCode,
                Cantidad = 1,
                ClaveUnidad = GlobalUnitCode,
                Descripcion = GlobalDescription,
                ValorUnitario = subtotal,
                Importe = subtotal,
                Descuento = discountAmount,
                ObjetoImp = "01"
            });
        }

        // Impuestos at Comprobante level — only if there are taxable sales.
        // Importe and TotalImpuestosTrasladados must equal the rounded sum of concept-level
        // Traslado.Importe values (SAT rule CFDI40221), so we reuse conceptoTaxRounded.
        Impuestos? impuestos = null;
        if (conceptoTaxRounded > 0 && taxRate > 0)
        {
            var taxBase = Math.Round(taxableSubtotal - taxableDiscount, 2);
            impuestos = new Impuestos
            {
                TotalImpuestosTrasladados = conceptoTaxRounded,
                Traslados = new List<Traslado>
                {
                    new Traslado
                    {
                        Base = taxBase,
                        Impuesto = IvaCode,
                        TipoFactor = IvaFactorType,
                        TasaOCuota = taxRate,
                        Importe = conceptoTaxRounded
                    }
                }
            };
        }

        // Total must equal SubTotal - Descuento + TotalImpuestosTrasladados (SAT rule CFDI40119).
        // Using the DB-summed total causes a 1-cent mismatch when conceptoTaxRounded differs
        // from the accumulated sum of individually-rounded sale taxes.
        var comprobanteTotal = Math.Round(subtotal - discountAmount + conceptoTaxRounded, 2);

        return new Comprobante
        {
            Serie = serie,
            Folio = folioStr,
            Fecha = issueDate.ToString("yyyy-MM-ddTHH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture),
            Sello = "",
            FormaPago = paymentForm,
            NoCertificado = "",
            Certificado = "",
            SubTotal = subtotal,
            Descuento = discountAmount,
            Total = comprobanteTotal,
            TipoDeComprobante = "I",
            Exportacion = "01",
            MetodoPago = PublicPaymentMethod,
            LugarExpedicion = invoice.IssuerPostalCode,
            InformacionGlobal = new InformacionGlobal
            {
                Periodicidad = periodicidadCode,
                Meses = periodMonth,
                Anio = periodYear
            },
            Emisor = new Emisor
            {
                Rfc = invoice.IssuerRfc,
                Nombre = invoice.IssuerLegalName,
                RegimenFiscal = invoice.IssuerFiscalRegime
            },
            Receptor = new Receptor
            {
                Rfc = PublicRfc,
                Nombre = receiverName,
                DomicilioFiscalReceptor = invoice.IssuerPostalCode,
                RegimenFiscalReceptor = receiverFiscalRegime,
                UsoCFDI = receiverCfdiUse
            },
            Conceptos = conceptos,
            Impuestos = impuestos
        };
    }

    private async Task<Result<GlobalInvoiceDto>> MarkStampErrorAsync(
        ApplicationDbContext context, GlobalInvoice invoice, string error)
    {
        invoice.Status = GlobalInvoiceStatus.StampError;
        invoice.StampError = error.Length > 1000 ? error[..1000] : error;
        invoice.ModifiedBy = await _currentUserService.GetUserIdAsync();
        invoice.ModifiedAt = _dateTime.Now;
        await context.SaveChangesAsync();

        _logger.LogError("Global invoice stamp error for invoice {Id}: {Error}", invoice.Id, error);
        return Result<GlobalInvoiceDto>.Success(MapToDto(invoice));
    }

    private Dictionary<string, object> BuildGlobalInvoiceTemplateData(
        GlobalInvoice invoice, TimeZoneInfo tz, string paymentFormDesc, string logoBase64,
        bool isCancelled, bool isPreview,
        string receiverName = PublicName, string receiverFiscalRegime = PublicFiscalRegime, string receiverCfdiUse = PublicCfdiUse)
    {
        string FormatDate(DateTime utc) =>
            TimeZoneInfo.ConvertTimeFromUtc(utc, tz).ToString("dd/MM/yyyy");
        string FormatDateTime(DateTime utc) =>
            TimeZoneInfo.ConvertTimeFromUtc(utc, tz).ToString("dd/MM/yyyy HH:mm:ss");

        var folioDisplay = string.IsNullOrEmpty(invoice.Serie) || invoice.Serie == "—"
            ? (invoice.Folio == 0 ? "—" : invoice.Folio.ToString())
            : $"{invoice.Serie}{invoice.Folio}";

        var qrCode = string.Empty;
        if (!string.IsNullOrEmpty(invoice.Uuid) && !string.IsNullOrEmpty(invoice.SelloCfdi))
        {
            var fe = invoice.SelloCfdi.Length >= 8 ? invoice.SelloCfdi[^8..] : invoice.SelloCfdi;
            var qrUrl = $"https://verificacfdi.facturaelectronica.sat.gob.mx/default.aspx" +
                        $"?id={invoice.Uuid}&re={invoice.IssuerRfc}&rr={PublicRfc}" +
                        $"&tt={invoice.Total:F6}&fe={fe}";
            qrCode = GenerateQrCodeBase64(qrUrl);
        }

        var periodicityDisplay = invoice.Periodicity switch
        {
            GlobalInvoicePeriodicity.Daily => "01 — Diaria",
            GlobalInvoicePeriodicity.Weekly => "02 — Semanal",
            GlobalInvoicePeriodicity.Biweekly => "03 — Quincenal",
            GlobalInvoicePeriodicity.Monthly => "04 — Mensual",
            _ => "04 — Mensual"
        };

        var item = new Dictionary<string, object>
        {
            { "sat_code", GlobalProductCode },
            { "description", GlobalDescription },
            { "quantity", "1" },
            { "unit_code", GlobalUnitCode },
            { "unit_name", "Actividad" },
            { "tax_object", "02 - Sí objeto de impuesto" },
            { "unit_price", invoice.Subtotal.ToString("N2") },
            { "discount", invoice.DiscountAmount.ToString("N2") },
            { "has_discount", invoice.DiscountAmount > 0 },
            { "amount", invoice.Subtotal.ToString("N2") }
        };

        return new Dictionary<string, object>
        {
            { "culture", "es" },
            { "app_name", _applicationOptions.Name },
            { "issuer_legal_name", invoice.IssuerLegalName },
            { "issuer_rfc", invoice.IssuerRfc },
            { "issuer_fiscal_regime", invoice.IssuerFiscalRegime },
            { "issuer_postal_code", invoice.IssuerPostalCode },
            { "serie", invoice.Serie ?? string.Empty },
            { "folio", invoice.Folio.ToString() },
            { "folio_display", folioDisplay },
            { "issue_date", invoice.StampDate.HasValue ? FormatDateTime(invoice.StampDate.Value) : string.Empty },
            { "stamp_date", invoice.StampDate.HasValue ? FormatDateTime(invoice.StampDate.Value) : string.Empty },
            { "uuid", invoice.Uuid ?? string.Empty },
            { "no_cert_cfdi", invoice.NoCertificadoCfdi ?? string.Empty },
            { "no_cert_sat", invoice.NoCertificadoSat ?? string.Empty },
            { "sello_cfdi", invoice.SelloCfdi ?? string.Empty },
            { "sello_sat", invoice.SelloSat ?? string.Empty },
            { "cadena_original", invoice.CadenaOriginalSat ?? string.Empty },
            { "qr_code", qrCode },
            { "payment_form", invoice.PaymentForm },
            { "payment_form_description", paymentFormDesc },
            { "payment_method", PublicPaymentMethod },
            { "payment_method_description", "Pago en una exhibición" },
            { "currency", "MXN" },
            { "customer_legal_name", receiverName },
            { "customer_rfc", PublicRfc },
            { "customer_fiscal_regime", receiverFiscalRegime },
            { "customer_fiscal_regime_description", "Sin obligaciones fiscales" },
            { "customer_postal_code", invoice.IssuerPostalCode },
            { "cfdi_use", receiverCfdiUse },
            { "cfdi_use_description", "Sin efectos fiscales" },
            { "subtotal", invoice.Subtotal.ToString("N2") },
            { "discount", invoice.DiscountAmount.ToString("N2") },
            { "tax_amount", invoice.TaxAmount.ToString("N2") },
            { "total", invoice.Total.ToString("N2") },
            { "items", new List<object> { item } },
            { "has_pdf", !isPreview },
            { "date_year", (object)_dateTime.Now.Year },
            { "is_cancelled", (object)isCancelled },
            { "cancellation_date", invoice.CancellationDate.HasValue ? FormatDateTime(invoice.CancellationDate.Value) : string.Empty },
            { "is_preview", (object)isPreview },
            { "company_logo_url", logoBase64 },
            // Global-specific fields (only shown when present in template)
            { "period_start_date", FormatDate(invoice.StartDate) },
            { "period_end_date", FormatDate(invoice.EndDate) },
            { "period_month", invoice.PeriodMonth },
            { "period_year", (object)invoice.PeriodYear },
            { "periodicity", periodicityDisplay },
            { "sale_count", (object)invoice.SaleCount }
        };
    }

    private static string GenerateQrCodeBase64(string text)
    {
        try
        {
            using var qrGenerator = new QRCoder.QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(text, QRCoder.QRCodeGenerator.ECCLevel.M);
            var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
            return "data:image/png;base64," + Convert.ToBase64String(qrCode.GetGraphic(3));
        }
        catch
        {
            return string.Empty;
        }
    }

    private static GlobalInvoiceListDto MapToListDto(GlobalInvoice e) => new()
    {
        Id = e.Id,
        Serie = e.Serie,
        Folio = e.Folio,
        Uuid = e.Uuid,
        Periodicity = e.Periodicity,
        StartDate = e.StartDate,
        EndDate = e.EndDate,
        PaymentForm = e.PaymentForm,
        SaleCount = e.SaleCount,
        Total = e.Total,
        Status = e.Status,
        StampDate = e.StampDate,
        CancellationDate = e.CancellationDate,
        CancellationReason = e.CancellationReason,
        CancellationNotes = e.CancellationNotes,
        CancellationStatus = e.CancellationStatus,
        HasCancellationAcuse = e.CancellationAcuse != null,
        CreatedAt = e.CreatedAt,
        CreatedBy = e.CreatedBy ?? string.Empty
    };

    private static GlobalInvoiceDto MapToDto(GlobalInvoice e) => new()
    {
        Id = e.Id,
        Serie = e.Serie,
        Folio = e.Folio,
        Uuid = e.Uuid,
        Periodicity = e.Periodicity,
        StartDate = e.StartDate,
        EndDate = e.EndDate,
        PaymentForm = e.PaymentForm,
        SaleCount = e.SaleCount,
        Total = e.Total,
        Status = e.Status,
        StampDate = e.StampDate,
        CreatedAt = e.CreatedAt,
        CreatedBy = e.CreatedBy ?? string.Empty,
        Subtotal = e.Subtotal,
        DiscountAmount = e.DiscountAmount,
        TaxAmount = e.TaxAmount,
        PeriodMonth = e.PeriodMonth,
        PeriodYear = e.PeriodYear,
        IssuerRfc = e.IssuerRfc,
        IssuerLegalName = e.IssuerLegalName,
        IssuerFiscalRegime = e.IssuerFiscalRegime,
        IssuerPostalCode = e.IssuerPostalCode,
        StampError = e.StampError,
        HasCancellationAcuse = e.CancellationAcuse != null,
        CancellationDate = e.CancellationDate,
        CancellationReason = e.CancellationReason,
        CancellationNotes = e.CancellationNotes,
        CancellationStatus = e.CancellationStatus
    };
}
