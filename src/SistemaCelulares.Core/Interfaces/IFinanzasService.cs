using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.Core.Interfaces;

public interface IFinanzasService
{
    Task<MovimientoFinanciero> RegistrarMovimientoAsync(
        TipoMovimientoFinanciero tipo,
        int categoriaId,
        decimal monto,
        DateTime fecha,
        string descripcion,
        int usuarioId,
        string? numeroComprobante = null,
        CancellationToken cancellationToken = default);

    Task<List<MovimientoFinanciero>> ObtenerMovimientosAsync(
        DateTime? desde = null,
        DateTime? hasta = null,
        TipoMovimientoFinanciero? tipo = null,
        int? categoriaId = null,
        CancellationToken cancellationToken = default);

    Task<BalanceNetoDto> ObtenerBalanceNetoPeriodoAsync(
        DateTime desde,
        DateTime hasta,
        CancellationToken cancellationToken = default);

    Task<List<CategoriaFinanciera>> ObtenerCategoriasFinancierasAsync(
        TipoMovimientoFinanciero? tipo = null,
        bool soloActivas = true,
        CancellationToken cancellationToken = default);

    Task<CategoriaFinanciera> CrearCategoriaFinancieraAsync(
        string nombre,
        TipoMovimientoFinanciero tipo,
        string? descripcion = null,
        CancellationToken cancellationToken = default);

    Task<bool> CambiarEstadoActivoCategoriaAsync(
        int id,
        bool activo,
        CancellationToken cancellationToken = default);
}
