namespace SistemaCelulares.Core.Entities;

public class Compra
{
    public int Id { get; set; }

    public int ProveedorId { get; set; }
    public Proveedor Proveedor { get; set; } = null!;

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public DateTime FechaCompra { get; set; } = DateTime.UtcNow;
    public string? NumeroFactura { get; set; }

    public decimal Total { get; set; }
    public string? Observaciones { get; set; }

    // Relación con los renglones / ítems comprados
    public ICollection<DetalleCompra> Detalles { get; set; } = new List<DetalleCompra>();
}
