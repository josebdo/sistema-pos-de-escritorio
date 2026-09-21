using SistemaCelulares.Core.Entities;

namespace SistemaCelulares.Core.Interfaces;

public interface IProveedorService
{
    Task<List<Proveedor>> ObtenerProveedoresAsync(bool soloActivos = true, string? busqueda = null, CancellationToken cancellationToken = default);
    Task<Proveedor?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Proveedor?> ObtenerPorRncAsync(string rnc, CancellationToken cancellationToken = default);
    Task<Proveedor> CrearProveedorAsync(
        string nombre,
        string? rnc = null,
        string? telefono = null,
        string? email = null,
        string? direccion = null,
        string? contacto = null,
        CancellationToken cancellationToken = default);

    Task<bool> ActualizarProveedorAsync(
        int id,
        string nombre,
        string? rnc = null,
        string? telefono = null,
        string? email = null,
        string? direccion = null,
        string? contacto = null,
        CancellationToken cancellationToken = default);

    Task<bool> CambiarEstadoActivoAsync(int id, bool activo, CancellationToken cancellationToken = default);
}
