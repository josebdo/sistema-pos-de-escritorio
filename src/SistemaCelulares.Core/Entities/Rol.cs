namespace SistemaCelulares.Core.Entities;

public class Rol
{
    public const string SuperAdmin = "Super Admin";
    public const string Admin = "Admin";
    public const string Cajero = "Cajero";
    public const string Tecnico = "Tecnico";

    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    
    /// <summary>
    /// Los roles fijos (Super Admin, Admin, Cajero, Tecnico) no pueden eliminarse ni cambiar de nombre.
    /// </summary>
    public bool EsFijo { get; set; }
    public bool Activo { get; set; } = true;

    // Relaciones
    public ICollection<RolPermiso> RolPermisos { get; set; } = new List<RolPermiso>();
    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
