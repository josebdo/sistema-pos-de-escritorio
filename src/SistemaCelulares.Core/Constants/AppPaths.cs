namespace SistemaCelulares.Core.Constants;

public static class AppPaths
{
    private static readonly string _appDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "SistemaCelulares"
    );

    static AppPaths()
    {
        try
        {
            if (!Directory.Exists(_appDataFolder))
            {
                Directory.CreateDirectory(_appDataFolder);
            }

            var backupsDir = Path.Combine(_appDataFolder, "Backups");
            if (!Directory.Exists(backupsDir))
            {
                Directory.CreateDirectory(backupsDir);
            }
        }
        catch
        {
            // Si hay restricción de permisos inusual, fallback a LocalApplicationData
            _appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SistemaCelulares"
            );
            if (!Directory.Exists(_appDataFolder))
            {
                Directory.CreateDirectory(_appDataFolder);
            }
        }
    }

    /// <summary>
    /// Carpeta raíz segura de datos mutables (por defecto C:\ProgramData\SistemaCelulares)
    /// </summary>
    public static string CarpetaDatos => _appDataFolder;

    /// <summary>
    /// Ruta del archivo de base de datos SQLite activo
    /// </summary>
    public static string RutaBaseDatos => Path.Combine(_appDataFolder, "sistema_celulares.db");

    /// <summary>
    /// Ruta del archivo de configuración de red y multicaja
    /// </summary>
    public static string RutaConfigRed => Path.Combine(_appDataFolder, "config_red.json");

    /// <summary>
    /// Carpeta para almacenar respaldos / copias de seguridad de la base de datos
    /// </summary>
    public static string CarpetaBackups => Path.Combine(_appDataFolder, "Backups");
}
