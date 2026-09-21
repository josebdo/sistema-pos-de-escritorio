using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Infrastructure.Data;

namespace SistemaCelulares.Infrastructure.Services;

public class CategoriaService : ICategoriaService
{
    private readonly AppDbContext _context;

    public CategoriaService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Categoria>> ObtenerCategoriasAsync(bool soloActivas = true, CancellationToken cancellationToken = default)
    {
        IQueryable<Categoria> query = _context.Categorias
            .Include(c => c.Productos);

        if (soloActivas)
        {
            query = query.Where(c => c.Activo);
        }

        return await query.OrderBy(c => c.Nombre).ToListAsync(cancellationToken);
    }

    public async Task<Categoria?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Categorias
            .Include(c => c.Productos)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Categoria> CrearCategoriaAsync(string nombre, string? descripcion, string? prefijoSku = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre de la categoría es obligatorio.", nameof(nombre));

        var normalizado = nombre.Trim().ToLower();
        var existe = await _context.Categorias.AnyAsync(c => c.Nombre.ToLower() == normalizado, cancellationToken);
        if (existe)
            throw new InvalidOperationException($"Ya existe una categoría con el nombre '{nombre}'.");

        var prefijo = string.IsNullOrWhiteSpace(prefijoSku)
            ? (nombre.Trim().Length >= 3 ? nombre.Trim()[..3].ToUpper() : nombre.Trim().ToUpper())
            : prefijoSku.Trim().ToUpper();

        var categoria = new Categoria
        {
            Nombre = nombre.Trim(),
            Descripcion = descripcion?.Trim(),
            PrefijoSku = prefijo,
            Activo = true
        };

        _context.Categorias.Add(categoria);
        await _context.SaveChangesAsync(cancellationToken);

        return categoria;
    }

    public async Task<bool> ActualizarCategoriaAsync(int id, string nombre, string? descripcion, string? prefijoSku = null, CancellationToken cancellationToken = default)
    {
        var categoria = await _context.Categorias.FindAsync(new object[] { id }, cancellationToken);
        if (categoria == null) return false;

        var normalizado = nombre.Trim().ToLower();
        var duplicado = await _context.Categorias.AnyAsync(c => c.Id != id && c.Nombre.ToLower() == normalizado, cancellationToken);
        if (duplicado)
            throw new InvalidOperationException($"Ya existe otra categoría con el nombre '{nombre}'.");

        categoria.Nombre = nombre.Trim();
        categoria.Descripcion = descripcion?.Trim();

        if (!string.IsNullOrWhiteSpace(prefijoSku))
        {
            categoria.PrefijoSku = prefijoSku.Trim().ToUpper();
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CambiarEstadoActivoAsync(int id, bool activo, CancellationToken cancellationToken = default)
    {
        var categoria = await _context.Categorias
            .Include(c => c.Productos)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (categoria == null) return false;

        if (!activo && categoria.Productos.Any(p => p.Activo))
        {
            throw new InvalidOperationException("No se puede desactivar la categoría porque contiene productos activos.");
        }

        categoria.Activo = activo;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
