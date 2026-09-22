namespace SistemaCelulares.Core.Entities;

/// <summary>
/// Estado del ciclo de vida de una unidad física (ej. celular rastreado por IMEI).
/// </summary>
public enum EstadoUnidadProducto
{
    /// <summary>
    /// Unidad disponible en tienda lista para ser vendida.
    /// </summary>
    EnStock = 1,

    /// <summary>
    /// Unidad vendida a un cliente con fecha y venta asociada.
    /// </summary>
    Vendido = 2,

    /// <summary>
    /// Unidad ingresada por proceso de garantía o servicio técnico.
    /// </summary>
    EnGarantia = 3,

    /// <summary>
    /// Unidad devuelta a proveedor o por el cliente.
    /// </summary>
    Devuelto = 4
}
