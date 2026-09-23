using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Infrastructure.Data;

namespace SistemaCelulares.Infrastructure.Services;

public class UsuarioService : IUsuarioService
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _hasher;

    public UsuarioService(AppDbContext context, IPasswordHasher hasher)
    {
        _context = context;
        _hasher = hasher;
    }

    public async Task<List<Usuario>> ObtenerTodosAsync(bool incluirInactivos = true, CancellationToken cancellationToken = default)
    {
        IQueryable<Usuario> query = _context.Usuarios
            .Include(u => u.Rol);

        if (!incluirInactivos)
        {
            query = query.Where(u => u.Activo);
        }

        return await query.OrderBy(u => u.NombreCompleto).ToListAsync(cancellationToken);
    }

    public async Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken = default)
    {
        return await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.NombreUsuario.ToLower() == nombreUsuario.Trim().ToLower(), cancellationToken);
    }

    public async Task<Usuario> CrearUsuarioAsync(
        string nombreCompleto,
        string nombreUsuario,
        string passwordTemporal,
        int rolId,
        string? email = null,
        string? telefono = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nombreCompleto))
            throw new ArgumentException("El nombre completo es obligatorio.");
        if (string.IsNullOrWhiteSpace(nombreUsuario))
            throw new ArgumentException("El nombre de usuario es obligatorio.");
        if (string.IsNullOrWhiteSpace(passwordTemporal) || passwordTemporal.Length < 6)
            throw new ArgumentException("La contraseña temporal debe tener al menos 6 caracteres.");

        var normalizado = nombreUsuario.Trim().ToLower();
        var existe = await _context.Usuarios.AnyAsync(u => u.NombreUsuario.ToLower() == normalizado, cancellationToken);
        if (existe)
            throw new InvalidOperationException($"Ya existe un usuario con el nombre '{nombreUsuario}'.");

        var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Id == rolId && r.Activo, cancellationToken);
        if (rol == null)
            throw new InvalidOperationException("El rol seleccionado no es válido o está inactivo.");

        if (rol.Nombre == Rol.SuperAdmin)
            throw new InvalidOperationException("No está permitido crear usuarios adicionales con el rol Super Admin.");

        var usuario = new Usuario
        {
            NombreCompleto = nombreCompleto.Trim(),
            NombreUsuario = nombreUsuario.Trim(),
            PasswordHash = _hasher.HashPassword(passwordTemporal),
            RolId = rolId,
            Rol = rol,
            Email = email?.Trim(),
            Telefono = telefono?.Trim(),
            Activo = true,
            DebeCambiarPassword = true, // Obligatorio para usuarios nuevos
            FechaCreacion = DateTime.UtcNow
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync(cancellationToken);

        return usuario;
    }

    public async Task<bool> ActualizarUsuarioAsync(
        int id,
        string nombreCompleto,
        int rolId,
        string? email = null,
        string? telefono = null,
        string? nombreUsuario = null,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _context.Usuarios.FindAsync(new object[] { id }, cancellationToken);
        if (usuario == null) return false;

        var rolExiste = await _context.Roles.AnyAsync(r => r.Id == rolId && r.Activo, cancellationToken);
        if (!rolExiste)
            throw new InvalidOperationException("El rol seleccionado no es válido.");

        if (!string.IsNullOrWhiteSpace(nombreUsuario))
        {
            var normalizado = nombreUsuario.Trim().ToLower();
            if (usuario.NombreUsuario.ToLower() != normalizado)
            {
                var existe = await _context.Usuarios.AnyAsync(u => u.Id != id && u.NombreUsuario.ToLower() == normalizado, cancellationToken);
                if (existe)
                    throw new InvalidOperationException($"Ya existe otro usuario con el nombre de usuario '{nombreUsuario}'.");

                usuario.NombreUsuario = nombreUsuario.Trim();
            }
        }

        usuario.NombreCompleto = nombreCompleto.Trim();
        usuario.RolId = rolId;
        usuario.Email = email?.Trim();
        usuario.Telefono = telefono?.Trim();

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ActualizarMiPerfilAsync(int id, string nombreCompleto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nombreCompleto))
            throw new ArgumentException("El nombre completo es obligatorio.");

        var usuario = await _context.Usuarios.FindAsync(new object[] { id }, cancellationToken);
        if (usuario == null) return false;

        usuario.NombreCompleto = nombreCompleto.Trim();
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ResetearPasswordAsync(int id, string nuevaPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nuevaPassword) || nuevaPassword.Length < 6)
            throw new ArgumentException("La nueva contraseña debe tener al menos 6 caracteres.");

        var usuario = await _context.Usuarios.FindAsync(new object[] { id }, cancellationToken);
        if (usuario == null) return false;

        usuario.PasswordHash = _hasher.HashPassword(nuevaPassword.Trim());
        usuario.DebeCambiarPassword = false;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CambiarEstadoActivoAsync(int id, bool activo, CancellationToken cancellationToken = default)
    {
        var usuario = await _context.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (usuario == null) return false;

        // No permitir desactivar al único superadmin si solo hay uno
        if (!activo && usuario.Rol.Nombre == Rol.SuperAdmin)
        {
            var superAdminsActivos = await _context.Usuarios.CountAsync(u => u.Rol.Nombre == Rol.SuperAdmin && u.Activo, cancellationToken);
            if (superAdminsActivos <= 1)
                throw new InvalidOperationException("No se puede desactivar al único Super Admin activo del sistema.");
        }

        usuario.Activo = activo;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
