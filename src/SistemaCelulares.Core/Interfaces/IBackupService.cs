namespace SistemaCelulares.Core.Interfaces;

public class InfoBaseDatosDto
{
    public string RutaArchivo { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
    public string TamanoFormateado { get; set; } = "0 KB";
    public DateTime UltimaModificacion { get; set; }
    public bool Existe { get; set; }
}

public interface IBackupService
{
    InfoBaseDatosDto ObtenerInfoBaseDatos();
    Task<(bool Exitoso, string Mensaje, string? RutaGenerada)> CrearBackupAsync(string? rutaDestinoPersonalizada = null);
    Task<(bool Exitoso, string Mensaje)> RestaurarBackupAsync(string rutaArchivoBackup);
    List<FileInfo> ListarBackupsExistentes();
}
