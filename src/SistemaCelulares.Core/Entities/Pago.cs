namespace SistemaCelulares.Core.Entities;

public class Pago
{
    public int Id { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal MontoPagado { get; set; }
    public decimal MontoVuelto { get; set; }
    public MetodoPago MetodoPrincipal { get; set; }
    public EstadoPago Estado { get; set; } = EstadoPago.Completado;
    public DateTime FechaPago { get; set; } = DateTime.UtcNow;
    
    public int UsuarioId { get; set; }
    public virtual Usuario? Usuario { get; set; }

    public int? TurnoId { get; set; }
    public virtual Turno? Turno { get; set; }

    public string? Notas { get; set; }

    public virtual ICollection<DetallePago> Detalles { get; set; } = new List<DetallePago>();
}
