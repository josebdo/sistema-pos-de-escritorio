namespace SistemaCelulares.Core.Entities;

public class Cliente
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string? RncOCedula { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Venta> Ventas { get; set; } = new List<Venta>();
}
