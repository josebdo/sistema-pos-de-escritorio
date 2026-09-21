using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;
using SistemaCelulares.Infrastructure.Data;

namespace SistemaCelulares.Infrastructure.Services;

public class CompraService : ICompraService
{
    private readonly AppDbContext _context;

    public CompraService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Compra> RegistrarCompraAsync(
        int proveedorId,
        int usuarioId,
        string? numeroFactura,
        string? observaciones,
        IEnumerable<ItemCompraDto> items,
        CancellationToken cancellationToken = default)
    {
        var itemsList = items?.ToList() ?? new List<ItemCompraDto>();
        if (itemsList.Count == 0)
            throw new ArgumentException("Debe agregar al menos un producto a la compra.", nameof(items));

        var proveedor = await _context.Proveedores.FindAsync(new object[] { proveedorId }, cancellationToken);
        if (proveedor == null || !proveedor.Activo)
            throw new InvalidOperationException("El proveedor seleccionado no existe o está inactivo.");

        var usuario = await _context.Usuarios.FindAsync(new object[] { usuarioId }, cancellationToken);
        if (usuario == null || !usuario.Activo)
            throw new InvalidOperationException("El usuario que registra la compra no es válido o está inactivo.");

        var compra = new Compra
        {
            ProveedorId = proveedorId,
            UsuarioId = usuarioId,
            FechaCompra = DateTime.UtcNow,
            NumeroFactura = numeroFactura?.Trim(),
            Observaciones = observaciones?.Trim(),
            Total = 0m
        };

        decimal totalCompra = 0m;

        foreach (var item in itemsList)
        {
            if (item.Cantidad <= 0)
                throw new ArgumentException($"La cantidad para el producto ID {item.ProductoId} debe ser mayor a cero.");

            if (item.CostoUnitario < 0)
                throw new ArgumentException($"El costo unitario para el producto ID {item.ProductoId} no puede ser negativo.");

            var producto = await _context.Productos.FindAsync(new object[] { item.ProductoId }, cancellationToken);
            if (producto == null || !producto.Activo)
                throw new InvalidOperationException($"El producto ID {item.ProductoId} no existe o está inactivo.");

            var subtotal = item.Cantidad * item.CostoUnitario;
            totalCompra += subtotal;

            var detalle = new DetalleCompra
            {
                Compra = compra,
                ProductoId = producto.Id,
                Cantidad = item.Cantidad,
                CostoUnitario = item.CostoUnitario,
                Subtotal = subtotal
            };

            compra.Detalles.Add(detalle);

            // Regla de Negocio (DEC-006):
            // 1. Incrementar stock
            producto.StockActual += item.Cantidad;

            // 2. Actualizar precio de costo según método "último costo"
            if (producto.PrecioCosto != item.CostoUnitario)
            {
                producto.PrecioCosto = item.CostoUnitario;
            }

            // 3. El precio de venta NO se actualiza automáticamente
            producto.UltimaModificacion = DateTime.UtcNow;
        }

        compra.Total = totalCompra;

        _context.Compras.Add(compra);
        await _context.SaveChangesAsync(cancellationToken);

        return compra;
    }

    public async Task<List<Compra>> ObtenerComprasAsync(
        DateTime? desde = null,
        DateTime? hasta = null,
        int? proveedorId = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Compra> query = _context.Compras
            .Include(c => c.Proveedor)
            .Include(c => c.Usuario)
            .Include(c => c.Detalles)
                .ThenInclude(d => d.Producto);

        if (desde.HasValue)
        {
            var fDesde = desde.Value.Date;
            query = query.Where(c => c.FechaCompra >= fDesde);
        }

        if (hasta.HasValue)
        {
            var fHasta = hasta.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(c => c.FechaCompra <= fHasta);
        }

        if (proveedorId.HasValue && proveedorId.Value > 0)
        {
            query = query.Where(c => c.ProveedorId == proveedorId.Value);
        }

        return await query.OrderByDescending(c => c.FechaCompra).ToListAsync(cancellationToken);
    }

    public async Task<Compra?> ObtenerPorIdAsync(int compraId, CancellationToken cancellationToken = default)
    {
        return await _context.Compras
            .Include(c => c.Proveedor)
            .Include(c => c.Usuario)
            .Include(c => c.Detalles)
                .ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(c => c.Id == compraId, cancellationToken);
    }

    public async Task<List<DetalleCompra>> ObtenerHistorialComprasPorProductoAsync(int productoId, CancellationToken cancellationToken = default)
    {
        return await _context.DetalleCompras
            .Include(d => d.Compra)
                .ThenInclude(c => c.Proveedor)
            .Include(d => d.Compra)
                .ThenInclude(c => c.Usuario)
            .Where(d => d.ProductoId == productoId)
            .OrderByDescending(d => d.Compra.FechaCompra)
            .ToListAsync(cancellationToken);
    }
}
