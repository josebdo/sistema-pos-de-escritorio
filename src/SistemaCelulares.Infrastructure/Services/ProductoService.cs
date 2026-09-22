using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Infrastructure.Data;

namespace SistemaCelulares.Infrastructure.Services;

public class ProductoService : IProductoService
{
    private readonly AppDbContext _context;

    public ProductoService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Producto>> ObtenerProductosAsync(
        bool soloActivos = true,
        int? categoriaId = null,
        string? busqueda = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Producto> query = _context.Productos
            .Include(p => p.Categoria);

        if (soloActivos)
        {
            query = query.Where(p => p.Activo);
        }

        if (categoriaId.HasValue && categoriaId.Value > 0)
        {
            query = query.Where(p => p.CategoriaId == categoriaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var q = busqueda.Trim().ToLower();
            query = query.Where(p =>
                p.Nombre.ToLower().Contains(q) ||
                p.Sku.ToLower().Contains(q) ||
                (p.CodigoBarras != null && p.CodigoBarras.ToLower().Contains(q)) ||
                p.Categoria.Nombre.ToLower().Contains(q)
            );
        }

        return await query.OrderBy(p => p.Nombre).ToListAsync(cancellationToken);
    }

    public async Task<Producto?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Productos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Producto?> ObtenerPorSkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sku)) return null;

        var q = sku.Trim().ToLower();
        return await _context.Productos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Sku.ToLower() == q, cancellationToken);
    }

    public async Task<Producto?> ObtenerPorCodigoBarrasAsync(string codigoBarras, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codigoBarras)) return null;

        var q = codigoBarras.Trim().ToLower();
        return await _context.Productos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.CodigoBarras != null && p.CodigoBarras.ToLower() == q, cancellationToken);
    }

    public async Task<Producto?> BuscarPorCodigoBarrasOSkuAsync(string codigoOSku, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codigoOSku)) return null;

        var q = codigoOSku.Trim().ToLower();

        // 1. Intentar por Código de Barras (coincidencia exacta)
        var productoPorCb = await _context.Productos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.CodigoBarras != null && p.CodigoBarras.ToLower() == q, cancellationToken);

        if (productoPorCb != null) return productoPorCb;

        // 2. Intentar por SKU (coincidencia exacta)
        return await _context.Productos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Sku.ToLower() == q, cancellationToken);
    }

    public async Task<string> GenerarSkuSiguienteAsync(int categoriaId, CancellationToken cancellationToken = default)
    {
        var categoria = await _context.Categorias.FindAsync(new object[] { categoriaId }, cancellationToken);
        var prefijo = (categoria?.PrefijoSku ?? "PROD").ToUpper();

        var productosConPrefijo = await _context.Productos
            .Where(p => p.Sku.StartsWith(prefijo + "-"))
            .Select(p => p.Sku)
            .ToListAsync(cancellationToken);

        int maxCorrelativo = 0;
        foreach (var sku in productosConPrefijo)
        {
            var partes = sku.Split('-');
            if (partes.Length >= 2 && int.TryParse(partes[1], out int numero))
            {
                if (numero > maxCorrelativo) maxCorrelativo = numero;
            }
        }

        var siguiente = maxCorrelativo + 1;
        var nuevoSku = $"{prefijo}-{siguiente:D4}";

        // Asegurar que no colisione
        while (await _context.Productos.AnyAsync(p => p.Sku.ToLower() == nuevoSku.ToLower(), cancellationToken))
        {
            siguiente++;
            nuevoSku = $"{prefijo}-{siguiente:D4}";
        }

        return nuevoSku;
    }

    public async Task<Producto> CrearProductoAsync(
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
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del producto es obligatorio.", nameof(nombre));

        if (precioCosto < 0)
            throw new ArgumentException("El precio de costo no puede ser negativo.", nameof(precioCosto));

        if (precioVenta < 0)
            throw new ArgumentException("El precio de venta no puede ser negativo.", nameof(precioVenta));

        if (stockInicial < 0)
            throw new ArgumentException("El stock inicial no puede ser negativo.", nameof(stockInicial));

        if (cantidadMinima < 0)
            throw new ArgumentException("La cantidad mínima no puede ser negativa.", nameof(cantidadMinima));

        var categoria = await _context.Categorias.FindAsync(new object[] { categoriaId }, cancellationToken);
        if (categoria == null || !categoria.Activo)
            throw new InvalidOperationException("La categoría seleccionada no existe o está inactiva.");

        string skuFinal;
        if (string.IsNullOrWhiteSpace(sku))
        {
            skuFinal = await GenerarSkuSiguienteAsync(categoriaId, cancellationToken);
        }
        else
        {
            skuFinal = sku.Trim().ToUpper();
            var skuExiste = await _context.Productos.AnyAsync(p => p.Sku.ToLower() == skuFinal.ToLower(), cancellationToken);
            if (skuExiste)
                throw new InvalidOperationException($"El SKU '{skuFinal}' ya se encuentra en uso por otro producto.");
        }

        string? codigoBarrasFinal = null;
        if (!string.IsNullOrWhiteSpace(codigoBarras))
        {
            codigoBarrasFinal = codigoBarras.Trim();
            var cbExiste = await _context.Productos.AnyAsync(p => p.CodigoBarras != null && p.CodigoBarras.ToLower() == codigoBarrasFinal.ToLower(), cancellationToken);
            if (cbExiste)
                throw new InvalidOperationException($"El código de barras '{codigoBarrasFinal}' ya está registrado.");
        }

        var producto = new Producto
        {
            Nombre = nombre.Trim(),
            Descripcion = descripcion?.Trim(),
            CategoriaId = categoriaId,
            Sku = skuFinal,
            PrecioCosto = precioCosto,
            PrecioVenta = precioVenta,
            StockActual = stockInicial,
            CantidadMinima = cantidadMinima,
            CodigoBarras = codigoBarrasFinal,
            RequiereSerie = requiereSerie,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Productos.Add(producto);
        await _context.SaveChangesAsync(cancellationToken);

        return producto;
    }

    public async Task<bool> ActualizarProductoAsync(
        int id,
        string nombre,
        int categoriaId,
        decimal precioCosto,
        decimal precioVenta,
        int cantidadMinima,
        string sku,
        string? codigoBarras = null,
        string? descripcion = null,
        CancellationToken cancellationToken = default)
    {
        var producto = await _context.Productos.FindAsync(new object[] { id }, cancellationToken);
        if (producto == null) return false;

        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del producto es obligatorio.", nameof(nombre));

        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("El SKU es obligatorio.", nameof(sku));

        if (precioCosto < 0 || precioVenta < 0)
            throw new ArgumentException("Los precios no pueden ser negativos.");

        var categoria = await _context.Categorias.FindAsync(new object[] { categoriaId }, cancellationToken);
        if (categoria == null || !categoria.Activo)
            throw new InvalidOperationException("La categoría seleccionada no existe o está inactiva.");

        var skuFinal = sku.Trim().ToUpper();
        var skuDuplicado = await _context.Productos.AnyAsync(p => p.Id != id && p.Sku.ToLower() == skuFinal.ToLower(), cancellationToken);
        if (skuDuplicado)
            throw new InvalidOperationException($"El SKU '{skuFinal}' ya pertenece a otro producto.");

        string? cbFinal = null;
        if (!string.IsNullOrWhiteSpace(codigoBarras))
        {
            cbFinal = codigoBarras.Trim();
            var cbDuplicado = await _context.Productos.AnyAsync(p => p.Id != id && p.CodigoBarras != null && p.CodigoBarras.ToLower() == cbFinal.ToLower(), cancellationToken);
            if (cbDuplicado)
                throw new InvalidOperationException($"El código de barras '{cbFinal}' ya está registrado en otro producto.");
        }

        producto.Nombre = nombre.Trim();
        producto.CategoriaId = categoriaId;
        producto.PrecioCosto = precioCosto;
        producto.PrecioVenta = precioVenta;
        producto.CantidadMinima = cantidadMinima;
        producto.Sku = skuFinal;
        producto.CodigoBarras = cbFinal;
        producto.Descripcion = descripcion?.Trim();
        producto.UltimaModificacion = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CambiarEstadoActivoAsync(int id, bool activo, CancellationToken cancellationToken = default)
    {
        var producto = await _context.Productos.FindAsync(new object[] { id }, cancellationToken);
        if (producto == null) return false;

        producto.Activo = activo;
        producto.UltimaModificacion = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> AjustarStockAsync(int productoId, int nuevoStock, string? motivo = null, CancellationToken cancellationToken = default)
    {
        if (nuevoStock < 0)
            throw new ArgumentException("El stock no puede ser negativo.", nameof(nuevoStock));

        var producto = await _context.Productos.FindAsync(new object[] { productoId }, cancellationToken);
        if (producto == null) return false;

        producto.StockActual = nuevoStock;
        producto.UltimaModificacion = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<Producto>> ObtenerProductosBajoStockAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Productos
            .Include(p => p.Categoria)
            .Where(p => p.Activo && p.StockActual <= p.CantidadMinima)
            .OrderBy(p => p.StockActual)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UnidadProducto>> ObtenerUnidadesPorProductoAsync(
        int productoId,
        EstadoUnidadProducto? estado = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<UnidadProducto> query = _context.UnidadesProducto
            .Include(u => u.Venta)
            .Where(u => u.ProductoId == productoId);

        if (estado.HasValue)
        {
            query = query.Where(u => u.Estado == estado.Value);
        }

        return await query.OrderByDescending(u => u.FechaIngreso).ToListAsync(cancellationToken);
    }

    public async Task<UnidadProducto?> ObtenerUnidadPorImeiAsync(string imei, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imei)) return null;

        var q = imei.Trim().ToLower();
        return await _context.UnidadesProducto
            .Include(u => u.Producto)
                .ThenInclude(p => p.Categoria)
            .Include(u => u.Venta)
            .FirstOrDefaultAsync(u => u.Imei.ToLower() == q, cancellationToken);
    }

    public async Task<UnidadProducto> RegistrarUnidadImeiAsync(
        int productoId,
        string imei,
        string? notas = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imei))
            throw new ArgumentException("El IMEI o número de serie es obligatorio.", nameof(imei));

        var imeiLimpio = imei.Trim();
        var producto = await _context.Productos.FindAsync(new object[] { productoId }, cancellationToken)
            ?? throw new InvalidOperationException($"El producto con ID {productoId} no existe.");

        var imeiExiste = await _context.UnidadesProducto.AnyAsync(u => u.Imei.ToLower() == imeiLimpio.ToLower(), cancellationToken);
        if (imeiExiste)
            throw new InvalidOperationException($"El IMEI '{imeiLimpio}' ya se encuentra registrado en el sistema.");

        var unidad = new UnidadProducto
        {
            ProductoId = productoId,
            Imei = imeiLimpio,
            Estado = EstadoUnidadProducto.EnStock,
            FechaIngreso = DateTime.UtcNow,
            Notas = notas?.Trim()
        };

        _context.UnidadesProducto.Add(unidad);

        // Si el producto requiere serie, recalcular su stock según unidades en stock
        if (producto.RequiereSerie)
        {
            producto.StockActual = await _context.UnidadesProducto
                .CountAsync(u => u.ProductoId == productoId && u.Estado == EstadoUnidadProducto.EnStock, cancellationToken) + 1;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return unidad;
    }

    public async Task<bool> ValidarImeiDisponibleAsync(string imei, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imei)) return false;

        var q = imei.Trim().ToLower();
        return await _context.UnidadesProducto
            .AnyAsync(u => u.Imei.ToLower() == q && u.Estado == EstadoUnidadProducto.EnStock, cancellationToken);
    }
}
