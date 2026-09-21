using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.Core.Interfaces;

public interface IPagoService
{
    CalculoVueltoDto CalcularVueltoEfectivo(decimal montoTotal, decimal montoEntregado);
    Task<ResultadoPagoDto> ProcesarPagoAsync(RegistrarPagoDto request);
    Task<bool> VerificarTransferenciaAsync(int detallePagoId);
    Task<Pago?> ObtenerPorIdAsync(int pagoId);
    Task<List<Pago>> ObtenerPagosPorTurnoAsync(int turnoId);
    Task<List<Pago>> ObtenerPagosPendientesVerificacionAsync();
}
