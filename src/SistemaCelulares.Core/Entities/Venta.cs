namespace SistemaCelulares.Core.Entities;

public class Venta
{
    public int Id { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public TipoComprobanteFiscal TipoComprobante { get; set; } = TipoComprobanteFiscal.Consumo_B02;
    public string? Ncf { get; set; }
    public DateTime FechaVenta { get; set; } = DateTime.UtcNow;

    public decimal Subtotal { get; set; }
    public decimal Itbis { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }

    public EstadoVenta Estado { get; set; } = EstadoVenta.Completada;

    public int UsuarioId { get; set; }
    public virtual Usuario? Usuario { get; set; }

    public int TurnoId { get; set; }
    public virtual Turno? Turno { get; set; }

    public int? ClienteId { get; set; }
    public virtual Cliente? Cliente { get; set; }

    public string? NombreClienteAnonimo { get; set; }
    public string? RncCliente { get; set; }

    public int? PagoId { get; set; }
    public virtual Pago? Pago { get; set; }

    public string? Observaciones { get; set; }

    public virtual ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
}
