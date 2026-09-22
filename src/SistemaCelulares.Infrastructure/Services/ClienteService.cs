using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Infrastructure.Data;

namespace SistemaCelulares.Infrastructure.Services;

public class ClienteService : IClienteService
{
    private readonly AppDbContext _context;

    public ClienteService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Cliente>> ObtenerTodosAsync(bool soloActivos = true)
    {
        IQueryable<Cliente> query = _context.Clientes.AsNoTracking();
        if (soloActivos)
        {
            query = query.Where(c => c.Activo);
        }

        return await query.OrderBy(c => c.NombreCompleto).ToListAsync();
    }

    public async Task<Cliente?> ObtenerPorIdAsync(int id)
    {
        return await _context.Clientes
            .Include(c => c.Ventas)
            .Include(c => c.Reparaciones)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Cliente?> ObtenerPorTelefonoAsync(string telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono))
            return null;

        var telLimpio = telefono.Trim();
        return await _context.Clientes
            .FirstOrDefaultAsync(c => c.Telefono != null && c.Telefono.Trim() == telLimpio);
    }

    public async Task<IReadOnlyList<Cliente>> BuscarAsync(string termino)
    {
        if (string.IsNullOrWhiteSpace(termino))
            return await ObtenerTodosAsync(true);

        var term = termino.Trim().ToLower();
        return await _context.Clientes
            .AsNoTracking()
            .Where(c => c.NombreCompleto.ToLower().Contains(term) ||
                        (c.Telefono != null && c.Telefono.Contains(term)) ||
                        (c.RncOCedula != null && c.RncOCedula.Contains(term)) ||
                        (c.Email != null && c.Email.ToLower().Contains(term)))
            .OrderBy(c => c.NombreCompleto)
            .ToListAsync();
    }

    public async Task<Cliente> CrearAsync(Cliente cliente)
    {
        ArgumentNullException.ThrowIfNull(cliente);

        if (string.IsNullOrWhiteSpace(cliente.NombreCompleto))
            throw new ArgumentException("El nombre del cliente es obligatorio.");

        cliente.NombreCompleto = cliente.NombreCompleto.Trim();
        cliente.Telefono = cliente.Telefono?.Trim();
        cliente.RncOCedula = cliente.RncOCedula?.Trim();
        cliente.Email = cliente.Email?.Trim();
        cliente.Direccion = cliente.Direccion?.Trim();
        cliente.FechaCreacion = DateTime.UtcNow;
        cliente.Activo = true;

        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();
        return cliente;
    }

    public async Task ActualizarAsync(Cliente cliente)
    {
        ArgumentNullException.ThrowIfNull(cliente);

        var existente = await _context.Clientes.FindAsync(cliente.Id)
            ?? throw new InvalidOperationException($"El cliente con ID {cliente.Id} no existe.");

        if (string.IsNullOrWhiteSpace(cliente.NombreCompleto))
            throw new ArgumentException("El nombre del cliente es obligatorio.");

        existente.NombreCompleto = cliente.NombreCompleto.Trim();
        existente.Telefono = cliente.Telefono?.Trim();
        existente.RncOCedula = cliente.RncOCedula?.Trim();
        existente.Email = cliente.Email?.Trim();
        existente.Direccion = cliente.Direccion?.Trim();
        existente.EsFrecuente = cliente.EsFrecuente;
        existente.PorcentajeDescuento = cliente.PorcentajeDescuento;

        await _context.SaveChangesAsync();
    }

    public async Task DesactivarAsync(int id)
    {
        var cliente = await _context.Clientes.FindAsync(id)
            ?? throw new InvalidOperationException($"El cliente con ID {id} no existe.");

        cliente.Activo = false;
        await _context.SaveChangesAsync();
    }

    public async Task ActivarAsync(int id)
    {
        var cliente = await _context.Clientes.FindAsync(id)
            ?? throw new InvalidOperationException($"El cliente con ID {id} no existe.");

        cliente.Activo = true;
        await _context.SaveChangesAsync();
    }

    public async Task AsignarClienteFrecuenteAsync(int clienteId, bool esFrecuente, decimal porcentajeDescuento)
    {
        var cliente = await _context.Clientes.FindAsync(clienteId)
            ?? throw new InvalidOperationException($"El cliente con ID {clienteId} no existe.");

        cliente.EsFrecuente = esFrecuente;
        cliente.PorcentajeDescuento = esFrecuente ? Math.Max(0m, Math.Min(100m, porcentajeDescuento)) : 0m;
        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Venta>> ObtenerHistorialVentasAsync(int clienteId)
    {
        return await _context.Ventas
            .AsNoTracking()
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
            .Include(v => v.Pago)
            .Where(v => v.ClienteId == clienteId)
            .OrderByDescending(v => v.FechaVenta)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<OrdenReparacion>> ObtenerHistorialReparacionesAsync(int clienteId)
    {
        return await _context.OrdenesReparacion
            .AsNoTracking()
            .Include(r => r.Pago)
            .Where(r => r.ClienteId == clienteId)
            .OrderByDescending(r => r.FechaRecepcion)
            .ToListAsync();
    }
}
