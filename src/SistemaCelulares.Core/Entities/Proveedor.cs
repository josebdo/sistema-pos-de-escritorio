namespace SistemaCelulares.Core.Entities;

public class Proveedor
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    
    /// <summary>
    /// Registro Nacional de Contribuyentes (RNC de República Dominicana, 9 u 11 dígitos).
    /// </summary>
    public string? Rnc { get; set; }
    
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public string? Contacto { get; set; }

    /// <summary>
    /// Desactivación lógica: no se elimina físicamente si tiene compras asociadas.
    /// </summary>
    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? UltimaModificacion { get; set; }
}
