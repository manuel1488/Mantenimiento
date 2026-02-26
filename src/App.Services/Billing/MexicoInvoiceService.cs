using App.Core.Common;
using App.Core.DTOs.Billing.Mexico;
using App.Core.Enums.Shop;
using App.Core.Interfaces;
using App.Core.Interfaces.Billing;
using App.Core.Models.Cfdi.V40;
using App.Models.Billing;
using App.Models.Data.Contexts;
using App.Models.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using App.Core.DTOs.Settings;

namespace App.Services.Billing;

public class MexicoInvoiceService : IMexicoInvoiceService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMexicoCfdiXmlService _xmlService;
    private readonly IMexicoCsdSigningService _signingService;
    private readonly ISwSapienService _pacService;
    private readonly IMexicoPacSettingsService _pacSettingsService;
    private readonly ITaxSettingsService _taxSettingsService;
    private readonly IPdfService _pdfService;
    private readonly ILogger<MexicoInvoiceService> _logger;

    private const string MexicoTimezone = "America/Mexico_City";
    private const string DefaultProductServiceCode = "01010101"; // No identificado
    private const string DefaultUnitCode = "H87"; // Pieza (SAT standard)
    private const string IvaCode = "002";
    private const string IvaFactorType = "Tasa";

    public MexicoInvoiceService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMexicoCfdiXmlService xmlService,
        IMexicoCsdSigningService signingService,
        ISwSapienService pacService,
        IMexicoPacSettingsService pacSettingsService,
        ITaxSettingsService taxSettingsService,
        IPdfService pdfService,
        ILogger<MexicoInvoiceService> logger)
    {
        _contextFactory = contextFactory;
        _xmlService = xmlService;
        _signingService = signingService;
        _pacService = pacService;
        _pacSettingsService = pacSettingsService;
        _taxSettingsService = taxSettingsService;
        _pdfService = pdfService;
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
                .FirstAsync(s => s.Id == dto.SaleId);

            // 4. Get next folio
            var folio = await GetNextFolioAsync();
            var serie = pacSettings.InvoiceSerie ?? "A";

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
                CreatedBy = "System",
                CreatedAt = DateTime.UtcNow,
                ModifiedBy = "System",
                ModifiedAt = DateTime.UtcNow
            };

            context.MexicoInvoices.Add(invoice);
            await context.SaveChangesAsync();

            try
            {
                // 6. Build Comprobante
                var comprobante = BuildComprobante(invoice, sale, serie, folio, dto);

                // 7. Generate XML
                var xmlResult = await _xmlService.GenerateXmlAsync(comprobante);
                if (!xmlResult.IsSuccess)
                    return await MarkStampError(context, invoice, xmlResult.Error!);

                // 8. Load CSD and sign
                var certResult = await _pacSettingsService.GetCsdCertificateBytesAsync();
                var keyResult = await _pacSettingsService.GetCsdPrivateKeyBytesAsync();
                var pwdResult = await _pacSettingsService.GetCsdPasswordAsync();

                if (!certResult.IsSuccess) return await MarkStampError(context, invoice, certResult.Error!);
                if (!keyResult.IsSuccess) return await MarkStampError(context, invoice, keyResult.Error!);
                if (!pwdResult.IsSuccess) return await MarkStampError(context, invoice, pwdResult.Error!);

                var signedXmlResult = await _signingService.SignXmlAsync(
                    xmlResult.Value!, certResult.Value!, keyResult.Value!, pwdResult.Value!);

                if (!signedXmlResult.IsSuccess)
                    return await MarkStampError(context, invoice, signedXmlResult.Error!);

                // 9. Stamp with PAC
                var stampResult = await _pacService.StampAsync(signedXmlResult.Value!);
                if (!stampResult.IsSuccess)
                    return await MarkStampError(context, invoice, stampResult.Error!);

                var stamp = stampResult.Value!;

                // 10. Update invoice with stamp data
                invoice.Uuid = stamp.Uuid;
                invoice.StampDate = DateTime.UtcNow;
                invoice.IsStamped = true;
                invoice.Status = "Stamped";
                invoice.NoCertificadoSat = stamp.NoCertificadoSat;
                invoice.NoCertificadoCfdi = stamp.NoCertificadoCfdi;
                invoice.SelloSat = stamp.SelloSat;
                invoice.SelloCfdi = stamp.SelloCfdi;
                invoice.CadenaOriginalSat = stamp.CadenaOriginalSat;
                invoice.StampError = null;
                invoice.ModifiedAt = DateTime.UtcNow;

                // 11. Save stamped XML
                var stampedXml = stamp.Cfdi ?? signedXmlResult.Value!;
                context.MexicoInvoiceFiles.Add(new MexicoInvoiceFile
                {
                    InvoiceId = invoice.Id,
                    FileType = "XML",
                    FileData = System.Text.Encoding.UTF8.GetBytes(stampedXml),
                    CreatedBy = "System",
                    CreatedAt = DateTime.UtcNow,
                    ModifiedBy = "System",
                    ModifiedAt = DateTime.UtcNow
                });

                await context.SaveChangesAsync();

                // 12. Generate and save PDF (non-blocking — don't fail if PDF fails)
                try
                {
                    var pdfDto = await BuildInvoiceDtoFromEntity(invoice);
                    var html = await BuildInvoiceHtmlAsync(pdfDto, stampedXml);
                    var pdf = await _pdfService.GeneratePdfFromHtmlAsync(html);
                    context.MexicoInvoiceFiles.Add(new MexicoInvoiceFile
                    {
                        InvoiceId = invoice.Id,
                        FileType = "PDF",
                        FileData = pdf,
                        CreatedBy = "System",
                        CreatedAt = DateTime.UtcNow,
                        ModifiedBy = "System",
                        ModifiedAt = DateTime.UtcNow
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

        var query = context.MexicoInvoices.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var s = searchString.Trim();
            query = query.Where(i =>
                i.CustomerRfc.Contains(s) ||
                i.CustomerLegalName.Contains(s) ||
                (i.Uuid != null && i.Uuid.Contains(s)));
        }

        if (startDate.HasValue)
            query = query.Where(i => i.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(i => i.CreatedAt <= endDate.Value.AddDays(1));

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
                CreatedAt = i.CreatedAt,
                CreatedBy = i.CreatedBy,
                ModifiedAt = i.ModifiedAt,
                ModifiedBy = i.ModifiedBy
            })
            .ToListAsync();

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
        await using var context = await _contextFactory.CreateDbContextAsync();
        var file = await context.MexicoInvoiceFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.InvoiceId == invoiceId && f.FileType == "PDF");

        return file == null
            ? Result<byte[]>.Failure("No se encontró el PDF de la factura")
            : Result<byte[]>.Success(file.FileData);
    }

    public async Task<Result> SendByEmailAsync(long invoiceId, string email)
    {
        // Placeholder — email sending implemented in Phase 2
        _logger.LogInformation("Email send requested for invoice {InvoiceId} to {Email}", invoiceId, email);
        return await Task.FromResult(Result.Success());
    }

    public async Task<Result> CancelAsync(long invoiceId, string reason)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var invoice = await context.MexicoInvoices.FindAsync(invoiceId);
            if (invoice == null)
                return Result.Failure("Factura no encontrada");

            if (!invoice.IsStamped)
                return Result.Failure("Solo se pueden cancelar facturas timbradas");

            // Mark as cancellation pending (SAT cancellation flow is async)
            invoice.CancellationStatus = "Pending";
            invoice.CancellationReason = reason;
            invoice.CancellationDate = DateTime.UtcNow;
            invoice.Status = "CancellationPending";
            invoice.ModifiedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            _logger.LogInformation("Cancellation requested for invoice {InvoiceId}", invoiceId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling invoice {InvoiceId}", invoiceId);
            return Result.Failure($"Error al cancelar: {ex.Message}");
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

    #region Private helpers

    private Comprobante BuildComprobante(
        MexicoInvoice invoice, Sale sale, string serie, long folio, CreateMexicoInvoiceDto dto)
    {
        var issueDate = ToMexicoCityTime(DateTime.UtcNow);

        var comprobante = new Comprobante
        {
            Serie = serie,
            Folio = folio.ToString(),
            Fecha = issueDate.ToString("yyyy-MM-ddTHH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture),
            Sello = "",
            FormaPago = dto.PaymentForm,
            NoCertificado = "",
            Certificado = "",
            SubTotal = sale.Subtotal,
            Descuento = sale.DiscountAmount,
            Total = sale.Total,
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
            Conceptos = BuildConceptos(sale),
            Impuestos = BuildImpuestos(sale)
        };

        return comprobante;
    }

    private List<Concepto> BuildConceptos(Sale sale)
    {
        return sale.Details.Select(detail =>
        {
            var product = detail.Product;
            var satCode = product.MexicoProductService?.Code ?? DefaultProductServiceCode;
            var unitCode = product.UnitMeasure?.Code ?? DefaultUnitCode;

            var concepto = new Concepto
            {
                ClaveProdServ = satCode,
                NoIdentificacion = product.Code,
                Cantidad = detail.Quantity,
                ClaveUnidad = unitCode,
                Unidad = product.UnitMeasure?.Name,
                Descripcion = product.Name,
                ValorUnitario = detail.UnitPrice,
                Importe = detail.Subtotal,
                Descuento = detail.DiscountAmount,
                ObjetoImp = product.IsTaxable ? "02" : "01"
            };

            if (product.IsTaxable && detail.TaxRate > 0)
            {
                var taxBase = detail.Subtotal - detail.DiscountAmount;
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
                            Importe = detail.TaxAmount
                        }
                    }
                };
            }

            return concepto;
        }).ToList();
    }

    private Impuestos? BuildImpuestos(Sale sale)
    {
        if (sale.TaxAmount == 0) return null;

        // Group taxable details
        var taxableDetails = sale.Details
            .Where(d => d.Product.IsTaxable && d.TaxRate > 0)
            .ToList();

        if (!taxableDetails.Any()) return null;

        // Group by tax rate
        var traslados = taxableDetails
            .GroupBy(d => d.TaxRate)
            .Select(g => new Traslado
            {
                Base = g.Sum(d => d.Subtotal - d.DiscountAmount),
                Impuesto = IvaCode,
                TipoFactor = IvaFactorType,
                TasaOCuota = g.Key,
                Importe = g.Sum(d => d.TaxAmount)
            }).ToList();

        return new Impuestos
        {
            TotalImpuestosTrasladados = sale.TaxAmount,
            Traslados = traslados
        };
    }

    private static DateTime ToMexicoCityTime(DateTime utc)
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
            return TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
        }
        catch
        {
            // Windows fallback
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
            }
            catch
            {
                return utc.AddHours(-6);
            }
        }
    }

    private async Task<Result<MexicoInvoiceDto>> MarkStampError(
        ApplicationDbContext context, MexicoInvoice invoice, string error)
    {
        invoice.Status = "StampError";
        invoice.StampError = error;
        invoice.ModifiedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        _logger.LogError("Invoice stamp error for sale {SaleId}: {Error}", invoice.SaleId, error);
        return Result<MexicoInvoiceDto>.Failure(error);
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
