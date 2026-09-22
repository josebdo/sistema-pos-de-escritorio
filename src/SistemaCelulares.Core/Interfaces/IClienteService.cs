using SistemaCelulares.Core.Entities;

namespace SistemaCelulares.Core.Interfaces;

public interface IClienteService
{
    Task<IReadOnlyList<Cliente>> ObtenerTodosAsync(bool soloActivos = true);
    Task<Cliente?> ObtenerPorIdAsync(int id);
    Task<Cliente?> ObtenerPorTelefonoAsync(string telefono);
    Task<IReadOnlyList<Cliente>> BuscarAsync(string termino);
    Task<Cliente> CrearAsync(Cliente cliente);
    Task ActualizarAsync(Cliente cliente);
    Task DesactivarAsync(int id);
    Task ActivarAsync(int id);
    Task AsignarClienteFrecuenteAsync(int clienteId, bool esFrecuente, decimal porcentajeDescuento);
    Task<IReadOnlyList<Venta>> ObtenerHistorialVentasAsync(int clienteId);
    Task<IReadOnlyList<OrdenReparacion>> ObtenerHistorialReparacionesAsync(int clienteId);
}
