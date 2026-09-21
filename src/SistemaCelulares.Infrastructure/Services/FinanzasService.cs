using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;
using SistemaCelulares.Infrastructure.Data;

namespace SistemaCelulares.Infrastructure.Services;

public class FinanzasService : IFinanzasService
{
    private readonly AppDbContext _context;

    public FinanzasService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<MovimientoFinanciero> RegistrarMovimientoAsync(
        TipoMovimientoFinanciero tipo,
        int categoriaId,
        decimal monto,
        DateTime fecha,
        string descripcion,
        int usuarioId,
        string? numeroComprobante = null,
        CancellationToken cancellationToken = default)
    {
        if (monto <= 0)
        {
            throw new ArgumentException("El monto del movimiento debe ser mayor a cero (RD$ 0.00).", nameof(monto));
        }

        if (string.IsNullOrWhiteSpace(descripcion))
        {
            throw new ArgumentException("La descripción o concepto del movimiento es obligatorio.", nameof(descripcion));
        }

        var categoria = await _context.CategoriasFinancieras
            .FirstOrDefaultAsync(c => c.Id == categoriaId, cancellationToken);

        if (categoria == null)
        {
            throw new InvalidOperationException($"La categoría financiera con ID {categoriaId} no existe.");
        }

        if (!categoria.Activo)
        {
            throw new InvalidOperationException($"La categoría financiera '{categoria.Nombre}' está inactiva.");
        }

        if (categoria.Tipo != tipo)
        {
            throw new InvalidOperationException($"La categoría '{categoria.Nombre}' es de tipo {categoria.Tipo}, pero se intenta registrar un movimiento de tipo {tipo}.");
        }

        var movimiento = new MovimientoFinanciero
        {
            Tipo = tipo,
            CategoriaFinancieraId = categoriaId,
            Monto = monto,
            Fecha = fecha,
            Descripcion = descripcion.Trim(),
            NumeroComprobante = numeroComprobante?.Trim(),
            UsuarioId = usuarioId,
            FechaCreacion = DateTime.Now
        };

        _context.MovimientosFinancieros.Add(movimiento);
        await _context.SaveChangesAsync(cancellationToken);

        await _context.Entry(movimiento).Reference(m => m.CategoriaFinanciera).LoadAsync(cancellationToken);
        await _context.Entry(movimiento).Reference(m => m.Usuario).LoadAsync(cancellationToken);

        return movimiento;
    }

    public async Task<List<MovimientoFinanciero>> ObtenerMovimientosAsync(
        DateTime? desde = null,
        DateTime? hasta = null,
        TipoMovimientoFinanciero? tipo = null,
        int? categoriaId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.MovimientosFinancieros
            .Include(m => m.CategoriaFinanciera)
            .Include(m => m.Usuario)
            .AsNoTracking();

        if (desde.HasValue)
        {
            query = query.Where(m => m.Fecha >= desde.Value);
        }

        if (hasta.HasValue)
        {
            query = query.Where(m => m.Fecha <= hasta.Value);
        }

        if (tipo.HasValue)
        {
            query = query.Where(m => m.Tipo == tipo.Value);
        }

        if (categoriaId.HasValue)
        {
            query = query.Where(m => m.CategoriaFinancieraId == categoriaId.Value);
        }

        return await query.OrderByDescending(m => m.Fecha).ToListAsync(cancellationToken);
    }

    public async Task<BalanceNetoDto> ObtenerBalanceNetoPeriodoAsync(
        DateTime desde,
        DateTime hasta,
        CancellationToken cancellationToken = default)
    {
        var fDesde = desde.Date;
        var fHasta = hasta.Date.AddDays(1).AddTicks(-1);

        // Obtener movimientos en el periodo
        var movimientos = await _context.MovimientosFinancieros
            .Include(m => m.CategoriaFinanciera)
            .Where(m => m.Fecha >= fDesde && m.Fecha <= fHasta)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Obtener compras a proveedores en el periodo
        var totalCompras = await _context.Compras
            .Where(c => c.FechaCompra >= fDesde && c.FechaCompra <= fHasta)
            .SumAsync(c => c.Total, cancellationToken);

        var totalOtrosIngresos = movimientos
            .Where(m => m.Tipo == TipoMovimientoFinanciero.Ingreso)
            .Sum(m => m.Monto);

        var totalGastosOperativos = movimientos
            .Where(m => m.Tipo == TipoMovimientoFinanciero.Gasto)
            .Sum(m => m.Monto);

        var gastosPorCat = movimientos
            .Where(m => m.Tipo == TipoMovimientoFinanciero.Gasto)
            .GroupBy(m => m.CategoriaFinanciera?.Nombre ?? "Sin Categoría")
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Monto));

        if (totalCompras > 0)
        {
            gastosPorCat["Compras de Inventario / Proveedores"] = totalCompras;
        }

        var ingresosPorCat = movimientos
            .Where(m => m.Tipo == TipoMovimientoFinanciero.Ingreso)
            .GroupBy(m => m.CategoriaFinanciera?.Nombre ?? "Sin Categoría")
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Monto));

        return new BalanceNetoDto
        {
            Desde = desde,
            Hasta = hasta,
            TotalVentas = 0, // Las ventas se integrarán en Fase 10/Ventas
            TotalOtrosIngresos = totalOtrosIngresos,
            TotalGastosOperativos = totalGastosOperativos,
            TotalComprasMercancia = totalCompras,
            CantidadGastos = movimientos.Count(m => m.Tipo == TipoMovimientoFinanciero.Gasto),
            CantidadOtrosIngresos = movimientos.Count(m => m.Tipo == TipoMovimientoFinanciero.Ingreso),
            GastosPorCategoria = gastosPorCat,
            IngresosPorCategoria = ingresosPorCat
        };
    }

    public async Task<List<CategoriaFinanciera>> ObtenerCategoriasFinancierasAsync(
        TipoMovimientoFinanciero? tipo = null,
        bool soloActivas = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CategoriasFinancieras.AsNoTracking();

        if (tipo.HasValue)
        {
            query = query.Where(c => c.Tipo == tipo.Value);
        }

        if (soloActivas)
        {
            query = query.Where(c => c.Activo);
        }

        return await query.OrderBy(c => c.Nombre).ToListAsync(cancellationToken);
    }

    public async Task<CategoriaFinanciera> CrearCategoriaFinancieraAsync(
        string nombre,
        TipoMovimientoFinanciero tipo,
        string? descripcion = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre de la categoría es requerido.", nameof(nombre));
        }

        var nombreNormalizado = nombre.Trim();

        var existe = await _context.CategoriasFinancieras
            .AnyAsync(c => c.Nombre.ToLower() == nombreNormalizado.ToLower() && c.Tipo == tipo, cancellationToken);

        if (existe)
        {
            throw new InvalidOperationException($"Ya existe una categoría de {tipo} con el nombre '{nombreNormalizado}'.");
        }

        var categoria = new CategoriaFinanciera
        {
            Nombre = nombreNormalizado,
            Tipo = tipo,
            Descripcion = descripcion?.Trim(),
            Activo = true
        };

        _context.CategoriasFinancieras.Add(categoria);
        await _context.SaveChangesAsync(cancellationToken);

        return categoria;
    }

    public async Task<bool> CambiarEstadoActivoCategoriaAsync(
        int id,
        bool activo,
        CancellationToken cancellationToken = default)
    {
        var categoria = await _context.CategoriasFinancieras.FindAsync([id], cancellationToken);
        if (categoria == null)
        {
            return false;
        }

        categoria.Activo = activo;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
