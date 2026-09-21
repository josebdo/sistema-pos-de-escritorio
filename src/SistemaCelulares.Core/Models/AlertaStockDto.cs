namespace SistemaCelulares.Core.Models;

public enum NivelCriticidadStock
{
    Agotado,  // Stock == 0
    Critico,  // Stock < CantidadMinima
    Bajo      // Stock == CantidadMinima
}

public class AlertaStockDto
{
    public int ProductoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string CategoriaNombre { get; set; } = string.Empty;
    public int StockActual { get; set; }
    public int CantidadMinima { get; set; }
    public int UnidadesFaltantes => Math.Max(0, CantidadMinima - StockActual);
    public decimal PrecioCosto { get; set; }
    public decimal InversionReposicion => UnidadesFaltantes * PrecioCosto;
    public NivelCriticidadStock Criticidad { get; set; }
}
