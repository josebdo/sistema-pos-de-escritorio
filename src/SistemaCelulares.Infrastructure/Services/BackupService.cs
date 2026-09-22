using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Constants;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Infrastructure.Data;

namespace SistemaCelulares.Infrastructure.Services;

public class BackupService : IBackupService
{
    private readonly AppDbContext _context;

    public BackupService(AppDbContext context)
    {
        _context = context;
    }

    public InfoBaseDatosDto ObtenerInfoBaseDatos()
    {
        var rutaDb = AppPaths.RutaBaseDatos;
        var info = new InfoBaseDatosDto { RutaArchivo = rutaDb };

        if (File.Exists(rutaDb))
        {
            var fileInfo = new FileInfo(rutaDb);
            info.Existe = true;
            info.TamanoBytes = fileInfo.Length;
            info.TamanoFormateado = FormatearTamano(fileInfo.Length);
            info.UltimaModificacion = fileInfo.LastWriteTime;
        }

        return info;
    }

    public async Task<(bool Exitoso, string Mensaje, string? RutaGenerada)> CrearBackupAsync(string? rutaDestinoPersonalizada = null)
    {
        try
        {
            var rutaDb = AppPaths.RutaBaseDatos;
            if (!File.Exists(rutaDb))
            {
                return (false, "El archivo de base de datos no existe aún.", null);
            }

            string rutaFinal;
            if (string.IsNullOrWhiteSpace(rutaDestinoPersonalizada))
            {
                var nombreArchivo = $"backup_sistemacelulares_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                rutaFinal = Path.Combine(AppPaths.CarpetaBackups, nombreArchivo);
            }
            else
            {
                rutaFinal = rutaDestinoPersonalizada;
            }

            // Asegurar directorio destino
            var dir = Path.GetDirectoryName(rutaFinal);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Forzar checkpoint de WAL en SQLite si está activo
            try
            {
                await _context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(FULL);");
            }
            catch
            {
                // Continuar si no está en modo WAL
            }

            // Intentar backup nativo SQLite (VACUUM INTO)
            try
            {
                if (File.Exists(rutaFinal))
                {
                    File.Delete(rutaFinal);
                }

                // Usar VACUUM INTO con parámetro interpolado seguro
                await _context.Database.ExecuteSqlInterpolatedAsync($"VACUUM INTO {rutaFinal};");
            }
            catch
            {
                // Fallback: Copia de archivo segura
                SqliteConnection.ClearAllPools();
                File.Copy(rutaDb, rutaFinal, overwrite: true);
            }

            return (true, $"Copia de seguridad creada con éxito en:\n{rutaFinal}", rutaFinal);
        }
        catch (Exception ex)
        {
            return (false, $"Error al generar la copia de seguridad: {ex.Message}", null);
        }
    }

    public async Task<(bool Exitoso, string Mensaje)> RestaurarBackupAsync(string rutaArchivoBackup)
    {
        try
        {
            if (!File.Exists(rutaArchivoBackup))
            {
                return (false, "El archivo de respaldo seleccionado no existe.");
            }

            // Validar que sea un archivo SQLite (los primeros 16 bytes contienen "SQLite format 3")
            using (var fs = new FileStream(rutaArchivoBackup, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                byte[] header = new byte[16];
                int bytesRead = await fs.ReadAsync(header, 0, 16);
                string headerStr = System.Text.Encoding.ASCII.GetString(header, 0, bytesRead);
                if (!headerStr.StartsWith("SQLite format 3"))
                {
                    return (false, "El archivo seleccionado no es un respaldo SQLite válido.");
                }
            }

            var rutaDb = AppPaths.RutaBaseDatos;

            // 1. Crear un backup preventivo de la base de datos actual antes de sobrescribirla
            if (File.Exists(rutaDb))
            {
                var autoBackupName = $"pre_restauracion_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                var preRuta = Path.Combine(AppPaths.CarpetaBackups, autoBackupName);
                try
                {
                    File.Copy(rutaDb, preRuta, overwrite: true);
                }
                catch { }
            }

            // 2. Liberar pools de conexiones activas
            SqliteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // 3. Sobrescribir archivo de base de datos
            File.Copy(rutaArchivoBackup, rutaDb, overwrite: true);

            // 4. Limpiar archivos -wal y -shm si existían
            var walFile = rutaDb + "-wal";
            var shmFile = rutaDb + "-shm";
            if (File.Exists(walFile)) File.Delete(walFile);
            if (File.Exists(shmFile)) File.Delete(shmFile);

            return (true, "Base de datos restaurada correctamente. Se recomienda reiniciar la aplicación para aplicar todos los cambios.");
        }
        catch (Exception ex)
        {
            return (false, $"Error al restaurar la copia de seguridad: {ex.Message}");
        }
    }

    public List<FileInfo> ListarBackupsExistentes()
    {
        try
        {
            var dir = new DirectoryInfo(AppPaths.CarpetaBackups);
            if (!dir.Exists) return new List<FileInfo>();

            return dir.GetFiles("*.db")
                      .OrderByDescending(f => f.LastWriteTime)
                      .ToList();
        }
        catch
        {
            return new List<FileInfo>();
        }
    }

    private static string FormatearTamano(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }
}
