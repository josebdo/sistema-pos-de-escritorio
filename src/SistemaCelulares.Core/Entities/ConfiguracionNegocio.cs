namespace SistemaCelulares.Core.Entities;

public class ConfiguracionNegocio
{
    public int Id { get; set; } = 1;
    public string NombreEmpresa { get; set; } = "Mi Tienda de Celulares";
    public string RncCedula { get; set; } = "000-0000000-0";
    public string Telefono { get; set; } = "809-000-0000";
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string Direccion { get; set; } = "Calle Principal #1";
    public string? Ciudad { get; set; } = "Santo Domingo, RD";
    public string MensajePieFactura { get; set; } = "¡Gracias por su compra! Garantía de 30 días con su factura original.";
    public string MensajeGarantiaReparacion { get; set; } = "Equipos no retirados en un plazo de 30 días pasarán a disposición del taller.";
    public string MonedaSimbolo { get; set; } = "RD$";
    public decimal ItbisPorcentaje { get; set; } = 18.00m;
    public DateTime UltimaModificacion { get; set; } = DateTime.UtcNow;
}
