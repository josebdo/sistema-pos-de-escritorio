namespace SistemaCelulares.Core.Models;

public class TicketReparacionDto
{
    public string NombreEmpresa { get; set; } = "Veyra POS — Tienda y Taller de Celulares";
    public string RncEmpresa { get; set; } = "131-12345-6";
    public string TelefonoEmpresa { get; set; } = "(809) 555-0199";
    public string DireccionEmpresa { get; set; } = "Santo Domingo, República Dominicana";

    public string NumeroOrden { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.Now;
    public string TecnicoEntrega { get; set; } = string.Empty;

    public string ClienteNombre { get; set; } = string.Empty;
    public string ClienteTelefono { get; set; } = string.Empty;
    public string? ClienteRnc { get; set; }

    public string EquipoMarcaModelo { get; set; } = string.Empty;
    public string ImeiOSerie { get; set; } = string.Empty;
    public string DescripcionProblema { get; set; } = string.Empty;
    public string? DiagnosticoSolucion { get; set; }

    public decimal MontoTotal { get; set; }
    public decimal MontoEntregado { get; set; }
    public decimal MontoVuelto { get; set; }
    public string MetodosPagoTexto { get; set; } = "Efectivo";
    public string EstadoOrden { get; set; } = "Entregada y Pagada";
}
