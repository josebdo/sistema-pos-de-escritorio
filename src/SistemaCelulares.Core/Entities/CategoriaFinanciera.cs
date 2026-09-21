namespace SistemaCelulares.Core.Entities;

public enum TipoMovimientoFinanciero
{
    Gasto = 1,
    Ingreso = 2
}

public class CategoriaFinanciera
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoMovimientoFinanciero Tipo { get; set; }
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;

    // Relaciones
    public ICollection<MovimientoFinanciero> Movimientos { get; set; } = new List<MovimientoFinanciero>();
}
