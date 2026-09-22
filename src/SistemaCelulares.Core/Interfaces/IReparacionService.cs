using SistemaCelulares.Core.Entities;

namespace SistemaCelulares.Core.Interfaces;

public interface IReparacionService
{
    Task<IReadOnlyList<OrdenReparacion>> ObtenerTodasAsync(EstadoReparacion? estado = null);
    Task<OrdenReparacion?> ObtenerPorIdAsync(int id);
    Task<OrdenReparacion?> ObtenerPorNumeroOrdenAsync(string numeroOrden);
    Task<IReadOnlyList<OrdenReparacion>> BuscarAsync(string termino);
    Task<IReadOnlyList<OrdenReparacion>> ObtenerPorClienteAsync(int clienteId);
    Task<OrdenReparacion> RegistrarRecepcionAsync(OrdenReparacion orden);
    Task ActualizarDiagnosticoAsync(int ordenId, string notasDiagnostico, decimal? precioFinal = null);
    Task CambiarEstadoAsync(int ordenId, EstadoReparacion nuevoEstado);
    Task<OrdenReparacion> CobrarYEntregarAsync(int ordenId, int usuarioEntregaId, Pago pago);
    Task CancelarOrdenAsync(int ordenId, string motivo);
    Task<string> GenerarSiguienteNumeroOrdenAsync();
}
