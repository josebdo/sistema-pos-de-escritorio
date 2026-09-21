using SistemaCelulares.Core.Entities;

namespace SistemaCelulares.Core.Interfaces;

public interface IRolService
{
    Task<List<Rol>> ObtenerRolesAsync(bool soloActivos = true, CancellationToken cancellationToken = default);
    Task<Rol?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Permiso>> ObtenerTodosLosPermisosAsync(CancellationToken cancellationToken = default);
    Task<Rol> CrearRolPersonalizadoAsync(string nombre, string? descripcion, IEnumerable<string> codigosPermisos, CancellationToken cancellationToken = default);
    Task<bool> ActualizarRolAsync(int rolId, string nombre, string? descripcion, IEnumerable<string> codigosPermisos, CancellationToken cancellationToken = default);
    Task<bool> EliminarRolAsync(int rolId, CancellationToken cancellationToken = default);
}
