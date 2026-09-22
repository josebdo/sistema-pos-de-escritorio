using SistemaCelulares.Core.Entities;

namespace SistemaCelulares.Core.Interfaces;

public interface IProductoService
{
    Task<List<Producto>> ObtenerProductosAsync(bool soloActivos = true, int? categoriaId = null, string? busqueda = null, CancellationToken cancellationToken = default);
    Task<Producto?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Producto?> ObtenerPorSkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<Producto?> ObtenerPorCodigoBarrasAsync(string codigoBarras, CancellationToken cancellationToken = default);
    Task<Producto?> BuscarPorCodigoBarrasOSkuAsync(string codigoOSku, CancellationToken cancellationToken = default);
    Task<string> GenerarSkuSiguienteAsync(int categoriaId, CancellationToken cancellationToken = default);
    Task<Producto> CrearProductoAsync(
        string nombre,
        int categoriaId,
        decimal precioCosto,
        decimal precioVenta,
        int stockInicial,
        int cantidadMinima,
        string? sku = null,
        string? codigoBarras = null,
        string? descripcion = null,
        bool requiereSerie = false,
        CancellationToken cancellationToken = default);

    Task<bool> ActualizarProductoAsync(
        int id,
        string nombre,
        int categoriaId,
        decimal precioCosto,
        decimal precioVenta,
        int cantidadMinima,
        string sku,
        string? codigoBarras = null,
        string? descripcion = null,
        CancellationToken cancellationToken = default);

    Task<bool> CambiarEstadoActivoAsync(int id, bool activo, CancellationToken cancellationToken = default);
    Task<bool> AjustarStockAsync(int productoId, int nuevoStock, string? motivo = null, CancellationToken cancellationToken = default);
    Task<List<Producto>> ObtenerProductosBajoStockAsync(CancellationToken cancellationToken = default);

    // Métodos para Unidades físicas e IMEI
    Task<List<UnidadProducto>> ObtenerUnidadesPorProductoAsync(int productoId, EstadoUnidadProducto? estado = null, CancellationToken cancellationToken = default);
    Task<UnidadProducto?> ObtenerUnidadPorImeiAsync(string imei, CancellationToken cancellationToken = default);
    Task<UnidadProducto> RegistrarUnidadImeiAsync(int productoId, string imei, string? notas = null, CancellationToken cancellationToken = default);
    Task<bool> ValidarImeiDisponibleAsync(string imei, CancellationToken cancellationToken = default);
}
