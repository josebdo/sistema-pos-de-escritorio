using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Infrastructure.Data;

namespace SistemaCelulares.Infrastructure.Services;

public class RolService : IRolService
{
    private readonly AppDbContext _context;

    public RolService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Rol>> ObtenerRolesAsync(bool soloActivos = true, CancellationToken cancellationToken = default)
    {
        IQueryable<Rol> query = _context.Roles
            .Include(r => r.RolPermisos)
                .ThenInclude(rp => rp.Permiso);

        if (soloActivos)
        {
            query = query.Where(r => r.Activo);
        }

        return await query.OrderBy(r => r.Nombre).ToListAsync(cancellationToken);
    }

    public async Task<Rol?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Include(r => r.RolPermisos)
                .ThenInclude(rp => rp.Permiso)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<List<Permiso>> ObtenerTodosLosPermisosAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Permisos
            .OrderBy(p => p.Modulo)
            .ThenBy(p => p.Descripcion)
            .ToListAsync(cancellationToken);
    }

    public async Task<Rol> CrearRolPersonalizadoAsync(
        string nombre,
        string? descripcion,
        IEnumerable<string> codigosPermisos,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del rol es obligatorio.");

        var normalizado = nombre.Trim().ToLower();
        var existe = await _context.Roles.AnyAsync(r => r.Nombre.ToLower() == normalizado, cancellationToken);
        if (existe)
            throw new InvalidOperationException($"Ya existe un rol con el nombre '{nombre}'.");

        var rol = new Rol
        {
            Nombre = nombre.Trim(),
            Descripcion = descripcion?.Trim(),
            EsFijo = false,
            Activo = true
        };

        var listaCodigos = codigosPermisos.Select(c => c.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var permisos = await _context.Permisos
            .Where(p => listaCodigos.Contains(p.Codigo))
            .ToListAsync(cancellationToken);

        foreach (var p in permisos)
        {
            rol.RolPermisos.Add(new RolPermiso { Rol = rol, PermisoId = p.Id });
        }

        _context.Roles.Add(rol);
        await _context.SaveChangesAsync(cancellationToken);

        return rol;
    }

    public async Task<bool> ActualizarRolAsync(
        int rolId,
        string nombre,
        string? descripcion,
        IEnumerable<string> codigosPermisos,
        CancellationToken cancellationToken = default)
    {
        var rol = await _context.Roles
            .Include(r => r.RolPermisos)
            .FirstOrDefaultAsync(r => r.Id == rolId, cancellationToken);

        if (rol == null) return false;

        // No permitir cambiar el nombre de roles fijos
        if (rol.EsFijo && !string.Equals(rol.Nombre, nombre.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("No se puede cambiar el nombre de un rol fijo del sistema.");
        }

        if (!rol.EsFijo)
        {
            rol.Nombre = nombre.Trim();
        }

        rol.Descripcion = descripcion?.Trim();

        // Si no es SuperAdmin, actualizar permisos
        if (rol.Nombre != Rol.SuperAdmin)
        {
            var listaCodigos = codigosPermisos.Select(c => c.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var permisosNuevos = await _context.Permisos
                .Where(p => listaCodigos.Contains(p.Codigo))
                .ToListAsync(cancellationToken);

            // Eliminar anteriores
            _context.RolPermisos.RemoveRange(rol.RolPermisos);

            // Agregar nuevos
            foreach (var p in permisosNuevos)
            {
                rol.RolPermisos.Add(new RolPermiso { RolId = rol.Id, PermisoId = p.Id });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> EliminarRolAsync(int rolId, CancellationToken cancellationToken = default)
    {
        var rol = await _context.Roles.Include(r => r.Usuarios).FirstOrDefaultAsync(r => r.Id == rolId, cancellationToken);
        if (rol == null) return false;

        if (rol.EsFijo)
            throw new InvalidOperationException("No se puede eliminar un rol fijo del sistema (Super Admin, Admin, Cajero).");

        if (rol.Usuarios.Any(u => u.Activo))
            throw new InvalidOperationException("No se puede eliminar el rol porque tiene usuarios activos asignados.");

        rol.Activo = false;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
