using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;
using SistemaCelulares.Infrastructure.Data;

namespace SistemaCelulares.Infrastructure.Services;

public class PagoService : IPagoService
{
    private readonly AppDbContext _context;

    public PagoService(AppDbContext context)
    {
        _context = context;
    }

    public CalculoVueltoDto CalcularVueltoEfectivo(decimal montoTotal, decimal montoEntregado)
    {
        if (montoTotal < 0) montoTotal = 0;
        if (montoEntregado < 0) montoEntregado = 0;

        if (montoEntregado >= montoTotal)
        {
            return new CalculoVueltoDto
            {
                MontoTotal = montoTotal,
                MontoEntregado = montoEntregado,
                Vuelto = montoEntregado - montoTotal,
                EsSuficiente = true,
                MontoFaltante = 0
            };
        }
        else
        {
            return new CalculoVueltoDto
            {
                MontoTotal = montoTotal,
                MontoEntregado = montoEntregado,
                Vuelto = 0,
                EsSuficiente = false,
                MontoFaltante = montoTotal - montoEntregado
            };
        }
    }

    public async Task<ResultadoPagoDto> ProcesarPagoAsync(RegistrarPagoDto request)
    {
        var errores = new List<string>();

        if (request.MontoTotal <= 0)
        {
            errores.Add("El monto total a pagar debe ser mayor a cero.");
        }

        if (request.Metodos == null || request.Metodos.Count == 0)
        {
            errores.Add("Debe especificar al menos un método de pago.");
            return new ResultadoPagoDto { Exitoso = false, Mensaje = "No se especificaron métodos de pago.", Errores = errores };
        }

        decimal sumaMontos = request.Metodos.Sum(m => m.Monto);
        if (Math.Abs(sumaMontos - request.MontoTotal) > 0.01m)
        {
            errores.Add($"La suma de los métodos de pago (RD${sumaMontos:N2}) no coincide con el total a pagar (RD${request.MontoTotal:N2}).");
        }

        bool tieneTransferenciaPendiente = false;
        decimal totalEntregadoGlobal = 0;
        decimal totalVueltoGlobal = 0;
        decimal totalEfectivoEfectivo = 0;

        foreach (var metodo in request.Metodos)
        {
            if (metodo.Monto <= 0)
            {
                errores.Add($"El monto para el método {metodo.Metodo} debe ser mayor a cero.");
            }

            if (metodo.Metodo == MetodoPago.Efectivo)
            {
                if (metodo.MontoEntregado < metodo.Monto)
                {
                    errores.Add($"El dinero entregado en efectivo (RD${metodo.MontoEntregado:N2}) es menor al monto a cubrir (RD${metodo.Monto:N2}).");
                }
                else
                {
                    decimal vuelto = metodo.MontoEntregado - metodo.Monto;
                    totalEntregadoGlobal += metodo.MontoEntregado;
                    totalVueltoGlobal += vuelto;
                    totalEfectivoEfectivo += metodo.Monto;
                }
            }
            else
            {
                totalEntregadoGlobal += metodo.Monto;
            }

            if (metodo.Metodo == MetodoPago.Transferencia)
            {
                if (!metodo.EsVerificado)
                {
                    tieneTransferenciaPendiente = true;
                }
            }

            if (metodo.Metodo == MetodoPago.Tarjeta)
            {
                if (!metodo.TipoTarjeta.HasValue)
                {
                    errores.Add("Debe indicar si el pago con tarjeta es Débito o Crédito.");
                }
            }
        }

        if (errores.Count > 0)
        {
            return new ResultadoPagoDto
            {
                Exitoso = false,
                Mensaje = "Existen errores de validación en la transacción de pago.",
                Errores = errores
            };
        }

        var estadoFinal = tieneTransferenciaPendiente 
            ? EstadoPago.PendienteVerificacion 
            : EstadoPago.Completado;

        MetodoPago metodoPrincipal = request.Metodos.Count == 1 
            ? request.Metodos[0].Metodo 
            : MetodoPago.Mixto;

        var nuevoPago = new Pago
        {
            MontoTotal = request.MontoTotal,
            MontoPagado = totalEntregadoGlobal,
            MontoVuelto = totalVueltoGlobal,
            MetodoPrincipal = metodoPrincipal,
            Estado = estadoFinal,
            FechaPago = DateTime.UtcNow,
            UsuarioId = request.UsuarioId,
            TurnoId = request.TurnoId,
            Notas = request.Notas
        };

        foreach (var m in request.Metodos)
        {
            decimal montoEntregado = m.Metodo == MetodoPago.Efectivo ? m.MontoEntregado : m.Monto;
            decimal montoVuelto = m.Metodo == MetodoPago.Efectivo ? (m.MontoEntregado - m.Monto) : 0;

            nuevoPago.Detalles.Add(new DetallePago
            {
                MetodoPago = m.Metodo,
                Monto = m.Monto,
                MontoEntregado = montoEntregado,
                MontoVuelto = montoVuelto,
                BancoDestino = m.BancoDestino,
                NumeroReferencia = m.NumeroReferencia,
                EsVerificado = m.Metodo == MetodoPago.Transferencia ? m.EsVerificado : true,
                FechaVerificacion = (m.Metodo == MetodoPago.Transferencia && m.EsVerificado) ? DateTime.UtcNow : null,
                TipoTarjeta = m.TipoTarjeta,
                NumeroAutorizacionPos = m.NumeroAutorizacionPos
            });
        }

        _context.Pagos.Add(nuevoPago);

        // Si hay turno y hubo cobro en efectivo y el pago fue completado
        if (request.TurnoId.HasValue && totalEfectivoEfectivo > 0 && estadoFinal == EstadoPago.Completado)
        {
            var turno = await _context.Turnos.FindAsync(request.TurnoId.Value);
            if (turno != null && turno.Estado == TurnoEstado.Abierto)
            {
                turno.TotalVentasEfectivo += totalEfectivoEfectivo;
                turno.MontoEsperado = turno.MontoApertura + turno.TotalVentasEfectivo;
            }
        }

        await _context.SaveChangesAsync();

        return new ResultadoPagoDto
        {
            Exitoso = true,
            Mensaje = estadoFinal == EstadoPago.PendienteVerificacion
                ? "Pago registrado. Pendiente de verificación por transferencia bancaria."
                : "Pago procesado exitosamente.",
            PagoId = nuevoPago.Id,
            Estado = estadoFinal,
            MontoTotal = nuevoPago.MontoTotal,
            MontoPagado = nuevoPago.MontoPagado,
            MontoVuelto = nuevoPago.MontoVuelto
        };
    }

