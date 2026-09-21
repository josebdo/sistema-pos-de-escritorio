using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Infrastructure.Data;

namespace SistemaCelulares.Infrastructure.Services;

public class TurnoService : ITurnoService
{
    private readonly AppDbContext _context;

    public TurnoService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Turno?> ObtenerTurnoAbiertoAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        return await _context.Turnos
            .Include(t => t.UsuarioApertura)
            .Include(t => t.UsuarioCierre)
            .FirstOrDefaultAsync(t => t.UsuarioAperturaId == usuarioId && t.Estado == TurnoEstado.Abierto, cancellationToken);
    }

    public async Task<bool> TieneTurnoAbiertoAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        return await _context.Turnos
            .AnyAsync(t => t.UsuarioAperturaId == usuarioId && t.Estado == TurnoEstado.Abierto, cancellationToken);
    }

    public async Task<Turno> AbrirTurnoAsync(int usuarioId, decimal montoApertura, string? observaciones = null, CancellationToken cancellationToken = default)
    {
        if (montoApertura < 0)
            throw new ArgumentException("El monto de apertura no puede ser negativo.", nameof(montoApertura));

        var usuario = await _context.Usuarios.FindAsync(new object[] { usuarioId }, cancellationToken);
        if (usuario == null || !usuario.Activo)
            throw new InvalidOperationException("El usuario no es válido o está inactivo.");

        var yaTieneAbierto = await _context.Turnos.AnyAsync(t => t.UsuarioAperturaId == usuarioId && t.Estado == TurnoEstado.Abierto, cancellationToken);
        if (yaTieneAbierto)
            throw new InvalidOperationException($"El usuario '{usuario.NombreUsuario}' ya tiene un turno de caja abierto.");

        var turno = new Turno
        {
            UsuarioAperturaId = usuarioId,
            FechaApertura = DateTime.UtcNow,
            MontoApertura = montoApertura,
            TotalVentasEfectivo = 0m,
            MontoEsperado = montoApertura,
            Estado = TurnoEstado.Abierto,
            ObservacionesApertura = observaciones?.Trim(),
            CajaId = 1
        };

        _context.Turnos.Add(turno);
        await _context.SaveChangesAsync(cancellationToken);

        return turno;
    }

    public async Task<decimal> CalcularEfectivoEsperadoAsync(int turnoId, CancellationToken cancellationToken = default)
    {
        var turno = await _context.Turnos.FindAsync(new object[] { turnoId }, cancellationToken);
        if (turno == null)
            throw new InvalidOperationException("El turno especificado no existe.");

        return turno.MontoApertura + turno.TotalVentasEfectivo;
    }

    public async Task<Turno> CerrarTurnoAsync(int turnoId, int usuarioCierreId, decimal montoCierre, string? observaciones = null, CancellationToken cancellationToken = default)
    {
        if (montoCierre < 0)
            throw new ArgumentException("El monto de cierre no puede ser negativo.", nameof(montoCierre));

        var turno = await _context.Turnos
            .Include(t => t.UsuarioApertura)
            .FirstOrDefaultAsync(t => t.Id == turnoId, cancellationToken);

        if (turno == null)
            throw new InvalidOperationException("El turno no existe.");

        if (turno.Estado == TurnoEstado.Cerrado)
            throw new InvalidOperationException("Este turno ya fue cerrado anteriormente.");

        var usuarioCierre = await _context.Usuarios.FindAsync(new object[] { usuarioCierreId }, cancellationToken);
        if (usuarioCierre == null || !usuarioCierre.Activo)
            throw new InvalidOperationException("El usuario de cierre no es válido o está inactivo.");

        var montoEsperado = turno.MontoApertura + turno.TotalVentasEfectivo;
        var diferencia = montoCierre - montoEsperado;

        turno.UsuarioCierreId = usuarioCierreId;
        turno.FechaCierre = DateTime.UtcNow;
        turno.MontoEsperado = montoEsperado;
        turno.MontoCierre = montoCierre;
        turno.Diferencia = diferencia;
        turno.Estado = TurnoEstado.Cerrado;
        turno.ObservacionesCierre = observaciones?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return turno;
    }

    public async Task<List<Turno>> ObtenerHistorialTurnosAsync(DateTime? desde = null, DateTime? hasta = null, int? usuarioId = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Turno> query = _context.Turnos
            .Include(t => t.UsuarioApertura)
            .Include(t => t.UsuarioCierre);

        if (desde.HasValue)
        {
            var inicioDia = desde.Value.Date;
            query = query.Where(t => t.FechaApertura >= inicioDia);
        }

        if (hasta.HasValue)
        {
            var finDia = hasta.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(t => t.FechaApertura <= finDia);
        }

        if (usuarioId.HasValue && usuarioId.Value > 0)
        {
            query = query.Where(t => t.UsuarioAperturaId == usuarioId.Value || t.UsuarioCierreId == usuarioId.Value);
        }

        return await query.OrderByDescending(t => t.FechaApertura).ToListAsync(cancellationToken);
    }

    public async Task<Turno?> ObtenerPorIdAsync(int turnoId, CancellationToken cancellationToken = default)
    {
        return await _context.Turnos
            .Include(t => t.UsuarioApertura)
            .Include(t => t.UsuarioCierre)
            .FirstOrDefaultAsync(t => t.Id == turnoId, cancellationToken);
    }
}
