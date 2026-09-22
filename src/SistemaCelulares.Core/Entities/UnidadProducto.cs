namespace SistemaCelulares.Core.Entities;

/// <summary>
/// Representa una unidad física individual (ej. un celular específico identificado por IMEI).
/// </summary>
public class UnidadProducto
{
    public int Id { get; set; }

    public int ProductoId { get; set; }
    public virtual Producto Producto { get; set; } = null!;

    /// <summary>
    /// IMEI o Número de serie único del celular físico. Indexado y único.
    /// </summary>
    public string Imei { get; set; } = string.Empty;

    public EstadoUnidadProducto Estado { get; set; } = EstadoUnidadProducto.EnStock;

    public DateTime FechaIngreso { get; set; } = DateTime.UtcNow;

    public DateTime? FechaVenta { get; set; }

    public int? VentaId { get; set; }
    public virtual Venta? Venta { get; set; }

    public string? Notas { get; set; }
}
