using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.Core.Interfaces;

public interface IVentaService
{
    Task<ResultadoVentaDto> RegistrarVentaAsync(RegistrarVentaRequestDto request);
    Task<Venta?> ObtenerPorIdAsync(int ventaId);
    Task<List<Venta>> ObtenerHistorialAsync(DateTime? desde = null, DateTime? hasta = null, int? usuarioId = null, int? turnoId = null);
    Task<bool> AnularVentaAsync(int ventaId, string motivo, int usuarioId);
    Task<string?> GenerarSiguienteNcfAsync(TipoComprobanteFiscal tipo);
    Task<List<ComprobanteFiscalSecuencia>> ObtenerSecuenciasNcfAsync();
    Task GuardarSecuenciaNcfAsync(ComprobanteFiscalSecuencia secuencia);
    Task<TicketVentaDto> GenerarTicketVentaAsync(int ventaId);
}
