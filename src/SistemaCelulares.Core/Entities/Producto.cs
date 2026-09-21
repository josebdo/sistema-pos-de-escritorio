namespace SistemaCelulares.Core.Entities;

public class Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public int CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;

    /// <summary>
    /// Código SKU único e indexado para control interno de inventario.
    /// </summary>
    public string Sku { get; set; } = string.Empty;

    public decimal PrecioCosto { get; set; }
    public decimal PrecioVenta { get; set; }

    public int StockActual { get; set; }
    
    /// <summary>
    /// Umbral mínimo de stock para disparar alertas de reabastecimiento.
    /// </summary>
    public int CantidadMinima { get; set; } = 3;

    /// <summary>
    /// Código de barras EAN-13 o similar (indexado para escaneo rápido en caja).
    /// </summary>
    public string? CodigoBarras { get; set; }

    /// <summary>
    /// Eliminación lógica: no se borra físicamente para preservar trazabilidad de ventas y compras.
    /// </summary>
    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? UltimaModificacion { get; set; }
}
