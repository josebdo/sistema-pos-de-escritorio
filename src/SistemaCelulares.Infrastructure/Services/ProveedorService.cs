using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Infrastructure.Data;

namespace SistemaCelulares.Infrastructure.Services;

public class ProveedorService : IProveedorService
{
    private readonly AppDbContext _context;

    public ProveedorService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Proveedor>> ObtenerProveedoresAsync(
        bool soloActivos = true,
        string? busqueda = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Proveedor> query = _context.Proveedores;

        if (soloActivos)
        {
            query = query.Where(p => p.Activo);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var q = busqueda.Trim().ToLower();
            query = query.Where(p =>
                p.Nombre.ToLower().Contains(q) ||
                (p.Rnc != null && p.Rnc.ToLower().Contains(q)) ||
                (p.Telefono != null && p.Telefono.ToLower().Contains(q)) ||
                (p.Contacto != null && p.Contacto.ToLower().Contains(q)) ||
                (p.Email != null && p.Email.ToLower().Contains(q))
            );
        }

        return await query.OrderBy(p => p.Nombre).ToListAsync(cancellationToken);
    }

    public async Task<Proveedor?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Proveedores.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<Proveedor?> ObtenerPorRncAsync(string rnc, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rnc)) return null;

        var q = rnc.Trim().ToLower();
        return await _context.Proveedores
            .FirstOrDefaultAsync(p => p.Rnc != null && p.Rnc.ToLower() == q, cancellationToken);
    }

    public async Task<Proveedor> CrearProveedorAsync(
        string nombre,
        string? rnc = null,
        string? telefono = null,
        string? email = null,
        string? direccion = null,
        string? contacto = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del proveedor es obligatorio.", nameof(nombre));

        string? rncFinal = null;
        if (!string.IsNullOrWhiteSpace(rnc))
        {
            rncFinal = rnc.Trim();
            var rncExiste = await _context.Proveedores.AnyAsync(p => p.Rnc != null && p.Rnc.ToLower() == rncFinal.ToLower(), cancellationToken);
            if (rncExiste)
                throw new InvalidOperationException($"Ya existe un proveedor registrado con el RNC '{rncFinal}'.");
        }

        var proveedor = new Proveedor
        {
            Nombre = nombre.Trim(),
            Rnc = rncFinal,
            Telefono = telefono?.Trim(),
            Email = email?.Trim(),
            Direccion = direccion?.Trim(),
            Contacto = contacto?.Trim(),
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Proveedores.Add(proveedor);
        await _context.SaveChangesAsync(cancellationToken);

        return proveedor;
    }

    public async Task<bool> ActualizarProveedorAsync(
        int id,
        string nombre,
        string? rnc = null,
        string? telefono = null,
        string? email = null,
        string? direccion = null,
        string? contacto = null,
        CancellationToken cancellationToken = default)
    {
        var proveedor = await _context.Proveedores.FindAsync(new object[] { id }, cancellationToken);
        if (proveedor == null) return false;

        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del proveedor es obligatorio.", nameof(nombre));

        string? rncFinal = null;
        if (!string.IsNullOrWhiteSpace(rnc))
        {
            rncFinal = rnc.Trim();
            var rncDuplicado = await _context.Proveedores.AnyAsync(p => p.Id != id && p.Rnc != null && p.Rnc.ToLower() == rncFinal.ToLower(), cancellationToken);
            if (rncDuplicado)
                throw new InvalidOperationException($"El RNC '{rncFinal}' ya está registrado con otro proveedor.");
        }

        proveedor.Nombre = nombre.Trim();
        proveedor.Rnc = rncFinal;
        proveedor.Telefono = telefono?.Trim();
        proveedor.Email = email?.Trim();
        proveedor.Direccion = direccion?.Trim();
        proveedor.Contacto = contacto?.Trim();
        proveedor.UltimaModificacion = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CambiarEstadoActivoAsync(int id, bool activo, CancellationToken cancellationToken = default)
    {
        var proveedor = await _context.Proveedores.FindAsync(new object[] { id }, cancellationToken);
        if (proveedor == null) return false;

        proveedor.Activo = activo;
        proveedor.UltimaModificacion = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
