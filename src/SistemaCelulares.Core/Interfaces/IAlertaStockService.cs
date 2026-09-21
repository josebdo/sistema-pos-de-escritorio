using SistemaCelulares.Core.Models;

namespace SistemaCelulares.Core.Interfaces;

public interface IAlertaStockService
{
    Task<List<AlertaStockDto>> ObtenerAlertasStockAsync(CancellationToken cancellationToken = default);
    Task<int> ContarProductosCriticosAsync(CancellationToken cancellationToken = default);
}
