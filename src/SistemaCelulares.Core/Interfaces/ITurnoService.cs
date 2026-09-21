using SistemaCelulares.Core.Entities;

namespace SistemaCelulares.Core.Interfaces;

public interface ITurnoService
{
    Task<Turno?> ObtenerTurnoAbiertoAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<bool> TieneTurnoAbiertoAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<Turno> AbrirTurnoAsync(int usuarioId, decimal montoApertura, string? observaciones = null, CancellationToken cancellationToken = default);
    Task<Turno> CerrarTurnoAsync(int turnoId, int usuarioCierreId, decimal montoCierre, string? observaciones = null, CancellationToken cancellationToken = default);
    Task<decimal> CalcularEfectivoEsperadoAsync(int turnoId, CancellationToken cancellationToken = default);
    Task<List<Turno>> ObtenerHistorialTurnosAsync(DateTime? desde = null, DateTime? hasta = null, int? usuarioId = null, CancellationToken cancellationToken = default);
    Task<Turno?> ObtenerPorIdAsync(int turnoId, CancellationToken cancellationToken = default);
}
