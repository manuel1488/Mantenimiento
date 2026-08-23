namespace App.Core.Options;

/// <summary>
/// Límite de cantidad de fotos por Cotización. El tamaño máximo por archivo, tipos permitidos,
/// calidad de compresión y dimensiones de miniatura ya están cubiertos por <see cref="ImageOptions"/>
/// (sección "Images"), compartida por todos los módulos que suben imágenes.
/// </summary>
public class CotizacionFotoOptions
{
    public const string SectionName = "CotizacionFotos";

    /// <summary>Cantidad máxima de fotos permitidas por Cotización.</summary>
    public int MaxFotos { get; set; } = 10;
}
