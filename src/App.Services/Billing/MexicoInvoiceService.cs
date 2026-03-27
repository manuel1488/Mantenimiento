using System.Text;
using App.Core.Common;
using App.Core.DTOs.Billing;
using App.Core.DTOs.Billing.Mexico;
using App.Core.Enums.Billing;
using App.Core.Enums.Shop;
using App.Core.Interfaces;
using App.Core.Interfaces.Billing;
using App.Core.Models.Cfdi.V40;
using App.Core.Models.Email;
using App.Core.Options;
using App.Models.Billing;
using App.Models.Data.Contexts;
using App.Models.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using App.Core.DTOs.Settings;
using App.Shared.Services;
using System.Globalization;

namespace App.Services.Billing;

public class MexicoInvoiceService : IMexicoInvoiceService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMexicoCfdiXmlService _xmlService;
    private readonly IMexicoCsdSigningService _signingService;
    private readonly ISwSapienService _pacService;
    private readonly IMexicoPacSettingsService _pacSettingsService;
    private readonly ITaxSettingsService _taxSettingsService;
    private readonly IMexicoStampAlertService _stampAlertService;
    private readonly IPdfService _pdfService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IStringLocalizer<MexicoInvoiceService> _localizer;
    private readonly ApplicationOptions _applicationOptions;
    private readonly IDateTime _dateTime;
    private readonly ICompanySettingsService _companySettingsService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<MexicoInvoiceService> _logger;

    private const string DefaultProductServiceCode = "01010101"; // No identificado
    private const string DefaultUnitCode = "H87"; // Pieza (SAT standard)
    private const string IvaCode = "002";
    private const string IvaFactorType = "Tasa";
    private const string RoundingProductServiceCode = "84111506"; // Servicios de facturación
    private const string RoundingUnitCode = "ACT"; // Actividad
    private const string RoundingDescription = "Ajuste por redondeo";

    public MexicoInvoiceService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMexicoCfdiXmlService xmlService,
        IMexicoCsdSigningService signingService,
        ISwSapienService pacService,
        IMexicoPacSettingsService pacSettingsService,
        ITaxSettingsService taxSettingsService,
        IMexicoStampAlertService stampAlertService,
        IPdfService pdfService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IStringLocalizer<MexicoInvoiceService> localizer,
        IOptions<ApplicationOptions> applicationOptions,
        IDateTime dateTime,
        ICompanySettingsService companySettingsService,
        ICurrentUserService currentUserService,
        ILogger<MexicoInvoiceService> logger)
    {
        _contextFactory = contextFactory;
        _xmlService = xmlService;
        _signingService = signingService;
        _pacService = pacService;
        _pacSettingsService = pacSettingsService;
        _taxSettingsService = taxSettingsService;
        _stampAlertService = stampAlertService;
        _pdfService = pdfService;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _localizer = localizer;
        _applicationOptions = applicationOptions.Value;
        _dateTime = dateTime;
        _companySettingsService = companySettingsService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<MexicoInvoiceDto>> CreateAndStampAsync(CreateMexicoInvoiceDto dto)
    {
        try
        {
            _logger.LogInformation("Creating and stamping invoice for sale {SaleId}", dto.SaleId);

            // 1. Validate sale
            var validationResult = await ValidateSaleForInvoicingAsync(dto.SaleId);
            if (!validationResult.IsSuccess)
                return Result<MexicoInvoiceDto>.Failure(validationResult.Error!);

            // 2. Load PAC settings
            var pacSettings = await _pacSettingsService.GetAsync();
            if (pacSettings == null || !pacSettings.IsConfigured)
                return Result<MexicoInvoiceDto>.Failure(
                    "La configuración fiscal (PAC/CSD) no está completa. Configure en Administración > Configuración Fiscal.");

            // 2b. Load issuer data from Fiscal (Tax) settings
            var taxSettings = await _taxSettingsService.GetSettingsAsync();
            if (taxSettings == null || string.IsNullOrEmpty(taxSettings.TaxId))
                return Result<MexicoInvoiceDto>.Failure(
                    "Los datos fiscales del emisor no están configurados. Configure en Administración > Configuración > Fiscal.");

            // 3. Load sale with details
            await using var context = await _contextFactory.CreateDbContextAsync();
            var sale = await context.Sales
                .Include(s => s.Customer)
                .Include(s => s.Details)
                    .ThenInclude(d => d.Product)
                        .ThenInclude(p => p.MexicoProductService)
                .Include(s => s.Details)
                    .ThenInclude(d => d.Product)
                        .ThenInclude(p => p.UnitMeasure)
                            .ThenInclude(u => u.MexicoSatUnit)
                .FirstAsync(s => s.Id == dto.SaleId);

            // 4. Get next folio
            var folio = await GetNextFolioAsync();
            var serie = pacSettings.InvoiceSerie ?? "A";
            var folioLength = pacSettings.FolioLength;

            // 5. Create draft invoice record
            var invoice = new MexicoInvoice
            {
                SaleId = dto.SaleId,
                Serie = serie,
                Folio = folio,
                CfdiUse = dto.CfdiUse,
                PaymentForm = dto.PaymentForm,
                PaymentMethod = dto.PaymentMethod,
                CustomerRfc = dto.CustomerRfc,
                CustomerLegalName = dto.CustomerLegalName,
                CustomerPostalCode = dto.CustomerPostalCode,
                CustomerFiscalRegime = dto.CustomerFiscalRegime,
                IssuerRfc = taxSettings.TaxId,
                IssuerLegalName = taxSettings.BusinessName,
                IssuerFiscalRegime = taxSettings.FiscalRegime,
                IssuerPostalCode = taxSettings.PostalCode ?? string.Empty,
                Subtotal = sale.Subtotal,
                TaxAmount = sale.TaxAmount,
                Total = sale.Total,
                Currency = "MXN",
                ExchangeRate = 1,
                Status = "Draft",
                IsStamped = false,
                CreatedBy = _currentUserService.UserId,
                CreatedAt = _dateTime.Now,
                ModifiedBy = _currentUserService.UserId,
                ModifiedAt = _dateTime.Now
            };

            context.MexicoInvoices.Add(invoice);
            await context.SaveChangesAsync();

            try
            {
                // 6. Build Comprobante — resolve local time from issuer postal code timezone
                // Using postal code timezone ensures the CFDI Fecha matches the issuer's local time,
                // preventing PAC rejection when the issuer is in a different timezone than Mexico City.
                TimeZoneInfo issuerTimeZone;
                if (!string.IsNullOrEmpty(taxSettings.PostalCodeIanaTimeZoneId))
                {
                    issuerTimeZone = TimeZoneInfo.FindSystemTimeZoneById(taxSettings.PostalCodeIanaTimeZoneId);
                }
                else
                {
                    _logger.LogWarning("Postal code timezone not configured — falling back to company timezone for CFDI Fecha");
                    issuerTimeZone = await _companySettingsService.GetCurrentTimeZoneAsync();
                }
                var issueDate = TimeZoneInfo.ConvertTimeFromUtc(_dateTime.Now, issuerTimeZone);
                var comprobante = BuildComprobante(invoice, sale, serie, folio, folioLength, dto, issueDate);

                // Update invoice record with CFDI-computed totals (may include rounding concepto)
                invoice.Subtotal = comprobante.SubTotal;
                invoice.TaxAmount = comprobante.Impuestos?.TotalImpuestosTrasladados ?? 0;
                invoice.Total = comprobante.Total;

                // 7. Generate XML
                var xmlResult = await _xmlService.GenerateXmlAsync(comprobante);
                if (!xmlResult.IsSuccess)
                    return await MarkStampError(context, invoice, xmlResult.Error!);

                // 8. Load CSD and sign
                var certResult = await _pacSettingsService.GetCsdCertificateBytesAsync();
                var keyResult = await _pacSettingsService.GetCsdPrivateKeyBytesAsync();
                var pwdResult = await _pacSettingsService.GetCsdPasswordAsync();

                if (!certResult.IsSuccess) return await MarkStampError(context, invoice, certResult.Error!, xmlResult.Value);
                if (!keyResult.IsSuccess) return await MarkStampError(context, invoice, keyResult.Error!, xmlResult.Value);
                if (!pwdResult.IsSuccess) return await MarkStampError(context, invoice, pwdResult.Error!, xmlResult.Value);

                var signedXmlResult = await _signingService.SignXmlAsync(
                    xmlResult.Value!, certResult.Value!, keyResult.Value!, pwdResult.Value!);

                if (!signedXmlResult.IsSuccess)
                    return await MarkStampError(context, invoice, signedXmlResult.Error!, xmlResult.Value);

                // 9. Stamp with PAC — pass signed XML so it can be reviewed on error
                var stampResult = await _pacService.StampAsync(signedXmlResult.Value!);
                if (!stampResult.IsSuccess)
                    return await MarkStampError(context, invoice, stampResult.Error!, signedXmlResult.Value);

                var stamp = stampResult.Value!;

                // 10. Update invoice with stamp data
                invoice.Uuid = stamp.Uuid;
                invoice.StampDate = _dateTime.Now;
                invoice.IsStamped = true;
                invoice.Status = "Stamped";
                invoice.NoCertificadoSat = stamp.NoCertificadoSat;
                invoice.NoCertificadoCfdi = stamp.NoCertificadoCfdi;
                invoice.SelloSat = stamp.SelloSat;
                invoice.SelloCfdi = stamp.SelloCfdi;
                invoice.CadenaOriginalSat = stamp.CadenaOriginalSat;
                invoice.StampError = null;
                invoice.ModifiedBy = _currentUserService.UserId;
                invoice.ModifiedAt = _dateTime.Now;

                // 11. Save stamped XML
                var stampedXml = stamp.Cfdi ?? signedXmlResult.Value!;
                context.MexicoInvoiceFiles.Add(new MexicoInvoiceFile
                {
                    InvoiceId = invoice.Id,
                    FileType = "XML",
                    FileData = System.Text.Encoding.UTF8.GetBytes(stampedXml),
                    CreatedBy = _currentUserService.UserId,
                    CreatedAt = _dateTime.Now,
                    ModifiedBy = _currentUserService.UserId,
                    ModifiedAt = _dateTime.Now
                });

                await context.SaveChangesAsync();

                // 12. Generate and save PDF using the configured invoice template
                try
                {
                    var folioDisplay = string.IsNullOrEmpty(invoice.Serie)
                        ? invoice.Folio.ToString()
                        : $"{invoice.Serie}{invoice.Folio}";
                    var pdfItems = sale.Details.Select(d => (object)new Dictionary<string, object>
                    {
                        { "sat_code", (object)(d.Product.MexicoProductService?.Code ?? DefaultProductServiceCode) },
                        { "description", d.Product.Name },
                        { "quantity", d.Quantity % 1 == 0 ? ((int)d.Quantity).ToString() : d.Quantity.ToString("G29") },
                        { "unit_price", d.UnitPrice.ToString("N2") },
                        { "discount", d.DiscountAmount > 0 ? d.DiscountAmount.ToString("N2") : string.Empty },
                        { "has_discount", (object)(d.DiscountAmount > 0) },
                        { "amount", d.Total.ToString("N2") }
                    }).ToList();
                    var logoBase64 = await _emailTemplateService.GetStaticFileBase64Async("images/logo.webp");
                    var discountTotal = sale.Details.Sum(d => d.DiscountAmount);
                    var (formDesc2, methodDesc2) = await GetPaymentDescriptionsAsync(context, invoice.PaymentForm, invoice.PaymentMethod);
                    var pdfData = BuildInvoiceTemplateData(invoice, folioDisplay, pdfItems, hasPdf: true,
                        discountTotal: discountTotal, logoBase64: logoBase64, serie: invoice.Serie ?? string.Empty,
                        paymentFormDescription: formDesc2, paymentMethodDescription: methodDesc2);
                    var html = await _emailTemplateService.GetTemplateAsync("invoice-cfdi", pdfData);
                    var pdf = await _pdfService.GeneratePdfFromHtmlAsync(html);
                    context.MexicoInvoiceFiles.Add(new MexicoInvoiceFile
                    {
                        InvoiceId = invoice.Id,
                        FileType = "PDF",
                        FileData = pdf,
                        CreatedBy = _currentUserService.UserId,
                        CreatedAt = _dateTime.Now,
                        ModifiedBy = _currentUserService.UserId,
                        ModifiedAt = _dateTime.Now
                    });
                    await context.SaveChangesAsync();
                }
                catch (Exception pdfEx)
                {
                    _logger.LogWarning(pdfEx, "PDF generation failed for invoice {InvoiceId}, continuing", invoice.Id);
                }

                // 13. Send by email if requested
                if (!string.IsNullOrWhiteSpace(dto.SendToEmail))
                {
                    try
                    {
                        await SendByEmailAsync(invoice.Id, dto.SendToEmail);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogWarning(emailEx, "Email send failed for invoice {InvoiceId}", invoice.Id);
                    }
                }

                // 14. Non-blocking stamp alert check (fire-and-forget)
                _ = Task.Run(() => _stampAlertService.CheckAndAlertIfNeededAsync());

                var result = await BuildInvoiceDtoFromEntity(invoice);
                result.HasXml = true;
                _logger.LogInformation("Invoice {InvoiceId} stamped successfully. UUID: {Uuid}",
                    invoice.Id, invoice.Uuid);

                return Result<MexicoInvoiceDto>.Success(result);
            }
            catch (Exception ex)
            {
                return await MarkStampError(context, invoice, ex.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice for sale {SaleId}", dto.SaleId);
            return Result<MexicoInvoiceDto>.Failure($"Error al crear la factura: {ex.Message}");
        }
    }

    public async Task<Result> RegeneratePdfAsync(long invoiceId)
    {
        try
        {
            _logger.LogInformation("Regenerating PDF for invoice {InvoiceId}", invoiceId);

            await using var context = await _contextFactory.CreateDbContextAsync();
            var invoice = await context.MexicoInvoices.FindAsync(invoiceId);

            if (invoice == null)
                return Result.Failure(_localizer["Invoice not found"]);

            if (!invoice.IsStamped)
                return Result.Failure(_localizer["Only stamped invoices can have PDF regenerated"]);

            // Load sale with details for PDF data
            var sale = await context.Sales
                .Include(s => s.Customer)
                .Include(s => s.Details)
                    .ThenInclude(d => d.Product)
                        .ThenInclude(p => p.MexicoProductService)
                .FirstOrDefaultAsync(s => s.Id == invoice.SaleId);

            if (sale == null)
                return Result.Failure(_localizer["Sale not found"]);

            // Remove existing PDF file if any
            var existingPdf = await context.MexicoInvoiceFiles
                .FirstOrDefaultAsync(f => f.InvoiceId == invoiceId && f.FileType == "PDF");
            if (existingPdf != null)
            {
                existingPdf.DeletedBy = _currentUserService.UserId;
                existingPdf.DeletedAt = _dateTime.Now;
                context.MexicoInvoiceFiles.Remove(existingPdf);
            }

            // Generate PDF
            var folioDisplay = string.IsNullOrEmpty(invoice.Serie)
                ? invoice.Folio.ToString()
                : $"{invoice.Serie}{invoice.Folio}";
            var pdfItems = sale.Details.Select(d => (object)new Dictionary<string, object>
            {
                { "sat_code", (object)(d.Product.MexicoProductService?.Code ?? DefaultProductServiceCode) },
                { "description", d.Product.Name },
                { "quantity", d.Quantity % 1 == 0 ? ((int)d.Quantity).ToString() : d.Quantity.ToString("G29") },
                { "unit_price", d.UnitPrice.ToString("N2") },
                { "discount", d.DiscountAmount > 0 ? d.DiscountAmount.ToString("N2") : string.Empty },
                { "has_discount", (object)(d.DiscountAmount > 0) },
                { "amount", d.Total.ToString("N2") }
            }).ToList();
            var logoBase64 = await _emailTemplateService.GetStaticFileBase64Async("images/logo.webp");
            var discountTotal = sale.Details.Sum(d => d.DiscountAmount);
            var (formDesc, methodDesc) = await GetPaymentDescriptionsAsync(context, invoice.PaymentForm, invoice.PaymentMethod);
            var pdfData = BuildInvoiceTemplateData(invoice, folioDisplay, pdfItems, hasPdf: true,
                discountTotal: discountTotal, logoBase64: logoBase64, serie: invoice.Serie ?? string.Empty,
                paymentFormDescription: formDesc, paymentMethodDescription: methodDesc);
            var html = await _emailTemplateService.GetTemplateAsync("invoice-cfdi", pdfData);
            var pdf = await _pdfService.GeneratePdfFromHtmlAsync(html);

            context.MexicoInvoiceFiles.Add(new MexicoInvoiceFile
            {
                InvoiceId = invoice.Id,
                FileType = "PDF",
                FileData = pdf,
                CreatedBy = _currentUserService.UserId,
                CreatedAt = _dateTime.Now,
                ModifiedBy = _currentUserService.UserId,
                ModifiedAt = _dateTime.Now
            });

            await context.SaveChangesAsync();
            _logger.LogInformation("PDF regenerated for invoice {InvoiceId}", invoiceId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating PDF for invoice {InvoiceId}", invoiceId);
            return Result.Failure($"Error al regenerar el PDF: {ex.Message}");
        }
    }

    public async Task<Result<MexicoInvoiceDto>> RetryStampAsync(long invoiceId)
    {
        try
        {
            _logger.LogInformation("Retrying stamp for invoice {InvoiceId}", invoiceId);

            await using var context = await _contextFactory.CreateDbContextAsync();
            var invoice = await context.MexicoInvoices.FindAsync(invoiceId);

            if (invoice == null)
                return Result<MexicoInvoiceDto>.Failure(_localizer["Invoice not found"]);

            if (invoice.Status != "StampError")
                return Result<MexicoInvoiceDto>.Failure(_localizer["Only invoices with stamp errors can be retried"]);

            // Load PAC settings
            var pacSettings = await _pacSettingsService.GetAsync();
            if (pacSettings == null || !pacSettings.IsConfigured)
                return Result<MexicoInvoiceDto>.Failure(
                    "La configuración fiscal (PAC/CSD) no está completa. Configure en Administración > Configuración Fiscal.");

            // Load issuer data
            var taxSettings = await _taxSettingsService.GetSettingsAsync();
            if (taxSettings == null || string.IsNullOrEmpty(taxSettings.TaxId))
                return Result<MexicoInvoiceDto>.Failure(
                    "Los datos fiscales del emisor no están configurados. Configure en Administración > Configuración > Fiscal.");

            // Load sale with details
            var sale = await context.Sales
                .Include(s => s.Customer)
                .Include(s => s.Details)
                    .ThenInclude(d => d.Product)
                        .ThenInclude(p => p.MexicoProductService)
                .Include(s => s.Details)
                    .ThenInclude(d => d.Product)
                        .ThenInclude(p => p.UnitMeasure)
                            .ThenInclude(u => u.MexicoSatUnit)
                .FirstOrDefaultAsync(s => s.Id == invoice.SaleId);

            if (sale == null)
                return Result<MexicoInvoiceDto>.Failure(_localizer["Sale not found"]);

            // Remove old files (error XML) so they get replaced
            var oldFiles = await context.MexicoInvoiceFiles
                .Where(f => f.InvoiceId == invoiceId)
                .ToListAsync();
            foreach (var file in oldFiles)
            {
                file.DeletedBy = _currentUserService.UserId;
                file.DeletedAt = _dateTime.Now;
            }
            context.MexicoInvoiceFiles.RemoveRange(oldFiles);

            try
            {
                // Build Comprobante with fresh timestamp
                TimeZoneInfo issuerTimeZone;
                if (!string.IsNullOrEmpty(taxSettings.PostalCodeIanaTimeZoneId))
                {
                    issuerTimeZone = TimeZoneInfo.FindSystemTimeZoneById(taxSettings.PostalCodeIanaTimeZoneId);
                }
                else
                {
                    _logger.LogWarning("Postal code timezone not configured — falling back to company timezone for CFDI Fecha");
                    issuerTimeZone = await _companySettingsService.GetCurrentTimeZoneAsync();
                }
                var issueDate = TimeZoneInfo.ConvertTimeFromUtc(_dateTime.Now, issuerTimeZone);

                // Use fresh customer data from DB (not stale invoice data)
                // so that corrections to customer fiscal info are picked up on retry
                var freshRfc = sale.Customer?.TaxId ?? invoice.CustomerRfc;
                var freshLegalName = sale.Customer?.LegalName ?? invoice.CustomerLegalName;
                var freshPostalCode = sale.Customer?.PostalCode ?? invoice.CustomerPostalCode;
                var freshFiscalRegime = sale.Customer?.FiscalRegime ?? invoice.CustomerFiscalRegime;

                // Update invoice record with corrected customer data
                invoice.CustomerRfc = freshRfc;
                invoice.CustomerLegalName = freshLegalName;
                invoice.CustomerPostalCode = freshPostalCode;
                invoice.CustomerFiscalRegime = freshFiscalRegime;

                var dto = new CreateMexicoInvoiceDto
                {
                    SaleId = invoice.SaleId,
                    CustomerRfc = freshRfc,
                    CustomerLegalName = freshLegalName,
                    CustomerPostalCode = freshPostalCode,
                    CustomerFiscalRegime = freshFiscalRegime,
                    CfdiUse = invoice.CfdiUse,
                    PaymentForm = invoice.PaymentForm,
                    PaymentMethod = invoice.PaymentMethod
                };

                var serie = invoice.Serie ?? "A";
                var folioLength = pacSettings.FolioLength;
                var comprobante = BuildComprobante(invoice, sale, serie, invoice.Folio, folioLength, dto, issueDate);

                // Generate XML
                var xmlResult = await _xmlService.GenerateXmlAsync(comprobante);
                if (!xmlResult.IsSuccess)
                    return await MarkStampError(context, invoice, xmlResult.Error!);

                // Load CSD and sign
                var certResult = await _pacSettingsService.GetCsdCertificateBytesAsync();
                var keyResult = await _pacSettingsService.GetCsdPrivateKeyBytesAsync();
                var pwdResult = await _pacSettingsService.GetCsdPasswordAsync();

                if (!certResult.IsSuccess) return await MarkStampError(context, invoice, certResult.Error!, xmlResult.Value);
                if (!keyResult.IsSuccess) return await MarkStampError(context, invoice, keyResult.Error!, xmlResult.Value);
                if (!pwdResult.IsSuccess) return await MarkStampError(context, invoice, pwdResult.Error!, xmlResult.Value);

                var signedXmlResult = await _signingService.SignXmlAsync(
                    xmlResult.Value!, certResult.Value!, keyResult.Value!, pwdResult.Value!);

                if (!signedXmlResult.IsSuccess)
                    return await MarkStampError(context, invoice, signedXmlResult.Error!, xmlResult.Value);

                // Stamp with PAC
                var stampResult = await _pacService.StampAsync(signedXmlResult.Value!);
                if (!stampResult.IsSuccess)
                    return await MarkStampError(context, invoice, stampResult.Error!, signedXmlResult.Value);

                var stamp = stampResult.Value!;

                // Update invoice with stamp data
                invoice.Uuid = stamp.Uuid;
                invoice.StampDate = _dateTime.Now;
                invoice.IsStamped = true;
                invoice.Status = "Stamped";
                invoice.NoCertificadoSat = stamp.NoCertificadoSat;
                invoice.NoCertificadoCfdi = stamp.NoCertificadoCfdi;
                invoice.SelloSat = stamp.SelloSat;
                invoice.SelloCfdi = stamp.SelloCfdi;
                invoice.CadenaOriginalSat = stamp.CadenaOriginalSat;
                invoice.StampError = null;
                invoice.ModifiedBy = _currentUserService.UserId;
                invoice.ModifiedAt = _dateTime.Now;

                // Save stamped XML
                var stampedXml = stamp.Cfdi ?? signedXmlResult.Value!;
                context.MexicoInvoiceFiles.Add(new MexicoInvoiceFile
                {
                    InvoiceId = invoice.Id,
                    FileType = "XML",
                    FileData = Encoding.UTF8.GetBytes(stampedXml),
                    CreatedBy = _currentUserService.UserId,
                    CreatedAt = _dateTime.Now,
                    ModifiedBy = _currentUserService.UserId,
                    ModifiedAt = _dateTime.Now
                });

                await context.SaveChangesAsync();

                // Generate PDF
                try
                {
                    var folioDisplay = string.IsNullOrEmpty(invoice.Serie)
                        ? invoice.Folio.ToString()
                        : $"{invoice.Serie}{invoice.Folio}";
                    var pdfItems = sale.Details.Select(d => (object)new Dictionary<string, object>
                    {
                        { "sat_code", (object)(d.Product.MexicoProductService?.Code ?? DefaultProductServiceCode) },
                        { "description", d.Product.Name },
                        { "quantity", d.Quantity % 1 == 0 ? ((int)d.Quantity).ToString() : d.Quantity.ToString("G29") },
                        { "unit_price", d.UnitPrice.ToString("N2") },
                        { "discount", d.DiscountAmount > 0 ? d.DiscountAmount.ToString("N2") : string.Empty },
                        { "has_discount", (object)(d.DiscountAmount > 0) },
                        { "amount", d.Total.ToString("N2") }
                    }).ToList();
                    var logoBase64 = await _emailTemplateService.GetStaticFileBase64Async("images/logo.webp");
                    var discountTotal = sale.Details.Sum(d => d.DiscountAmount);
                    var (formDesc2, methodDesc2) = await GetPaymentDescriptionsAsync(context, invoice.PaymentForm, invoice.PaymentMethod);
                    var pdfData = BuildInvoiceTemplateData(invoice, folioDisplay, pdfItems, hasPdf: true,
                        discountTotal: discountTotal, logoBase64: logoBase64, serie: invoice.Serie ?? string.Empty,
                        paymentFormDescription: formDesc2, paymentMethodDescription: methodDesc2);
                    var html = await _emailTemplateService.GetTemplateAsync("invoice-cfdi", pdfData);
                    var pdf = await _pdfService.GeneratePdfFromHtmlAsync(html);
                    context.MexicoInvoiceFiles.Add(new MexicoInvoiceFile
                    {
                        InvoiceId = invoice.Id,
                        FileType = "PDF",
                        FileData = pdf,
                        CreatedBy = _currentUserService.UserId,
                        CreatedAt = _dateTime.Now,
                        ModifiedBy = _currentUserService.UserId,
                        ModifiedAt = _dateTime.Now
                    });
                    await context.SaveChangesAsync();
                }
                catch (Exception pdfEx)
                {
                    _logger.LogWarning(pdfEx, "PDF generation failed for invoice {InvoiceId} on retry, continuing", invoice.Id);
                }

                // Non-blocking stamp alert check
                _ = Task.Run(() => _stampAlertService.CheckAndAlertIfNeededAsync());

                var result = await BuildInvoiceDtoFromEntity(invoice);
                result.HasXml = true;
                _logger.LogInformation("Invoice {InvoiceId} retry stamped successfully. UUID: {Uuid}",
                    invoice.Id, invoice.Uuid);

                return Result<MexicoInvoiceDto>.Success(result);
            }
            catch (Exception ex)
            {
                return await MarkStampError(context, invoice, ex.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying stamp for invoice {InvoiceId}", invoiceId);
            return Result<MexicoInvoiceDto>.Failure($"Error al retimbrar la factura: {ex.Message}");
        }
    }

    public async Task<MexicoInvoiceDto?> GetBySaleIdAsync(long saleId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var invoice = await context.MexicoInvoices
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.SaleId == saleId);

        return invoice == null ? null : await BuildInvoiceDtoFromEntity(invoice);
    }

    public async Task<MexicoInvoiceDto?> GetByIdAsync(long id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var invoice = await context.MexicoInvoices
            .AsNoTracking()
            .Include(i => i.Files)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null) return null;

        var dto = await BuildInvoiceDtoFromEntity(invoice);
        dto.HasXml = invoice.Files.Any(f => f.FileType == "XML");
        dto.HasPdf = invoice.Files.Any(f => f.FileType == "PDF");
        return dto;
    }

    public async Task<(int TotalCount, IList<MexicoInvoiceSummaryDto> Items)> GetHistoryAsync(
        int page = 1,
        int pageSize = 20,
        string? searchString = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? status = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Convert local date boundaries to UTC so they match CreatedAt (stored in UTC)
        var companyTz = await _companySettingsService.GetCurrentTimeZoneAsync() ?? TimeZoneInfo.Utc;

        var query = context.MexicoInvoices.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var s = searchString.Trim();
            if (long.TryParse(s, out var saleIdSearch))
            {
                query = query.Where(i => i.SaleId == saleIdSearch);
            }
            else
            {
                query = query.Where(i =>
                    i.CustomerRfc.Contains(s) ||
                    i.CustomerLegalName.Contains(s) ||
                    (i.Uuid != null && i.Uuid.Contains(s)));
            }
        }

        if (startDate.HasValue)
        {
            var startLocal = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Unspecified);
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, companyTz);
            query = query.Where(i => i.CreatedAt >= startUtc);
        }

        if (endDate.HasValue)
        {
            // End of the selected local day converted to UTC
            var endLocal = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1), DateTimeKind.Unspecified);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, companyTz);
            query = query.Where(i => i.CreatedAt < endUtc);
        }

        if (!string.IsNullOrEmpty(status))
            query = query.Where(i => i.Status == status);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(i => i.Folio)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new MexicoInvoiceSummaryDto
            {
                Id = i.Id,
                SaleId = i.SaleId,
                Serie = i.Serie,
                Folio = i.Folio,
                Uuid = i.Uuid,
                Status = i.Status,
                IsStamped = i.IsStamped,
                StampDate = i.StampDate,
                CustomerRfc = i.CustomerRfc,
                CustomerLegalName = i.CustomerLegalName,
                Total = i.Total,
                CfdiUse = i.CfdiUse,
                CancellationStatus = i.CancellationStatus,
                CancellationDate = i.CancellationDate,
                HasCancellationAcuse = i.CancellationAcuse != null,
                StampError = i.StampError,
                CreatedAt = i.CreatedAt,
                CreatedBy = i.CreatedBy,
                ModifiedAt = i.ModifiedAt,
                ModifiedBy = i.ModifiedBy,
                CustomerEmail = i.Sale.Customer != null ? i.Sale.Customer.Email : null
            })
            .ToListAsync();

        // Resolve HasXml / HasPdf with a separate simple query to avoid
        // correlated-subquery translation issues in Pomelo/MySQL
        if (items.Count > 0)
        {
            var invoiceIds = items.Select(i => i.Id).ToList();
            var fileTypes = await context.MexicoInvoiceFiles
                .AsNoTracking()
                .Where(f => invoiceIds.Contains(f.InvoiceId))
                .Select(f => new { f.InvoiceId, f.FileType })
                .ToListAsync();

            foreach (var item in items)
            {
                item.HasXml = fileTypes.Any(f => f.InvoiceId == item.Id && f.FileType == "XML");
                item.HasPdf = fileTypes.Any(f => f.InvoiceId == item.Id && f.FileType == "PDF");
            }
        }

        return (totalCount, items);
    }

    public async Task<Result<byte[]>> GetXmlAsync(long invoiceId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var file = await context.MexicoInvoiceFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.InvoiceId == invoiceId && f.FileType == "XML");

        return file == null
            ? Result<byte[]>.Failure("No se encontró el XML de la factura")
            : Result<byte[]>.Success(file.FileData);
    }

    public async Task<Result<byte[]>> GetPdfAsync(long invoiceId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var invoice = await context.MexicoInvoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
                return Result<byte[]>.Failure("Factura no encontrada");

            var file = await context.MexicoInvoiceFiles
                .FirstOrDefaultAsync(f => f.InvoiceId == invoiceId && f.FileType == "PDF");

            if (file == null)
                return Result<byte[]>.Failure("No se encontró el PDF de la factura");

            if (invoice.Status == "Cancelled")
            {
                var watermarkedPdf = await RegenerateCancelledPdfAsync(invoice, context);
                if (watermarkedPdf != null)
                {
                    file.FileData = watermarkedPdf;
                    file.ModifiedAt = _dateTime.Now;
                    file.ModifiedBy = _currentUserService.UserId;
                    await context.SaveChangesAsync();
                    return Result<byte[]>.Success(watermarkedPdf);
                }
                // regeneration failed → return original PDF without watermark
            }

            return Result<byte[]>.Success(file.FileData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving PDF for invoice {InvoiceId}", invoiceId);
            return Result<byte[]>.Failure("Error al obtener el PDF de la factura");
        }
    }

    public async Task<Result> SendByEmailAsync(long invoiceId, string email)
    {
        _logger.LogInformation("Email send requested for invoice {InvoiceId} to {Email}", invoiceId, email);
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var invoice = await context.MexicoInvoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
                return Result.Failure("Factura no encontrada");

            var folio = string.IsNullOrEmpty(invoice.Serie)
                ? invoice.Folio.ToString()
                : $"{invoice.Serie}{invoice.Folio}";

            var attachments = new List<EmailAttachment>();

            var xmlFile = await context.MexicoInvoiceFiles
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.InvoiceId == invoiceId && f.FileType == "XML");
            if (xmlFile != null)
            {
                attachments.Add(new EmailAttachment
                {
                    FileName = $"CFDI_{folio}.xml",
                    Content = xmlFile.FileData,
                    ContentType = "application/xml"
                });
            }

            var pdfFile = await context.MexicoInvoiceFiles
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.InvoiceId == invoiceId && f.FileType == "PDF");
            if (pdfFile != null)
            {
                attachments.Add(new EmailAttachment
                {
                    FileName = $"CFDI_{folio}.pdf",
                    Content = pdfFile.FileData,
                    ContentType = "application/pdf"
                });
            }

            var (logoBytes, logoMime) = await _emailTemplateService.GetStaticFileBytesAsync("images/logo.webp");
            var hasLogo = logoBytes.Length > 0;

            var notificationData = new Dictionary<string, object>
            {
                { "culture", "es" },
                { "app_name", _applicationOptions.Name },
                { "folio", folio },
                { "serie", invoice.Serie ?? string.Empty },
                { "folio_number", invoice.Folio.ToString() },
                { "uuid", invoice.Uuid ?? string.Empty },
                { "stamp_date", invoice.StampDate?.ToString("dd/MM/yyyy HH:mm") ?? string.Empty },
                { "customer_legal_name", invoice.CustomerLegalName },
                { "customer_rfc", invoice.CustomerRfc },
                { "issuer_legal_name", invoice.IssuerLegalName },
                { "issuer_rfc", invoice.IssuerRfc },
                { "total", invoice.Total.ToString("N2") },
                { "currency", invoice.Currency },
                { "has_pdf", (object)(pdfFile != null) },
                { "date_year", (object)DateTime.UtcNow.Year },
                { "company_logo_url", hasLogo ? "cid:logo" : string.Empty }
            };
            var body = await _emailTemplateService.GetTemplateAsync("invoice-cfdi-notification", notificationData);

            var linkedResources = new List<EmailLinkedResource>();
            if (hasLogo)
            {
                linkedResources.Add(new EmailLinkedResource
                {
                    ContentId = "logo",
                    Content = logoBytes,
                    ContentType = logoMime
                });
            }

            var message = new EmailMessage
            {
                To = email,
                Subject = _localizer["Electronic Invoice {0} - {1}", folio, invoice.IssuerLegalName],
                Body = body,
                IsHtml = true,
                Attachments = attachments,
                LinkedResources = linkedResources
            };

            var result = await _emailService.SendAsync(message);
            if (!result.Success)
            {
                _logger.LogWarning("Email send failed for invoice {InvoiceId}: {Error}", invoiceId, result.Error);
                return Result.Failure(result.Error ?? "Error al enviar el correo");
            }

            _logger.LogInformation("Invoice {InvoiceId} email sent successfully to {Email}", invoiceId, email);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending invoice {InvoiceId} by email to {Email}", invoiceId, email);
            return Result.Failure("Error al enviar el correo de la factura");
        }
    }

    public async Task<Result> CancelAsync(long invoiceId, string cancellationReason, string? replacementUuid = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var invoice = await context.MexicoInvoices.FindAsync(invoiceId);
            if (invoice == null)
                return Result.Failure("Factura no encontrada");

            if (!invoice.IsStamped || string.IsNullOrEmpty(invoice.Uuid))
                return Result.Failure("Solo se pueden cancelar facturas timbradas");

            if (invoice.Status == "Cancelled")
                return Result.Failure("La factura ya fue cancelada");

            var validReasons = new[] { "01", "02", "03", "04" };
            if (!validReasons.Contains(cancellationReason))
                return Result.Failure("Motivo de cancelación inválido. Use 01, 02, 03 o 04");

            if (cancellationReason == "01" && string.IsNullOrEmpty(replacementUuid))
                return Result.Failure("El motivo 01 requiere el UUID de la factura sustituta");

            // Mark as pending before calling PAC
            invoice.CancellationStatus = "Pending";
            invoice.CancellationReason = cancellationReason;
            invoice.ReplacementUuid = replacementUuid;
            invoice.CancellationDate = _dateTime.Now;
            invoice.Status = "CancellationPending";
            invoice.ModifiedAt = _dateTime.Now;
            await context.SaveChangesAsync();

            _logger.LogInformation("Sending cancellation request to PAC for invoice {InvoiceId} UUID {Uuid}",
                invoiceId, invoice.Uuid);

            // Call PAC
            var cancelResult = await _pacService.CancelCfdiAsync(
                invoice.Uuid!,
                invoice.IssuerRfc,
                invoice.CustomerRfc,
                invoice.Total,
                cancellationReason,
                replacementUuid);

            if (!cancelResult.IsSuccess)
            {
                // Revert status on PAC failure
                invoice.CancellationStatus = null;
                invoice.CancellationReason = null;
                invoice.ReplacementUuid = null;
                invoice.CancellationDate = null;
                invoice.Status = "Stamped";
                invoice.ModifiedAt = _dateTime.Now;
                await context.SaveChangesAsync();
                return Result.Failure(cancelResult.Error!);
            }

            var data = cancelResult.Value!;

            // Extract UUID status code from PAC response
            var uuidStatusCode = string.Empty;
            if (data.Uuid != null && data.Uuid.TryGetValue(invoice.Uuid!, out var statusCode))
                uuidStatusCode = statusCode;

            // Update invoice with acuse and PAC response data
            invoice.CancellationAcuse = data.Acuse;
            invoice.CancellationStatusSat = data.StatusSat;
            invoice.CancellationIsCancelable = data.IsCancelable;
            invoice.CancellationUuidStatusCode = uuidStatusCode;
            invoice.ModifiedAt = _dateTime.Now;

            // If SAT confirms immediate cancellation (code 201 or 202)
            if (uuidStatusCode is "201" or "202" || data.StatusSat == "Cancelado")
            {
                invoice.Status = "Cancelled";
                invoice.CancellationStatus = "Accepted";
                _logger.LogInformation("Invoice {InvoiceId} cancelled immediately by SAT (code {Code})",
                    invoiceId, uuidStatusCode);
            }
            // Code 204 = pending receiver acceptance
            else if (uuidStatusCode == "204")
            {
                invoice.Status = "CancellationPending";
                invoice.CancellationStatus = "Pending";
                _logger.LogInformation("Invoice {InvoiceId} cancellation pending receiver acceptance", invoiceId);
            }
            else
            {
                _logger.LogInformation("Invoice {InvoiceId} cancellation sent, status code: {Code}", invoiceId, uuidStatusCode);
            }

            await context.SaveChangesAsync();
            _logger.LogInformation("Cancellation processed for invoice {InvoiceId}", invoiceId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling invoice {InvoiceId}", invoiceId);
            return Result.Failure($"Error al cancelar: {ex.Message}");
        }
    }

    public async Task<Result> RefreshCancellationStatusAsync(long invoiceId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var invoice = await context.MexicoInvoices.FindAsync(invoiceId);
            if (invoice == null)
                return Result.Failure("Factura no encontrada");

            if (invoice.CancellationStatus != "Pending" || string.IsNullOrEmpty(invoice.Uuid))
                return Result.Failure("La factura no está en estado de cancelación pendiente");

            if (string.IsNullOrEmpty(invoice.CancellationReason))
            {
                invoice.CancellationStatus = "Error";
                invoice.ModifiedAt = _dateTime.Now;
                invoice.ModifiedBy = _currentUserService.UserId;
                await context.SaveChangesAsync();
                return Result.Failure("La factura no tiene motivo de cancelación registrado");
            }

            _logger.LogInformation(
                "Refreshing cancellation status for invoice {InvoiceId} UUID {Uuid}",
                invoiceId, invoice.Uuid);

            var checkResult = await _pacService.CheckCancellationStatusAsync(
                invoice.Uuid,
                invoice.IssuerRfc,
                invoice.CustomerRfc,
                invoice.Total,
                invoice.CancellationReason,
                invoice.ReplacementUuid);

            if (!checkResult.IsSuccess)
            {
                _logger.LogWarning(
                    "PAC returned error when checking cancellation status for invoice {InvoiceId}: {Error}",
                    invoiceId, checkResult.Error);
                return Result.Failure(checkResult.Error!);
            }

            var data = checkResult.Value!;

            var uuidStatusCode = string.Empty;
            if (data.Uuid != null && data.Uuid.TryGetValue(invoice.Uuid, out var code))
                uuidStatusCode = code;

            invoice.CancellationAcuse = data.Acuse ?? invoice.CancellationAcuse;
            invoice.CancellationStatusSat = data.StatusSat ?? invoice.CancellationStatusSat;
            invoice.CancellationIsCancelable = data.IsCancelable ?? invoice.CancellationIsCancelable;
            if (!string.IsNullOrEmpty(uuidStatusCode))
                invoice.CancellationUuidStatusCode = uuidStatusCode;
            invoice.ModifiedAt = _dateTime.Now;

            if (uuidStatusCode is "201" or "202" || data.StatusSat == "Cancelado")
            {
                invoice.Status = "Cancelled";
                invoice.CancellationStatus = "Accepted";
                _logger.LogInformation(
                    "Invoice {InvoiceId} cancellation accepted by SAT (code {Code})",
                    invoiceId, uuidStatusCode);
            }
            else if (data.StatusCancelation == "Rechazado" || uuidStatusCode == "205")
            {
                invoice.Status = "Stamped";
                invoice.CancellationStatus = "Rejected";
                _logger.LogInformation(
                    "Invoice {InvoiceId} cancellation rejected (code {Code}, statusCancelation {SC})",
                    invoiceId, uuidStatusCode, data.StatusCancelation);
            }
            else
            {
                _logger.LogDebug(
                    "Invoice {InvoiceId} cancellation still pending (code {Code})",
                    invoiceId, uuidStatusCode);
            }

            await context.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing cancellation status for invoice {InvoiceId}", invoiceId);
            return Result.Failure($"Error al consultar estado de cancelación: {ex.Message}");
        }
    }

    public async Task<Result<byte[]>> GetCancellationAcuseAsync(long invoiceId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var invoice = await context.MexicoInvoices.FindAsync(invoiceId);
            if (invoice == null)
                return Result<byte[]>.Failure("Factura no encontrada");

            if (string.IsNullOrEmpty(invoice.CancellationAcuse))
                return Result<byte[]>.Failure("No hay acuse de cancelación disponible para esta factura");

            return Result<byte[]>.Success(Encoding.UTF8.GetBytes(invoice.CancellationAcuse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cancellation acuse for invoice {InvoiceId}", invoiceId);
            return Result<byte[]>.Failure("Error al obtener el acuse de cancelación");
        }
    }

    public async Task<long> GetNextFolioAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var pacSettings = await context.MexicoPacSettings.AsNoTracking().FirstOrDefaultAsync();
        var startFolio = pacSettings?.StartFolio ?? 1L;

        var maxFolio = await context.MexicoInvoices
            .AsNoTracking()
            .MaxAsync(i => (long?)i.Folio);

        return maxFolio.HasValue
            ? Math.Max(startFolio, maxFolio.Value + 1)
            : startFolio;
    }

    public async Task<Result> ValidateSaleForInvoicingAsync(long saleId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var sale = await context.Sales
            .AsNoTracking()
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.Id == saleId);

        if (sale == null)
            return Result.Failure("Venta no encontrada");

        if (sale.Status == SaleStatus.Cancelled)
            return Result.Failure("No se puede facturar una venta cancelada");

        var alreadyInvoiced = await context.MexicoInvoices
            .AsNoTracking()
            .AnyAsync(i => i.SaleId == saleId && i.Status != "StampError");

        if (alreadyInvoiced)
            return Result.Failure("Esta venta ya tiene una factura generada");

        return Result.Success();
    }

    public async Task<Result<SaleForInvoicingDto>> GetSaleInfoForInvoicingAsync(long saleId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var sale = await context.Sales
                .AsNoTracking()
                .Include(s => s.Customer)
                .Include(s => s.Payments)
                    .ThenInclude(p => p.PaymentMethod)
                .FirstOrDefaultAsync(s => s.Id == saleId);

            if (sale == null)
                return Result<SaleForInvoicingDto>.Failure("Venta no encontrada");

            if (sale.Status == SaleStatus.Cancelled)
                return Result<SaleForInvoicingDto>.Failure("No se puede facturar una venta cancelada");

            var alreadyInvoiced = await context.MexicoInvoices
                .AsNoTracking()
                .AnyAsync(i => i.SaleId == saleId && i.Status != "StampError");

            if (alreadyInvoiced)
                return Result<SaleForInvoicingDto>.Failure("Esta venta ya tiene una factura generada");

            // Resolve CFDI FormaPago from the sale's actual payment methods
            var pacSettings = await context.MexicoPacSettings.AsNoTracking().FirstOrDefaultAsync();
            var policy = pacSettings?.MultiPaymentFormPolicy ?? MultiPaymentFormPolicy.UseHighestAmount;

            var dto = new SaleForInvoicingDto
            {
                SaleId = sale.Id,
                Total = sale.Total,
                SaleDate = sale.CreatedAt,
                CustomerName = sale.Customer?.Name ?? string.Empty,
                CustomerEmail = sale.Customer?.Email,
                CustomerSendInvoiceEmail = sale.Customer?.SendInvoiceEmail ?? false,
                CustomerRfc = sale.Customer?.TaxId,
                CustomerLegalName = sale.Customer?.LegalName,
                CustomerPostalCode = sale.Customer?.PostalCode,
                CustomerFiscalRegime = sale.Customer?.FiscalRegime,
                ResolvedPaymentForm = ResolvePaymentFormFromSale(sale, policy)
            };

            return Result<SaleForInvoicingDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sale info for invoicing {SaleId}", saleId);
            return Result<SaleForInvoicingDto>.Failure("Error al obtener la información de la venta");
        }
    }

    #region Private helpers

    /// <summary>
    /// Resolves the CFDI FormaPago code from a sale's payment records.
    /// Single method: returns its MxCfdiFormCode.
    /// Multiple methods: applies the configured policy (highest amount or "99").
    /// On amount tie: the method with lowest SortOrder wins.
    /// </summary>
    private static string ResolvePaymentFormFromSale(Sale sale, MultiPaymentFormPolicy policy)
    {
        var distinctMethods = sale.Payments
            .GroupBy(p => p.PaymentMethodId)
            .Select(g => new
            {
                g.First().PaymentMethod,
                TotalAmount = g.Sum(p => p.Amount)
            })
            .ToList();

        if (distinctMethods.Count <= 1)
            return distinctMethods.FirstOrDefault()?.PaymentMethod.MxCfdiFormCode ?? "01";

        if (policy == MultiPaymentFormPolicy.UseUndefined99)
            return "99";

        var winner = distinctMethods
            .OrderByDescending(m => m.TotalAmount)
            .ThenBy(m => m.PaymentMethod.SortOrder)
            .First();

        return winner.PaymentMethod.MxCfdiFormCode!;
    }

    private static async Task<(string formDesc, string methodDesc)> GetPaymentDescriptionsAsync(
        ApplicationDbContext context, string paymentFormCode, string paymentMethodCode)
    {
        var formDesc = await context.Set<MexicoPaymentForm>()
            .AsNoTracking()
            .Where(f => f.Code == paymentFormCode)
            .Select(f => f.Description)
            .FirstOrDefaultAsync() ?? string.Empty;

        var methodDesc = paymentMethodCode switch
        {
            "PUE" => "Pago en una sola exhibición",
            "PPD" => "Pago en parcialidades o diferido",
            _ => string.Empty
        };

        return (formDesc, methodDesc);
    }

    private static string FormatFolio(long folio, int folioLength) =>
        folioLength > 0 ? folio.ToString().PadLeft(folioLength, '0') : folio.ToString();

    private Comprobante BuildComprobante(
        MexicoInvoice invoice, Sale sale, string serie, long folio, int folioLength, CreateMexicoInvoiceDto dto,
        DateTime issueDate)
    {
        // Build conceptos first so we can derive SubTotal and Descuento from them.
        // Line-level amounts use 6-decimal precision; document-level totals round to 2.
        var conceptos = BuildConceptos(sale);

        // Check if a rounding adjustment is needed to match the sale total.
        // This bridges precision differences (6-dec POS vs CFDI rounding) and POS rounding.
        var impuestos = BuildImpuestosFromConceptos(conceptos);
        var cfdiSubTotal = Math.Round(conceptos.Sum(c => c.Importe), 2);
        var cfdiDescuento = Math.Round(conceptos.Sum(c => c.Descuento), 2);
        var cfdiTotalImpuestos = impuestos?.TotalImpuestosTrasladados ?? 0m;
        var cfdiTotal = cfdiSubTotal - cfdiDescuento + cfdiTotalImpuestos;

        var adjustment = sale.Total - cfdiTotal;
        if (adjustment > 0)
        {
            // Add a non-taxable "Redondeo" concepto to bridge the gap.
            // ObjetoImp="01" means no tax — avoids cascading tax base changes.
            conceptos.Add(new Concepto
            {
                ClaveProdServ = RoundingProductServiceCode,
                Cantidad = 1,
                ClaveUnidad = RoundingUnitCode,
                Descripcion = RoundingDescription,
                ValorUnitario = adjustment,
                Importe = adjustment,
                ObjetoImp = "01"
            });

            // Recalculate document totals with the rounding concepto
            cfdiSubTotal = Math.Round(conceptos.Sum(c => c.Importe), 2);
            cfdiDescuento = Math.Round(conceptos.Sum(c => c.Descuento), 2);
            cfdiTotal = cfdiSubTotal - cfdiDescuento + cfdiTotalImpuestos;
        }
        else if (adjustment < 0)
        {
            _logger.LogWarning(
                "Sale {SaleId} has negative rounding adjustment ({Adjustment}). " +
                "CFDI total {CfdiTotal} differs from sale total {SaleTotal}.",
                sale.Id, adjustment, cfdiTotal, sale.Total);
        }

        var comprobante = new Comprobante
        {
            Serie = serie,
            Folio = FormatFolio(folio, folioLength),
            Fecha = issueDate.ToString("yyyy-MM-ddTHH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture),
            Sello = "",
            FormaPago = dto.PaymentForm,
            NoCertificado = "",
            Certificado = "",
            SubTotal = cfdiSubTotal,
            Descuento = cfdiDescuento,
            Total = cfdiTotal,
            TipoDeComprobante = "I",
            Exportacion = "01",
            MetodoPago = dto.PaymentMethod,
            LugarExpedicion = invoice.IssuerPostalCode,
            Emisor = new Emisor
            {
                Rfc = invoice.IssuerRfc,
                Nombre = invoice.IssuerLegalName,
                RegimenFiscal = invoice.IssuerFiscalRegime
            },
            Receptor = new Receptor
            {
                Rfc = dto.CustomerRfc,
                Nombre = dto.CustomerLegalName,
                DomicilioFiscalReceptor = dto.CustomerPostalCode,
                RegimenFiscalReceptor = dto.CustomerFiscalRegime,
                UsoCFDI = dto.CfdiUse
            },
            Conceptos = conceptos,
            Impuestos = impuestos
        };

        return comprobante;
    }

    private List<Concepto> BuildConceptos(Sale sale)
    {
        return sale.Details.Select(detail =>
        {
            var product = detail.Product;
            var satCode = product.MexicoProductService?.Code ?? DefaultProductServiceCode;
            var unitCode = product.UnitMeasure?.MexicoSatUnit?.Code ?? DefaultUnitCode;

            // Line-level amounts use 6-decimal precision (SAT Anexo 20 allows up to 6
            // for Concepto). This matches PricingCalculationService.CalculateLine so that
            // POS totals and CFDI totals converge at the document level.
            var grossAmount = Math.Round(detail.Quantity * detail.UnitPrice, 6);
            var roundedDiscount = Math.Round(detail.DiscountAmount, 6);

            var concepto = new Concepto
            {
                ClaveProdServ = satCode,
                NoIdentificacion = product.Code,
                Cantidad = detail.Quantity,
                ClaveUnidad = unitCode,
                Unidad = product.UnitMeasure?.Name,
                Descripcion = product.Name,
                ValorUnitario = detail.UnitPrice,
                Importe = grossAmount,
                Descuento = roundedDiscount,
                ObjetoImp = product.IsTaxable ? "02" : "01"
            };

            if (product.IsTaxable && detail.TaxRate > 0)
            {
                var taxBase = grossAmount - roundedDiscount;
                concepto.Impuestos = new ConceptoImpuestos
                {
                    Traslados = new List<ConceptoTraslado>
                    {
                        new ConceptoTraslado
                        {
                            Base = taxBase,
                            Impuesto = IvaCode,
                            TipoFactor = IvaFactorType,
                            TasaOCuota = detail.TaxRate,
                            Importe = Math.Round(taxBase * detail.TaxRate, 6)
                        }
                    }
                };
            }

            return concepto;
        }).ToList();
    }

    /// <summary>
    /// Builds Impuestos from the already-built Conceptos list.
    /// This ensures the global tax summary matches the per-concept values exactly,
    /// including any rounding concepto that may have been added.
    /// CFDI40215: Global Base = sum of per-concept bases (grouped by rate).
    /// CFDI40216: Global Importe = sum of per-concept importes (NOT recalculated from base sum).
    /// Document-level amounts (Traslado.Base, Traslado.Importe, TotalTraslados) use 2 decimals.
    /// </summary>
    private Impuestos? BuildImpuestosFromConceptos(List<Concepto> conceptos)
    {
        // Collect all per-concept tax lines
        var allTraslados = conceptos
            .Where(c => c.Impuestos?.Traslados != null)
            .SelectMany(c => c.Impuestos!.Traslados!)
            .ToList();

        if (!allTraslados.Any()) return null;

        // Group by tax rate — sum per-concept values, then round to 2 for document level
        var traslados = allTraslados
            .GroupBy(t => t.TasaOCuota)
            .Select(g => new Traslado
            {
                Base = Math.Round(g.Sum(t => t.Base), 2),
                Impuesto = IvaCode,
                TipoFactor = IvaFactorType,
                TasaOCuota = g.Key,
                Importe = Math.Round(g.Sum(t => t.Importe), 2)
            }).ToList();

        var totalImpuestos = traslados.Sum(t => t.Importe);
        if (totalImpuestos == 0) return null;

        return new Impuestos
        {
            TotalImpuestosTrasladados = totalImpuestos,
            Traslados = traslados
        };
    }

    private async Task<Result<MexicoInvoiceDto>> MarkStampError(
        ApplicationDbContext context, MexicoInvoice invoice, string error, string? xmlContent = null)
    {
        invoice.Status = "StampError";
        invoice.StampError = error;
        invoice.ModifiedBy = _currentUserService.UserId;
        invoice.ModifiedAt = _dateTime.Now;

        if (!string.IsNullOrEmpty(xmlContent))
        {
            context.MexicoInvoiceFiles.Add(new MexicoInvoiceFile
            {
                InvoiceId = invoice.Id,
                FileType = "XML",
                FileData = System.Text.Encoding.UTF8.GetBytes(xmlContent),
                CreatedBy = _currentUserService.UserId,
                CreatedAt = _dateTime.Now,
                ModifiedBy = _currentUserService.UserId,
                ModifiedAt = _dateTime.Now
            });
        }

        await context.SaveChangesAsync();
        _logger.LogError("Invoice stamp error for sale {SaleId}: {Error}", invoice.SaleId, error);
        // Return Success so the UI receives the invoice DTO and can show it in the list.
        // The caller checks Status == "StampError" to distinguish from a successful stamp.
        var dto = await BuildInvoiceDtoFromEntity(invoice);
        dto.HasXml = !string.IsNullOrEmpty(xmlContent);
        return Result<MexicoInvoiceDto>.Success(dto);
    }

    private Task<MexicoInvoiceDto> BuildInvoiceDtoFromEntity(MexicoInvoice i) =>
        Task.FromResult(new MexicoInvoiceDto
        {
            Id = i.Id,
            SaleId = i.SaleId,
            Serie = i.Serie,
            Folio = i.Folio,
            Uuid = i.Uuid,
            Status = i.Status,
            IsStamped = i.IsStamped,
            StampDate = i.StampDate,
            CustomerRfc = i.CustomerRfc,
            CustomerLegalName = i.CustomerLegalName,
            CustomerPostalCode = i.CustomerPostalCode,
            CustomerFiscalRegime = i.CustomerFiscalRegime,
            IssuerRfc = i.IssuerRfc,
            IssuerLegalName = i.IssuerLegalName,
            IssuerFiscalRegime = i.IssuerFiscalRegime,
            IssuerPostalCode = i.IssuerPostalCode,
            Subtotal = i.Subtotal,
            TaxAmount = i.TaxAmount,
            Total = i.Total,
            Currency = i.Currency,
            CfdiUse = i.CfdiUse,
            PaymentForm = i.PaymentForm,
            PaymentMethod = i.PaymentMethod,
            NoCertificadoSat = i.NoCertificadoSat,
            NoCertificadoCfdi = i.NoCertificadoCfdi,
            CancellationStatus = i.CancellationStatus,
            CancellationReason = i.CancellationReason,
            CancellationDate = i.CancellationDate,
            StampError = i.StampError,
            CreatedAt = i.CreatedAt,
            CreatedBy = i.CreatedBy,
            ModifiedAt = i.ModifiedAt,
            ModifiedBy = i.ModifiedBy
        });

    private async Task<byte[]?> RegenerateCancelledPdfAsync(
        MexicoInvoice invoice, ApplicationDbContext context)
    {
        try
        {
            var sale = await context.Sales
                .Include(s => s.Details)
                    .ThenInclude(d => d.Product)
                        .ThenInclude(p => p.MexicoProductService)
                .FirstOrDefaultAsync(s => s.Id == invoice.SaleId);

            if (sale == null)
            {
                _logger.LogWarning(
                    "Sale {SaleId} not found while regenerating cancelled PDF for invoice {InvoiceId}",
                    invoice.SaleId, invoice.Id);
                return null;
            }

            var folioDisplay = string.IsNullOrEmpty(invoice.Serie)
                ? invoice.Folio.ToString()
                : $"{invoice.Serie}{invoice.Folio}";

            var pdfItems = sale.Details.Select(d => (object)new Dictionary<string, object>
            {
                { "sat_code", (object)(d.Product.MexicoProductService?.Code ?? DefaultProductServiceCode) },
                { "description", d.Product.Name },
                { "quantity", d.Quantity % 1 == 0 ? ((int)d.Quantity).ToString() : d.Quantity.ToString("G29") },
                { "unit_price", d.UnitPrice.ToString("N2") },
                { "discount", d.DiscountAmount > 0 ? d.DiscountAmount.ToString("N2") : string.Empty },
                { "has_discount", (object)(d.DiscountAmount > 0) },
                { "amount", d.Total.ToString("N2") }
            }).ToList();

            var logoBase64 = await _emailTemplateService.GetStaticFileBase64Async("images/logo.webp");
            var discountTotal = sale.Details.Sum(d => d.DiscountAmount);
            var cancellationDate = invoice.CancellationDate.HasValue
                ? invoice.CancellationDate.Value.ToString("dd/MM/yyyy")
                : string.Empty;

            var (formDesc, methodDesc) = await GetPaymentDescriptionsAsync(context, invoice.PaymentForm, invoice.PaymentMethod);
            var pdfData = BuildInvoiceTemplateData(
                invoice, folioDisplay, pdfItems, hasPdf: true,
                discountTotal: discountTotal, logoBase64: logoBase64,
                serie: invoice.Serie ?? string.Empty,
                isCancelled: true,
                cancellationDate: cancellationDate,
                paymentFormDescription: formDesc, paymentMethodDescription: methodDesc);

            var html = await _emailTemplateService.GetTemplateAsync("invoice-cfdi", pdfData);
            html = InjectCancellationWatermark(html, cancellationDate);
            return await _pdfService.GeneratePdfFromHtmlAsync(html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to regenerate cancelled PDF for invoice {InvoiceId}", invoice.Id);
            return null;
        }
    }

    private static string InjectCancellationWatermark(string html, string cancellationDate)
    {
        // Skip if the template already rendered the watermark
        if (html.Contains("watermark-overlay", StringComparison.OrdinalIgnoreCase) ||
            html.Contains("cfdi-cancel-overlay", StringComparison.OrdinalIgnoreCase))
            return html;

        const string css = """
            .cfdi-cancel-overlay {
                position: fixed;
                top: 0; left: 0;
                width: 100%; height: 100%;
                pointer-events: none;
                z-index: 9999;
                display: flex;
                align-items: center;
                justify-content: center;
            }
            .cfdi-cancel-text {
                font-size: 100px;
                font-weight: 900;
                color: rgba(192, 57, 43, 0.20);
                text-transform: uppercase;
                letter-spacing: 8px;
                transform: rotate(-40deg);
                white-space: nowrap;
                font-family: Arial Black, Arial, sans-serif;
                user-select: none;
                text-align: center;
                line-height: 1.2;
            }
            .cfdi-cancel-banner {
                margin: 0 12px 0 12px;
                padding: 8px 14px;
                background-color: #fdecea;
                border-left: 4px solid #c0392b;
                font-size: 12px;
                color: #c0392b;
                font-weight: bold;
            }
            """;

        var dateSpan = string.IsNullOrEmpty(cancellationDate)
            ? string.Empty
            : $"<br><span style=\"font-size:28px;letter-spacing:4px;\">{cancellationDate}</span>";

        var overlay = $"""
            <div class="cfdi-cancel-overlay">
                <div class="cfdi-cancel-text">CANCELADA{dateSpan}</div>
            </div>
            """;

        var banner = $"""
            <div class="cfdi-cancel-banner">
                &#x26A0; FACTURA CANCELADA ante el SAT{(string.IsNullOrEmpty(cancellationDate) ? "" : $" — Fecha: {cancellationDate}")}
            </div>
            """;

        // Inject CSS before </style> (or before </head> if no style block)
        if (html.Contains("</style>", StringComparison.OrdinalIgnoreCase))
        {
            html = html.Replace("</style>", css + "\n</style>",
                StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            html = html.Replace("</head>",
                $"<style>\n{css}\n</style>\n</head>",
                StringComparison.OrdinalIgnoreCase);
        }

        // Inject watermark overlay right after <body> opening tag
        html = System.Text.RegularExpressions.Regex.Replace(
            html,
            @"<body([^>]*)>",
            m => m.Value + "\n" + overlay,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Inject cancellation banner as first child of .container div
        html = System.Text.RegularExpressions.Regex.Replace(
            html,
            @"(<div[^>]+class=""container""[^>]*>)",
            m => m.Value + "\n" + banner,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return html;
    }

    private Dictionary<string, object> BuildInvoiceTemplateData(
        MexicoInvoice invoice, string folioDisplay, List<object> items, bool hasPdf,
        decimal discountTotal = 0, string logoBase64 = "", string serie = "",
        bool isCancelled = false, string cancellationDate = "",
        string paymentFormDescription = "", string paymentMethodDescription = "")
    {
        var qrCode = string.Empty;
        if (!string.IsNullOrEmpty(invoice.Uuid) && !string.IsNullOrEmpty(invoice.SelloCfdi))
        {
            var fe = invoice.SelloCfdi.Length >= 8
                ? invoice.SelloCfdi[^8..]
                : invoice.SelloCfdi;
            var qrUrl = $"https://verificacfdi.facturaelectronica.sat.gob.mx/default.aspx" +
                        $"?id={invoice.Uuid}" +
                        $"&re={invoice.IssuerRfc}" +
                        $"&rr={invoice.CustomerRfc}" +
                        $"&tt={invoice.Total:F6}" +
                        $"&fe={fe}";
            qrCode = GenerateQrCodeBase64(qrUrl);
        }

        return new Dictionary<string, object>
        {
            { "culture", CultureInfo.CurrentUICulture.Name },
            { "app_name", _applicationOptions.Name },
            { "issuer_legal_name", invoice.IssuerLegalName },
            { "issuer_rfc", invoice.IssuerRfc },
            { "issuer_fiscal_regime", invoice.IssuerFiscalRegime },
            { "issuer_postal_code", invoice.IssuerPostalCode },
            { "serie", serie },
            { "folio", invoice.Folio.ToString() },
            { "folio_display", folioDisplay },
            { "issue_date", invoice.StampDate?.ToString("dd/MM/yyyy HH:mm") ?? string.Empty },
            { "uuid", invoice.Uuid ?? string.Empty },
            { "payment_form", invoice.PaymentForm },
            { "payment_form_description", paymentFormDescription },
            { "payment_method", invoice.PaymentMethod },
            { "payment_method_description", paymentMethodDescription },
            { "currency", invoice.Currency },
            { "customer_legal_name", invoice.CustomerLegalName },
            { "customer_rfc", invoice.CustomerRfc },
            { "customer_fiscal_regime", invoice.CustomerFiscalRegime },
            { "customer_postal_code", invoice.CustomerPostalCode },
            { "cfdi_use", invoice.CfdiUse },
            { "subtotal", invoice.Subtotal.ToString("N2") },
            { "tax_amount", invoice.TaxAmount.ToString("N2") },
            { "total", invoice.Total.ToString("N2") },
            { "no_cert_cfdi", invoice.NoCertificadoCfdi ?? string.Empty },
            { "no_cert_sat", invoice.NoCertificadoSat ?? string.Empty },
            { "stamp_date", invoice.StampDate?.ToString("dd/MM/yyyy HH:mm:ss") ?? string.Empty },
            { "cadena_original", invoice.CadenaOriginalSat ?? string.Empty },
            { "sello_cfdi", invoice.SelloCfdi ?? string.Empty },
            { "sello_sat", invoice.SelloSat ?? string.Empty },
            { "qr_code", qrCode },
            { "discount", discountTotal.ToString("N2") },
            { "items", (object)items },
            { "has_pdf", (object)hasPdf },
            { "date_year", (object)DateTime.UtcNow.Year },
            { "is_cancelled", (object)isCancelled },
            { "cancellation_date", cancellationDate },
            { "company_logo_url", string.IsNullOrEmpty(logoBase64)
                ? $"{_applicationOptions.BaseUrl.TrimEnd('/')}/images/logo.webp"
                : logoBase64 }
        };
    }

    private static string GenerateQrCodeBase64(string text)
    {
        try
        {
            using var qrGenerator = new QRCoder.QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(text, QRCoder.QRCodeGenerator.ECCLevel.M);
            var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
            var bytes = qrCode.GetGraphic(3);
            return "data:image/png;base64," + Convert.ToBase64String(bytes);
        }
        catch
        {
            return string.Empty;
        }
    }


    private Task<string> BuildInvoiceHtmlAsync(MexicoInvoiceDto dto, string stampedXml)
    {
        // Basic HTML representation (Representación Impresa CFDI)
        var html = $@"<!DOCTYPE html>
<html lang=""es"">
<head>
<meta charset=""UTF-8"" />
<title>Factura {dto.FolioDisplay}</title>
<style>
  body {{ font-family: Arial, sans-serif; font-size: 11px; margin: 20px; }}
  h1 {{ font-size: 14px; margin: 0; }}
  .header {{ display: flex; justify-content: space-between; margin-bottom: 12px; }}
  .section {{ margin-bottom: 10px; border: 1px solid #ccc; padding: 8px; }}
  .section h2 {{ font-size: 11px; margin: 0 0 4px 0; font-weight: bold; }}
  table {{ width: 100%; border-collapse: collapse; }}
  th, td {{ border: 1px solid #ccc; padding: 4px 6px; text-align: left; font-size: 10px; }}
  th {{ background: #f0f0f0; }}
  .amount {{ text-align: right; }}
  .totals td {{ font-weight: bold; }}
  .uuid {{ font-size: 9px; word-break: break-all; }}
  .footer {{ margin-top: 10px; font-size: 9px; color: #555; }}
</style>
</head>
<body>
<div class=""header"">
  <div>
    <h1>{dto.IssuerLegalName}</h1>
    <div>RFC: {dto.IssuerRfc} | Régimen: {dto.IssuerFiscalRegime}</div>
    <div>Lugar de expedición: {dto.IssuerPostalCode}</div>
  </div>
  <div style=""text-align:right"">
    <strong>CFDI de Ingreso</strong><br/>
    Folio: {dto.FolioDisplay}<br/>
    Fecha: {dto.StampDate?.ToString("dd/MM/yyyy HH:mm") ?? DateTime.Now.ToString("dd/MM/yyyy HH:mm")}<br/>
    UUID: <span class=""uuid"">{dto.Uuid}</span>
  </div>
</div>

<div class=""section"">
  <h2>RECEPTOR</h2>
  <div>RFC: {dto.CustomerRfc} | {dto.CustomerLegalName}</div>
  <div>Régimen: {dto.CustomerFiscalRegime} | CP: {dto.CustomerPostalCode}</div>
  <div>Uso CFDI: {dto.CfdiUse}</div>
</div>

<div class=""section"">
  <h2>CONCEPTOS</h2>
  <table>
    <tr><th>Clave SAT</th><th>Descripción</th><th>Cant</th><th>P.U.</th><th>Descuento</th><th>Importe</th></tr>
    <!-- Conceptos rendered from XML -->
  </table>
</div>

<div style=""text-align:right; margin-top:8px;"">
  <table style=""width:250px; margin-left:auto;"">
    <tr><td>Subtotal:</td><td class=""amount"">${dto.Subtotal:N2}</td></tr>
    <tr><td>IVA:</td><td class=""amount"">${dto.TaxAmount:N2}</td></tr>
    <tr class=""totals""><td>Total:</td><td class=""amount"">${dto.Total:N2} MXN</td></tr>
  </table>
</div>

<div class=""section"" style=""margin-top:10px;"">
  <h2>INFORMACIÓN DEL TIMBRE</h2>
  <div>UUID: <span class=""uuid"">{dto.Uuid}</span></div>
  <div>No. Certificado SAT: {dto.NoCertificadoSat}</div>
  <div>No. Certificado Emisor: {dto.NoCertificadoCfdi}</div>
  <div>Fecha de timbrado: {dto.StampDate?.ToString("yyyy-MM-dd HH:mm:ss")}</div>
</div>

<div class=""footer"">
  Este documento es una representación impresa de un CFDI.<br/>
  Verifique su autenticidad en: https://verificacfdi.facturaelectronica.sat.gob.mx
</div>
</body>
</html>";

        return Task.FromResult(html);
    }

    #endregion
}
