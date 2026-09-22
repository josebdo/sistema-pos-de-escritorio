using SistemaCelulares.Core.Entities;

namespace SistemaCelulares.Core.Models;

public class ItemCarritoVentaDto
{
    public int ProductoId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public string NombreProducto { get; set; } = string.Empty;
    public int Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
    public decimal CostoUnitario { get; set; }
    public bool AplicaItbis { get; set; } = true;
    public decimal Itbis => AplicaItbis ? Math.Round(PrecioUnitario * Cantidad * 0.18m / 1.18m, 2) : 0m;
    public decimal SubtotalSinItbis => AplicaItbis ? (PrecioUnitario * Cantidad - Itbis) : (PrecioUnitario * Cantidad);
    public decimal TotalBruto => PrecioUnitario * Cantidad;
    public int StockDisponible { get; set; }
    public bool RequiereSerie { get; set; } = false;
    public int? UnidadProductoId { get; set; }
    public string? Imei { get; set; }
}

public class RegistrarVentaRequestDto
{
    public int UsuarioId { get; set; }
    public int TurnoId { get; set; }
    public TipoComprobanteFiscal TipoComprobante { get; set; } = TipoComprobanteFiscal.Consumo_B02;
    
    public int? ClienteId { get; set; }
    public string? NombreCliente { get; set; }
    public string? RncCliente { get; set; }
    
    public decimal Descuento { get; set; } = 0m;
    public string? Observaciones { get; set; }
    
    public List<ItemCarritoVentaDto> Items { get; set; } = new();
    public RegistrarPagoDto? PagoRequest { get; set; }
}

public class ResultadoVentaDto
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public int? VentaId { get; set; }
    public string? NumeroFactura { get; set; }
    public string? Ncf { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Itbis { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
    public decimal Vuelto { get; set; }
    public List<string> Errores { get; set; } = new();
}

public class TicketVentaDto
{
    public string NombreEmpresa { get; set; } = "TIENDA DE CELULARES Y ACCESORIOS";
    public string RncEmpresa { get; set; } = "131-12345-6";
    public string TelefonoEmpresa { get; set; } = "(809) 555-0199";
    public string DireccionEmpresa { get; set; } = "Santo Domingo, República Dominicana";
    
    public string NumeroFactura { get; set; } = string.Empty;
    public string? Ncf { get; set; }
    public string TipoComprobanteDescripcion { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string CajeroNombre { get; set; } = string.Empty;
    public int CajaId { get; set; } = 1;

    public string? ClienteNombre { get; set; }
    public string? ClienteRnc { get; set; }

    public List<ItemCarritoVentaDto> Items { get; set; } = new();

    public decimal Subtotal { get; set; }
    public decimal Itbis { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
    
    public decimal MontoEntregado { get; set; }
    public decimal MontoVuelto { get; set; }
    public string MetodosPagoTexto { get; set; } = string.Empty;
}
