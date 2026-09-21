using SistemaCelulares.Core.Entities;

namespace SistemaCelulares.Core.Interfaces;

public interface ICategoriaService
{
    Task<List<Categoria>> ObtenerCategoriasAsync(bool soloActivas = true, CancellationToken cancellationToken = default);
    Task<Categoria?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Categoria> CrearCategoriaAsync(string nombre, string? descripcion, string? prefijoSku = null, CancellationToken cancellationToken = default);
    Task<bool> ActualizarCategoriaAsync(int id, string nombre, string? descripcion, string? prefijoSku = null, CancellationToken cancellationToken = default);
    Task<bool> CambiarEstadoActivoAsync(int id, bool activo, CancellationToken cancellationToken = default);
}
