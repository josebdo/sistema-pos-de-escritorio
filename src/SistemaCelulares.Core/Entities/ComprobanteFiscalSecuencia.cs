namespace SistemaCelulares.Core.Entities;

public class ComprobanteFiscalSecuencia
{
    public int Id { get; set; }
    public TipoComprobanteFiscal Tipo { get; set; }
    public string Serie { get; set; } = "B";
    public string CodigoTipo { get; set; } = "02";
    public long SecuenciaActual { get; set; } = 1;
    public long SecuenciaHasta { get; set; } = 99999999;
    public DateTime FechaVencimiento { get; set; } = DateTime.UtcNow.AddYears(1);
    public bool Activo { get; set; } = true;
    public string? Descripcion { get; set; }

    public string FormatearNcf(long numero)
    {
        return $"{Serie}{CodigoTipo}{numero:D8}";
    }

    public string ObtenerNcfActual()
    {
        return FormatearNcf(SecuenciaActual);
    }
}
