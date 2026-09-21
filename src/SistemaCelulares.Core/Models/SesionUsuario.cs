using SistemaCelulares.Core.Entities;

namespace SistemaCelulares.Core.Models;

/// <summary>
/// Representa la sesión del usuario actualmente autenticado en el sistema.
/// </summary>
public class SesionUsuario
{
    public int UsuarioId { get; }
    public string NombreCompleto { get; }
    public string NombreUsuario { get; }
    public int RolId { get; }
    public string RolNombre { get; }
    public bool EsSuperAdmin => RolNombre == Rol.SuperAdmin;
    public bool EsAdmin => RolNombre == Rol.Admin || EsSuperAdmin;
    public HashSet<string> Permisos { get; }

    public SesionUsuario(int usuarioId, string nombreCompleto, string nombreUsuario, int rolId, string rolNombre, IEnumerable<string> permisos)
    {
        UsuarioId = usuarioId;
        NombreCompleto = nombreCompleto;
        NombreUsuario = nombreUsuario;
        RolId = rolId;
        RolNombre = rolNombre;
        Permisos = new HashSet<string>(permisos, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Valida si el usuario actual posee un permiso determinado.
    /// Super Admin tiene acceso total por definición.
    /// </summary>
    public bool TienePermiso(string codigoPermiso)
    {
        if (EsSuperAdmin) return true;
        return Permisos.Contains(codigoPermiso);
    }
}
