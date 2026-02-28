using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml.Xsl;
using App.Core.Common;
using App.Core.Interfaces.Billing;
using App.Core.Models.Cfdi.V40;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace App.Services.Billing;

/// <summary>
/// Generates and validates CFDI 4.0 XML for income (Ingreso) invoices.
/// XSLT and XSD files are embedded resources in App.Services.
/// </summary>
public class MexicoCfdiXmlService : IMexicoCfdiXmlService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MexicoCfdiXmlService> _logger;
    private static readonly XmlSerializer _serializer = new(typeof(Comprobante));
    private const string XsltCacheKey = "CfdiV40XslTransform";
    private const string XsdCacheKey = "CfdiV40XsdSchemas";
    private static readonly Assembly _assembly = typeof(MexicoCfdiXmlService).Assembly;

    public MexicoCfdiXmlService(IMemoryCache cache, ILogger<MexicoCfdiXmlService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<string>> GenerateXmlAsync(Comprobante comprobante)
    {
        try
        {
            _logger.LogInformation("Generating CFDI 4.0 XML, folio {Folio}", comprobante.Folio);
            var xml = SerializeToXml(comprobante);
            return await Task.FromResult(Result<string>.Success(xml));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating CFDI XML");
            return Result<string>.Failure($"Error al generar el XML CFDI: {ex.Message}");
        }
    }

    public async Task<Result<string>> GenerateOriginalChainAsync(string cfdiXml)
    {
        try
        {
            var xslTransform = await _cache.GetOrCreateAsync(XsltCacheKey, async entry =>
            {
                entry.Priority = CacheItemPriority.NeverRemove;
                var xsltContent = LoadEmbeddedResource("App.Services.Resources.Xslt.cadenaoriginal_4_0.xslt");
                if (string.IsNullOrEmpty(xsltContent))
                    throw new InvalidOperationException("Recurso XSLT no encontrado en el ensamblado");

                var transform = new XslCompiledTransform();
                var settings = new XsltSettings { EnableDocumentFunction = true, EnableScript = false };
                using var reader = XmlReader.Create(new StringReader(xsltContent));
                transform.Load(reader, settings, new EmbeddedXsltResolver(_logger));
                _logger.LogInformation("XSLT compilado y cacheado correctamente");
                return await Task.FromResult(transform);
            });

            if (xslTransform == null)
                return Result<string>.Failure("No se pudo cargar la transformación XSLT");

            using var xmlReader = XmlReader.Create(new StringReader(cfdiXml));
            using var output = new StringWriter();
            xslTransform.Transform(xmlReader, null, output);

            var chain = output.ToString();
            if (string.IsNullOrWhiteSpace(chain))
                return Result<string>.Failure("La cadena original generada está vacía");

            return Result<string>.Success(chain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating original chain");
            return Result<string>.Failure($"Error al generar la cadena original: {ex.Message}");
        }
    }

    public async Task<Result<List<string>>> ValidateXmlAsync(string cfdiXml)
    {
        try
        {
            var errors = new List<string>();

            var schemaSet = await _cache.GetOrCreateAsync(XsdCacheKey, async entry =>
            {
                entry.Priority = CacheItemPriority.NeverRemove;
                var schemas = new XmlSchemaSet { XmlResolver = null! };

                var xsdResources = new[]
                {
                    ("App.Services.Resources.Xsd.tdCFDI.xsd",  "http://www.sat.gob.mx/sitio_internet/cfd/tipoDatos/tdCFDI"),
                    ("App.Services.Resources.Xsd.catCFDI.xsd", "http://www.sat.gob.mx/sitio_internet/cfd/catalogos"),
                    ("App.Services.Resources.Xsd.cfdv40.xsd",  "http://www.sat.gob.mx/cfd/4"),
                };

                foreach (var (name, ns) in xsdResources)
                {
                    var content = LoadEmbeddedResource(name);
                    if (string.IsNullOrEmpty(content))
                        throw new InvalidOperationException($"XSD no encontrado: {name}");
                    using var reader = XmlReader.Create(new StringReader(content));
                    schemas.Add(ns, reader);
                }

                schemas.Compile();
                _logger.LogInformation("XSD schemas compilados y cacheados");
                return await Task.FromResult(schemas);
            });

            if (schemaSet == null)
                return Result<List<string>>.Failure("No se pudieron cargar los esquemas XSD");

            var readerSettings = new XmlReaderSettings
            {
                Async = true,
                ValidationType = ValidationType.Schema,
                Schemas = schemaSet,
                ValidationFlags = XmlSchemaValidationFlags.ProcessInlineSchema |
                                  XmlSchemaValidationFlags.ReportValidationWarnings
            };
            readerSettings.ValidationEventHandler += (_, args) => errors.Add(
                $"[{args.Severity}] Línea {args.Exception?.LineNumber}: {args.Message}");

            using var sr = new StringReader(cfdiXml);
            using var xmlReader = XmlReader.Create(sr, readerSettings);
            while (await xmlReader.ReadAsync()) { }

            return Result<List<string>>.Success(errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating XML");
            return Result<List<string>>.Failure($"Error al validar el XML: {ex.Message}");
        }
    }

    #region Private helpers

    private string SerializeToXml(Comprobante comprobante)
    {
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
            IndentChars = "  ",
            OmitXmlDeclaration = false
        };

        using var sw = new Utf8StringWriter();
        using var xw = XmlWriter.Create(sw, settings);

        var ns = new XmlSerializerNamespaces();
        ns.Add("cfdi", "http://www.sat.gob.mx/cfd/4");
        ns.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");

        _serializer.Serialize(xw, comprobante, ns);
        return sw.ToString();
    }

    private string? LoadEmbeddedResource(string resourceName)
    {
        using var stream = _assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            _logger.LogError("Recurso embebido no encontrado: {Name}", resourceName);
            return null;
        }
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }

    private sealed class EmbeddedXsltResolver : XmlUrlResolver
    {
        private readonly ILogger _logger;

        public EmbeddedXsltResolver(ILogger logger)
        {
            _logger = logger;
        }

        public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
        {
            try
            {
                var uriString = absoluteUri.ToString();

                int satIndex = uriString.LastIndexOf("/SAT/", StringComparison.OrdinalIgnoreCase);
                if (satIndex < 0)
                    satIndex = uriString.LastIndexOf("\\SAT\\", StringComparison.OrdinalIgnoreCase);

                if (satIndex < 0)
                    return base.GetEntity(absoluteUri, role, ofObjectToReturn);

                var relativePath = uriString.Substring(satIndex + 1)
                    .Replace("/", ".")
                    .Replace("\\", ".");

                var resourceName = $"App.Services.Resources.Xslt.{relativePath}";
                _logger.LogDebug("Resolving XSLT include: {Resource}", resourceName);

                var stream = _assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                    return stream;

                _logger.LogWarning("XSLT embedded resource not found: {Resource}", resourceName);
                return base.GetEntity(absoluteUri, role, ofObjectToReturn);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving XSLT resource: {Uri}", absoluteUri);
                return base.GetEntity(absoluteUri, role, ofObjectToReturn);
            }
        }
    }

    #endregion
}
