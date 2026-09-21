namespace SistemaCelulares.Core.Entities;

public class DetallePago
{
    public int Id { get; set; }
    
    public int PagoId { get; set; }
    public virtual Pago? Pago { get; set; }

    public MetodoPago MetodoPago { get; set; }
    public decimal Monto { get; set; }

    // Campos especificos para Efectivo
    public decimal MontoEntregado { get; set; }
    public decimal MontoVuelto { get; set; }

    // Campos especificos para Transferencia Bancaria
    public string? BancoDestino { get; set; }
    public string? NumeroReferencia { get; set; }
    public bool EsVerificado { get; set; }
    public DateTime? FechaVerificacion { get; set; }

    // Campos especificos para Tarjeta
    public SubtipoTarjeta? TipoTarjeta { get; set; }
    public string? NumeroAutorizacionPos { get; set; }
}
