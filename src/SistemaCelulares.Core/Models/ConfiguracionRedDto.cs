namespace SistemaCelulares.Core.Models;

public enum ModoOperacionCaja
{
    CajaUnicaLocal = 1,
    ServidorCentral = 2,
    CajaClienteLan = 3
}

public class ConfiguracionRedDto
{
    public ModoOperacionCaja ModoOperacion { get; set; } = ModoOperacionCaja.CajaUnicaLocal;
    public int CajaId { get; set; } = 1;
    public string NombreCaja { get; set; } = "Caja Principal (Caja 1)";
    public string ServidorIpOHost { get; set; } = "127.0.0.1";
    public int Puerto { get; set; } = 1433;
    public string BaseDatosNombre { get; set; } = "sistema_celulares";
    public string? UsuarioDb { get; set; }
    public string? PasswordDb { get; set; }
    public string? CadenaConexionPersonalizada { get; set; }
    public DateTime UltimaModificacion { get; set; } = DateTime.UtcNow;
}
