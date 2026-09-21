using SistemaCelulares.Core.Entities;

namespace SistemaCelulares.Core.Models;

public class CalculoVueltoDto
{
    public decimal MontoTotal { get; set; }
    public decimal MontoEntregado { get; set; }
    public decimal Vuelto { get; set; }
    public bool EsSuficiente { get; set; }
    public decimal MontoFaltante { get; set; }
}

public class DetallePagoRequestDto
{
    public MetodoPago Metodo { get; set; }
    public decimal Monto { get; set; }

    // Para Efectivo
    public decimal MontoEntregado { get; set; }

    // Para Transferencia Bancaria
    public string? BancoDestino { get; set; }
    public string? NumeroReferencia { get; set; }
    public bool EsVerificado { get; set; }

    // Para Tarjeta
    public SubtipoTarjeta? TipoTarjeta { get; set; }
    public string? NumeroAutorizacionPos { get; set; }
}

public class RegistrarPagoDto
{
    public decimal MontoTotal { get; set; }
    public int UsuarioId { get; set; }
    public int? TurnoId { get; set; }
    public string? Notas { get; set; }
    public List<DetallePagoRequestDto> Metodos { get; set; } = new();
}

public class ResultadoPagoDto
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public int? PagoId { get; set; }
    public EstadoPago Estado { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal MontoPagado { get; set; }
    public decimal MontoVuelto { get; set; }
    public List<string> Errores { get; set; } = new();
}
