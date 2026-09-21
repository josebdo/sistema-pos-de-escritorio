using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.Core.Interfaces;

public interface ICompraService
{
    Task<Compra> RegistrarCompraAsync(
        int proveedorId,
        int usuarioId,
        string? numeroFactura,
        string? observaciones,
        IEnumerable<ItemCompraDto> items,
        CancellationToken cancellationToken = default);

    Task<List<Compra>> ObtenerComprasAsync(
        DateTime? desde = null,
        DateTime? hasta = null,
        int? proveedorId = null,
        CancellationToken cancellationToken = default);

    Task<Compra?> ObtenerPorIdAsync(int compraId, CancellationToken cancellationToken = default);
    Task<List<DetalleCompra>> ObtenerHistorialComprasPorProductoAsync(int productoId, CancellationToken cancellationToken = default);
}
