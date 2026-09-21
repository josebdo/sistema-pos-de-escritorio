namespace SistemaCelulares.Core.Entities;

public class Categoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string PrefijoSku { get; set; } = "PROD";
    public bool Activo { get; set; } = true;

    // Relaciones
    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
