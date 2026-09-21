namespace SistemaCelulares.Core.Models;

public class BalanceNetoDto
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public decimal TotalVentas { get; set; }
    public decimal TotalOtrosIngresos { get; set; }
    public decimal TotalIngresosGlobales => TotalVentas + TotalOtrosIngresos;
    public decimal TotalGastosOperativos { get; set; }
    public decimal TotalComprasMercancia { get; set; }
    public decimal TotalGastos => TotalGastosOperativos + TotalComprasMercancia;
    public decimal BalanceNeto => TotalIngresosGlobales - TotalGastos;
    public int CantidadGastos { get; set; }
    public int CantidadOtrosIngresos { get; set; }
    public Dictionary<string, decimal> GastosPorCategoria { get; set; } = new();
    public Dictionary<string, decimal> IngresosPorCategoria { get; set; } = new();
}
