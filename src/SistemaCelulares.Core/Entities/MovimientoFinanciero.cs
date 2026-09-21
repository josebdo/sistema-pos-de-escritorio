namespace SistemaCelulares.Core.Entities;

public class MovimientoFinanciero
{
    public int Id { get; set; }

    public TipoMovimientoFinanciero Tipo { get; set; }

    public int CategoriaFinancieraId { get; set; }
    public CategoriaFinanciera CategoriaFinanciera { get; set; } = null!;

    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public string Descripcion { get; set; } = string.Empty;

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public string? NumeroComprobante { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
}
