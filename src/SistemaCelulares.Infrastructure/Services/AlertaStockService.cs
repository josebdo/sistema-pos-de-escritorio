using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;
using SistemaCelulares.Infrastructure.Data;

namespace SistemaCelulares.Infrastructure.Services;

public class AlertaStockService : IAlertaStockService
{
    private readonly AppDbContext _context;

    public AlertaStockService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<AlertaStockDto>> ObtenerAlertasStockAsync(CancellationToken cancellationToken = default)
    {
        var productos = await _context.Productos
            .Include(p => p.Categoria)
            .Where(p => p.Activo && p.StockActual <= p.CantidadMinima)
            .OrderBy(p => p.StockActual)
            .ThenBy(p => p.Nombre)
            .ToListAsync(cancellationToken);

        return productos.Select(p =>
        {
            var criticidad = NivelCriticidadStock.Bajo;
            if (p.StockActual == 0)
            {
                criticidad = NivelCriticidadStock.Agotado;
            }
            else if (p.StockActual < p.CantidadMinima)
            {
                criticidad = NivelCriticidadStock.Critico;
            }

            return new AlertaStockDto
            {
                ProductoId = p.Id,
                Nombre = p.Nombre,
                Sku = p.Sku,
                CategoriaNombre = p.Categoria.Nombre,
                StockActual = p.StockActual,
                CantidadMinima = p.CantidadMinima,
                PrecioCosto = p.PrecioCosto,
                Criticidad = criticidad
            };
        }).ToList();
    }

    public async Task<int> ContarProductosCriticosAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Productos
            .CountAsync(p => p.Activo && p.StockActual <= p.CantidadMinima, cancellationToken);
    }
}
