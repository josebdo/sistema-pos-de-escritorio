using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;
using SistemaCelulares.Infrastructure.Data;

namespace SistemaCelulares.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _hasher;

    public AuthService(AppDbContext context, IPasswordHasher hasher)
    {
        _context = context;
        _hasher = hasher;
    }

    public async Task<LoginResult> LoginAsync(string nombreUsuario, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(password))
            return LoginResult.Fallo(LoginStatus.CredencialesInvalidas, "El usuario y la contraseña son obligatorios.");

        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
                .ThenInclude(r => r.RolPermisos)
                    .ThenInclude(rp => rp.Permiso)
            .FirstOrDefaultAsync(u => u.NombreUsuario.ToLower() == nombreUsuario.Trim().ToLower(), cancellationToken);

        if (usuario == null)
            return LoginResult.Fallo(LoginStatus.UsuarioNoExiste, "Usuario o contraseña incorrectos.");

        if (!usuario.Activo)
            return LoginResult.Fallo(LoginStatus.UsuarioInactivo, "Este usuario se encuentra desactivado. Contacte al administrador.");

        if (!_hasher.VerifyPassword(password, usuario.PasswordHash))
            return LoginResult.Fallo(LoginStatus.CredencialesInvalidas, "Usuario o contraseña incorrectos.");

        // Actualizar último acceso
        usuario.UltimoAcceso = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var codigosPermisos = usuario.Rol.RolPermisos.Select(rp => rp.Permiso.Codigo).ToList();
        var sesion = new SesionUsuario(
            usuario.Id,
            usuario.NombreCompleto,
            usuario.NombreUsuario,
            usuario.RolId,
            usuario.Rol.Nombre,
            codigosPermisos
        );

        if (usuario.DebeCambiarPassword)
        {
            return LoginResult.RequiereCambioPassword(sesion);
        }

        return LoginResult.Exito(sesion);
    }

    public async Task<bool> CambiarPasswordObligatorioAsync(int usuarioId, string nuevaPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nuevaPassword) || nuevaPassword.Length < 6)
            throw new ArgumentException("La nueva contraseña debe tener al menos 6 caracteres.");

        var usuario = await _context.Usuarios.FindAsync(new object[] { usuarioId }, cancellationToken);
        if (usuario == null || !usuario.Activo)
            return false;

        usuario.PasswordHash = _hasher.HashPassword(nuevaPassword);
        usuario.DebeCambiarPassword = false;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CambiarPasswordVoluntarioAsync(int usuarioId, string passwordActual, string nuevaPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nuevaPassword) || nuevaPassword.Length < 6)
            throw new ArgumentException("La nueva contraseña debe tener al menos 6 caracteres.");

        var usuario = await _context.Usuarios.FindAsync(new object[] { usuarioId }, cancellationToken);
        if (usuario == null || !usuario.Activo)
            return false;

        if (!_hasher.VerifyPassword(passwordActual, usuario.PasswordHash))
            return false;

        usuario.PasswordHash = _hasher.HashPassword(nuevaPassword);
        usuario.DebeCambiarPassword = false;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ResetPasswordAsync(int adminId, int targetUsuarioId, string passwordTemporal, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(passwordTemporal) || passwordTemporal.Length < 6)
            throw new ArgumentException("La contraseña temporal debe tener al menos 6 caracteres.");

        var admin = await _context.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == adminId, cancellationToken);
        if (admin == null || !admin.Activo)
            return false;

        var targetUsuario = await _context.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == targetUsuarioId, cancellationToken);
        if (targetUsuario == null)
            return false;

        // Regla: Solo Super Admin puede resetear a otro Super Admin o al Admin
        if (targetUsuario.Rol.Nombre == Core.Entities.Rol.SuperAdmin && admin.Rol.Nombre != Core.Entities.Rol.SuperAdmin)
            throw new UnauthorizedAccessException("Solo el Super Admin puede resetear cuentas con rol Super Admin.");

        targetUsuario.PasswordHash = _hasher.HashPassword(passwordTemporal);
        targetUsuario.DebeCambiarPassword = true; // Forzar cambio en próximo login
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