    public async Task<bool> VerificarTransferenciaAsync(int detallePagoId)
    {
        var detalle = await _context.DetallePagos
            .Include(d => d.Pago)
            .ThenInclude(p => p!.Detalles)
            .FirstOrDefaultAsync(d => d.Id == detallePagoId);

        if (detalle == null || detalle.MetodoPago != MetodoPago.Transferencia || detalle.EsVerificado)
        {
            return false;
        }

        detalle.EsVerificado = true;
        detalle.FechaVerificacion = DateTime.UtcNow;

        // Comprobar si todas las transferencias de este pago están ya verificadas
        if (detalle.Pago != null)
        {
            bool todasVerificadas = detalle.Pago.Detalles
                .Where(d => d.MetodoPago == MetodoPago.Transferencia)
                .All(d => d.EsVerificado);

            if (todasVerificadas)
            {
                detalle.Pago.Estado = EstadoPago.Completado;
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Pago?> ObtenerPorIdAsync(int pagoId)
    {
        return await _context.Pagos
            .Include(p => p.Detalles)
            .Include(p => p.Usuario)
            .Include(p => p.Turno)
            .FirstOrDefaultAsync(p => p.Id == pagoId);
    }

    public async Task<List<Pago>> ObtenerPagosPorTurnoAsync(int turnoId)
    {
        return await _context.Pagos
            .Include(p => p.Detalles)
            .Include(p => p.Usuario)
            .Where(p => p.TurnoId == turnoId)
            .OrderByDescending(p => p.FechaPago)
            .ToListAsync();
    }

    public async Task<List<Pago>> ObtenerPagosPendientesVerificacionAsync()
    {
        return await _context.Pagos
            .Include(p => p.Detalles)
            .Include(p => p.Usuario)
            .Include(p => p.Turno)
            .Where(p => p.Estado == EstadoPago.PendienteVerificacion)
            .OrderByDescending(p => p.FechaPago)
            .ToListAsync();
    }
}
