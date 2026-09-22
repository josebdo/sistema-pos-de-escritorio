namespace SistemaCelulares.Core.Entities;

public class DetalleVenta
{
    public int Id { get; set; }

    public int VentaId { get; set; }
    public virtual Venta? Venta { get; set; }

    public int ProductoId { get; set; }
    public virtual Producto? Producto { get; set; }

    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Itbis { get; set; }
    public decimal Subtotal { get; set; }

    /// <summary>
    /// En caso de celular con serie, referencia a la unidad física vendida.
    /// </summary>
    public int? UnidadProductoId { get; set; }
    public virtual UnidadProducto? UnidadProducto { get; set; }

    /// <summary>
    /// IMEI registrado al momento de la venta para trazabilidad y garantía en ticket.
    /// </summary>
    public string? Imei { get; set; }
}
