namespace SistemaCelulares.Core.Entities;

/// <summary>
/// Representa una orden de reparación o servicio técnico de un equipo celular recibido de un cliente.
/// </summary>
public class OrdenReparacion
{
    public int Id { get; set; }

    /// <summary>
    /// Código identificador correlativo único (ej. REP-0001) para la boleta de recepción.
    /// </summary>
    public string NumeroOrden { get; set; } = string.Empty;

    public int ClienteId { get; set; }
    public virtual Cliente Cliente { get; set; } = null!;

    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;

    /// <summary>
    /// IMEI o Número de serie del celular del cliente (texto libre).
    /// </summary>
    public string? ImeiOSerie { get; set; }

    public string DescripcionProblema { get; set; } = string.Empty;
    public string? NotasDiagnostico { get; set; }

    public decimal PrecioEstimado { get; set; }
    public decimal PrecioFinal { get; set; }

    public DateTime FechaRecepcion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaListaParaEntrega { get; set; }
    public DateTime? FechaEntrega { get; set; }

    public EstadoReparacion Estado { get; set; } = EstadoReparacion.EnReparacion;

    public int? PagoId { get; set; }
    public virtual Pago? Pago { get; set; }

    public int UsuarioRecepcionId { get; set; }
    public virtual Usuario? UsuarioRecepcion { get; set; }

    public int? UsuarioEntregaId { get; set; }
    public virtual Usuario? UsuarioEntrega { get; set; }
}
