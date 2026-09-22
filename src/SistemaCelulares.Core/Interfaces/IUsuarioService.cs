using SistemaCelulares.Core.Entities;

namespace SistemaCelulares.Core.Interfaces;

public interface IUsuarioService
{
    Task<List<Usuario>> ObtenerTodosAsync(bool incluirInactivos = true, CancellationToken cancellationToken = default);
    Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken = default);
    Task<Usuario> CrearUsuarioAsync(string nombreCompleto, string nombreUsuario, string passwordTemporal, int rolId, string? email = null, string? telefono = null, CancellationToken cancellationToken = default);
    Task<bool> ActualizarUsuarioAsync(int id, string nombreCompleto, int rolId, string? email = null, string? telefono = null, string? nombreUsuario = null, CancellationToken cancellationToken = default);
    Task<bool> ActualizarMiPerfilAsync(int id, string nombreCompleto, CancellationToken cancellationToken = default);
    Task<bool> ResetearPasswordAsync(int id, string nuevaPassword, CancellationToken cancellationToken = default);
    Task<bool> CambiarEstadoActivoAsync(int id, bool activo, CancellationToken cancellationToken = default);
}
