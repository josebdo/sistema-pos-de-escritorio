using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Infrastructure.Data;

namespace SistemaCelulares.Infrastructure.Services;

public class ReparacionService : IReparacionService
{
    private readonly AppDbContext _context;

    public ReparacionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<OrdenReparacion>> ObtenerTodasAsync(EstadoReparacion? estado = null)
    {
        IQueryable<OrdenReparacion> query = _context.OrdenesReparacion
            .AsNoTracking()
            .Include(r => r.Cliente)
            .Include(r => r.UsuarioRecepcion)
            .Include(r => r.UsuarioEntrega)
            .Include(r => r.Pago)
                .ThenInclude(p => p!.Detalles);

        if (estado.HasValue)
        {
            query = query.Where(r => r.Estado == estado.Value);
        }

        return await query.OrderByDescending(r => r.FechaRecepcion).ToListAsync();
    }

    public async Task<OrdenReparacion?> ObtenerPorIdAsync(int id)
    {
        return await _context.OrdenesReparacion
            .Include(r => r.Cliente)
            .Include(r => r.UsuarioRecepcion)
            .Include(r => r.UsuarioEntrega)
            .Include(r => r.Pago)
                .ThenInclude(p => p!.Detalles)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<OrdenReparacion?> ObtenerPorNumeroOrdenAsync(string numeroOrden)
    {
        if (string.IsNullOrWhiteSpace(numeroOrden)) return null;

        var q = numeroOrden.Trim().ToLower();
        return await _context.OrdenesReparacion
            .Include(r => r.Cliente)
            .Include(r => r.UsuarioRecepcion)
            .Include(r => r.UsuarioEntrega)
            .Include(r => r.Pago)
                .ThenInclude(p => p!.Detalles)
            .FirstOrDefaultAsync(r => r.NumeroOrden.ToLower() == q);
    }

    public async Task<IReadOnlyList<OrdenReparacion>> BuscarAsync(string termino)
    {
        if (string.IsNullOrWhiteSpace(termino))
            return await ObtenerTodasAsync();

        var q = termino.Trim().ToLower();
        return await _context.OrdenesReparacion
            .AsNoTracking()
            .Include(r => r.Cliente)
            .Include(r => r.UsuarioRecepcion)
            .Include(r => r.UsuarioEntrega)
            .Include(r => r.Pago)
            .Where(r => r.NumeroOrden.ToLower().Contains(q) ||
                        r.Cliente.NombreCompleto.ToLower().Contains(q) ||
                        (r.Cliente.Telefono != null && r.Cliente.Telefono.Contains(q)) ||
                        r.Marca.ToLower().Contains(q) ||
                        r.Modelo.ToLower().Contains(q) ||
                        (r.ImeiOSerie != null && r.ImeiOSerie.ToLower().Contains(q)) ||
                        r.DescripcionProblema.ToLower().Contains(q))
            .OrderByDescending(r => r.FechaRecepcion)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<OrdenReparacion>> ObtenerPorClienteAsync(int clienteId)
    {
        return await _context.OrdenesReparacion
            .AsNoTracking()
            .Include(r => r.UsuarioRecepcion)
            .Include(r => r.UsuarioEntrega)
            .Include(r => r.Pago)
            .Where(r => r.ClienteId == clienteId)
            .OrderByDescending(r => r.FechaRecepcion)
            .ToListAsync();
    }

    public async Task<string> GenerarSiguienteNumeroOrdenAsync()
    {
        int total = await _context.OrdenesReparacion.CountAsync();
        string nuevoNumero = $"REP-{total + 1:D4}";

        while (await _context.OrdenesReparacion.AnyAsync(r => r.NumeroOrden.ToLower() == nuevoNumero.ToLower()))
        {
            total++;
            nuevoNumero = $"REP-{total + 1:D4}";
        }

        return nuevoNumero;
    }

    public async Task<OrdenReparacion> RegistrarRecepcionAsync(OrdenReparacion orden)
    {
        ArgumentNullException.ThrowIfNull(orden);

        if (string.IsNullOrWhiteSpace(orden.Marca))
            throw new ArgumentException("La marca del equipo es obligatoria.");

        if (string.IsNullOrWhiteSpace(orden.Modelo))
            throw new ArgumentException("El modelo del equipo es obligatorio.");

        if (string.IsNullOrWhiteSpace(orden.DescripcionProblema))
            throw new ArgumentException("La descripción del problema o falla es obligatoria.");

        if (string.IsNullOrWhiteSpace(orden.ImeiOSerie))
            throw new ArgumentException("El IMEI o número de serie del equipo es obligatorio.");

        var cliente = await _context.Clientes.FindAsync(orden.ClienteId)
            ?? throw new InvalidOperationException($"El cliente con ID {orden.ClienteId} no existe.");

        if (string.IsNullOrWhiteSpace(orden.NumeroOrden))
        {
            orden.NumeroOrden = await GenerarSiguienteNumeroOrdenAsync();
        }

        orden.FechaRecepcion = DateTime.UtcNow;
        orden.Estado = EstadoReparacion.EnReparacion;
        if (orden.PrecioFinal <= 0 && orden.PrecioEstimado > 0)
        {
            orden.PrecioFinal = orden.PrecioEstimado;
        }

        _context.OrdenesReparacion.Add(orden);
        await _context.SaveChangesAsync();
        return orden;
    }

    public async Task ActualizarDiagnosticoAsync(int ordenId, string notasDiagnostico, decimal? precioFinal = null)
    {
        var orden = await _context.OrdenesReparacion.FindAsync(ordenId)
            ?? throw new InvalidOperationException($"La orden de reparación con ID {ordenId} no existe.");

        orden.NotasDiagnostico = notasDiagnostico?.Trim();
        if (precioFinal.HasValue && precioFinal.Value >= 0)
        {
            orden.PrecioFinal = precioFinal.Value;
        }

        await _context.SaveChangesAsync();
    }

    public async Task CambiarEstadoAsync(int ordenId, EstadoReparacion nuevoEstado)
    {
        var orden = await _context.OrdenesReparacion.FindAsync(ordenId)
            ?? throw new InvalidOperationException($"La orden de reparación con ID {ordenId} no existe.");

        orden.Estado = nuevoEstado;

        if (nuevoEstado == EstadoReparacion.ListaParaEntrega)
        {
            orden.FechaListaParaEntrega = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<OrdenReparacion> CobrarYEntregarAsync(int ordenId, int usuarioEntregaId, Pago pago)
    {
        ArgumentNullException.ThrowIfNull(pago);

        var orden = await _context.OrdenesReparacion.FindAsync(ordenId)
            ?? throw new InvalidOperationException($"La orden de reparación con ID {ordenId} no existe.");

        if (orden.Estado == EstadoReparacion.Entregada)
            throw new InvalidOperationException("La orden de reparación ya fue entregada anteriormente.");

        var usuarioEntrega = await _context.Usuarios.FindAsync(usuarioEntregaId)
            ?? throw new InvalidOperationException($"El usuario con ID {usuarioEntregaId} no existe.");

        orden.Pago = pago;
        orden.UsuarioEntregaId = usuarioEntregaId;
        orden.FechaEntrega = DateTime.UtcNow;
        orden.Estado = EstadoReparacion.Entregada;

        await _context.SaveChangesAsync();
        return orden;
    }

    public async Task CancelarOrdenAsync(int ordenId, string motivo)
    {
        var orden = await _context.OrdenesReparacion.FindAsync(ordenId)
            ?? throw new InvalidOperationException($"La orden de reparación con ID {ordenId} no existe.");

        orden.Estado = EstadoReparacion.Cancelada;
        orden.NotasDiagnostico = $"{orden.NotasDiagnostico} | CANCELADA: {motivo}".TrimStart(' ', '|');
        await _context.SaveChangesAsync();
    }
}
