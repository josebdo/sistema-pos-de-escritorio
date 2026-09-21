namespace SistemaCelulares.Core.Entities;

public class Usuario
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefono { get; set; }

    public int RolId { get; set; }
    public Rol Rol { get; set; } = null!;

    public bool Activo { get; set; } = true;
    
    /// <summary>
    /// Si es true, la aplicación bloquea el acceso hasta que el usuario cambie su contraseña.
    /// Aplica a usuarios recién creados y tras un reseteo de contraseña por Super Admin / Admin.
    /// </summary>
    public bool DebeCambiarPassword { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? UltimoAcceso { get; set; }
}
