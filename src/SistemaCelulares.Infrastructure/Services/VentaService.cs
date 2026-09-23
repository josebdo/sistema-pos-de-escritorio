using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;
using SistemaCelulares.Infrastructure.Data;

namespace SistemaCelulares.Infrastructure.Services;

public class VentaService : IVentaService
{
    private readonly AppDbContext _context;
    private readonly IPagoService _pagoService;
    private readonly IConfiguracionNegocioService? _configNegocioService;

    public VentaService(AppDbContext context, IPagoService pagoService, IConfiguracionNegocioService? configNegocioService = null)
    {
        _context = context;
        _pagoService = pagoService;
        _configNegocioService = configNegocioService;
    }

    public async Task<ResultadoVentaDto> RegistrarVentaAsync(RegistrarVentaRequestDto request)
    {
        var errores = new List<string>();

        if (request.Items == null || request.Items.Count == 0)
        {
            errores.Add("La venta debe contener al menos un producto.");
            return new ResultadoVentaDto { Exitoso = false, Mensaje = "El carrito de ventas está vacío.", Errores = errores };
        }

        // 1. Validar Turno Abierto
        var turno = await _context.Turnos.FindAsync(request.TurnoId);
        if (turno == null || turno.Estado != TurnoEstado.Abierto)
        {
            errores.Add("No se puede registrar una venta sin un turno de caja abierto.");
            return new ResultadoVentaDto { Exitoso = false, Mensaje = "Turno de caja cerrado o inexistente.", Errores = errores };
        }

        // 2. Validar Requisitos de NCF DGII
        if (request.TipoComprobante == TipoComprobanteFiscal.CreditoFiscal_B01)
        {
            if (string.IsNullOrWhiteSpace(request.RncCliente) && !request.ClienteId.HasValue)
            {
                errores.Add("Para emitir Factura de Crédito Fiscal (B01) es obligatorio registrar el RNC o Cédula del cliente.");
            }
        }

        // 3. Validar Stock y Cargar Productos
        var productosDict = new Dictionary<int, Producto>();
        decimal subtotalCalculado = 0m;
        decimal itbisCalculado = 0m;

        foreach (var item in request.Items)
        {
            if (item.Cantidad <= 0)
            {
                errores.Add($"La cantidad del producto '{item.NombreProducto}' debe ser mayor a cero.");
                continue;
            }

            var producto = await _context.Productos.FindAsync(item.ProductoId);
            if (producto == null || !producto.Activo)
            {
                errores.Add($"El producto '{item.NombreProducto}' (ID {item.ProductoId}) no existe o está desactivado.");
                continue;
            }

            if (producto.StockActual < item.Cantidad)
            {
                errores.Add($"Stock insuficiente para '{producto.Nombre}'. Stock disponible: {producto.StockActual}, solicitado: {item.Cantidad}.");
            }

            if (producto.RequiereSerie)
            {
                if (string.IsNullOrWhiteSpace(item.Imei) && !item.UnidadProductoId.HasValue)
                {
                    errores.Add($"El producto '{producto.Nombre}' requiere seleccionar o ingresar el IMEI del celular para la venta y garantía.");
                }
                else
                {
                    var imeiBuscado = item.Imei?.Trim().ToLower();
                    var unidad = await _context.UnidadesProducto
                        .FirstOrDefaultAsync(u => u.ProductoId == producto.Id &&
                            (u.Id == item.UnidadProductoId || (imeiBuscado != null && u.Imei.ToLower() == imeiBuscado)));

                    if (unidad == null || unidad.Estado != EstadoUnidadProducto.EnStock)
                    {
                        errores.Add($"La unidad celular con IMEI '{item.Imei}' no está disponible en stock para '{producto.Nombre}'.");
                    }
                    else
                    {
                        item.UnidadProductoId = unidad.Id;
                        item.Imei = unidad.Imei;
                    }
                }
            }

            productosDict[producto.Id] = producto;

            decimal totalLinea = producto.PrecioVenta * item.Cantidad;
            decimal itbisLinea = item.AplicaItbis ? Math.Round(totalLinea * 0.18m / 1.18m, 2) : 0m;
            decimal subtotalLinea = totalLinea - itbisLinea;

            subtotalCalculado += subtotalLinea;
            itbisCalculado += itbisLinea;
        }

        if (errores.Count > 0)
        {
            return new ResultadoVentaDto
            {
                Exitoso = false,
                Mensaje = "Existen errores de validación de productos o stock.",
                Errores = errores
            };
        }

        decimal totalVenta = subtotalCalculado + itbisCalculado - request.Descuento;
        if (totalVenta < 0) totalVenta = 0;

        // 4. Generar NCF si corresponde
        string? ncfGenerado = null;
        if (request.TipoComprobante != TipoComprobanteFiscal.SinComprobante)
        {
            ncfGenerado = await GenerarSiguienteNcfAsync(request.TipoComprobante);
            if (string.IsNullOrEmpty(ncfGenerado))
            {
                return new ResultadoVentaDto
                {
                    Exitoso = false,
                    Mensaje = $"No hay secuencias de NCF activas o vigentes para el tipo {request.TipoComprobante}.",
                    Errores = new List<string> { "Secuencia de NCF agotada o vencida. Configure una nueva secuencia en Comprobantes Fiscales." }
                };
            }
        }

        // 5. Procesar Pago
        int? pagoId = request.PagoId;
        decimal vuelto = 0m;
        if (request.PagoRequest != null)
        {
            request.PagoRequest.MontoTotal = totalVenta;
            request.PagoRequest.UsuarioId = request.UsuarioId;
            request.PagoRequest.TurnoId = request.TurnoId;

            var resultadoPago = await _pagoService.ProcesarPagoAsync(request.PagoRequest);
            if (!resultadoPago.Exitoso)
            {
                return new ResultadoVentaDto
                {
                    Exitoso = false,
                    Mensaje = "Error al procesar el pago de la venta.",
                    Errores = resultadoPago.Errores
                };
            }

            pagoId = resultadoPago.PagoId;
            vuelto = resultadoPago.MontoVuelto;
        }
        else if (pagoId.HasValue)
        {
            var pagoExistente = await _context.Pagos.FindAsync(pagoId.Value);
            if (pagoExistente != null)
            {
                vuelto = pagoExistente.MontoVuelto;
            }
        }

        // 6. Generar Correlativo de Factura Interna
        int totalVentasExistentes = await _context.Ventas.CountAsync();
        string numeroFactura = $"FAC-{totalVentasExistentes + 1:D6}";

        // 7. Crear Entidad Venta y Descontar Stock
        var nuevaVenta = new Venta
        {
            NumeroFactura = numeroFactura,
            TipoComprobante = request.TipoComprobante,
            Ncf = ncfGenerado,
            FechaVenta = DateTime.UtcNow,
            Subtotal = subtotalCalculado,
            Itbis = itbisCalculado,
            Descuento = request.Descuento,
            Total = totalVenta,
            Estado = EstadoVenta.Completada,
            UsuarioId = request.UsuarioId,
            TurnoId = request.TurnoId,
            ClienteId = request.ClienteId,
            NombreClienteAnonimo = request.NombreCliente,
            RncCliente = request.RncCliente,
            PagoId = pagoId,
            Observaciones = request.Observaciones
        };

        foreach (var item in request.Items)
        {
            var prod = productosDict[item.ProductoId];
            decimal totalLinea = prod.PrecioVenta * item.Cantidad;
            decimal itbisLinea = item.AplicaItbis ? Math.Round(totalLinea * 0.18m / 1.18m, 2) : 0m;
            decimal subtotalLinea = totalLinea - itbisLinea;

            var detalle = new DetalleVenta
            {
                ProductoId = prod.Id,
                Cantidad = item.Cantidad,
                PrecioUnitario = prod.PrecioVenta,
                CostoUnitario = prod.PrecioCosto,
                Itbis = itbisLinea,
                Subtotal = subtotalLinea,
                UnidadProductoId = item.UnidadProductoId,
                Imei = item.Imei
            };

            nuevaVenta.Detalles.Add(detalle);

            // Actualizar unidad física vendida si aplica
            if (item.UnidadProductoId.HasValue)
            {
                var unidadVendida = await _context.UnidadesProducto.FindAsync(item.UnidadProductoId.Value);
                if (unidadVendida != null)
                {
                    unidadVendida.Estado = EstadoUnidadProducto.Vendido;
                    unidadVendida.FechaVenta = DateTime.UtcNow;
                    unidadVendida.Venta = nuevaVenta;
                }
            }

            // Decremento de stock atómico
            prod.StockActual -= item.Cantidad;
        }

        _context.Ventas.Add(nuevaVenta);
        await _context.SaveChangesAsync();

        return new ResultadoVentaDto
        {
            Exitoso = true,
            Mensaje = "Venta registrada exitosamente.",
            VentaId = nuevaVenta.Id,
            NumeroFactura = nuevaVenta.NumeroFactura,
            Ncf = nuevaVenta.Ncf,
            Subtotal = nuevaVenta.Subtotal,
            Itbis = nuevaVenta.Itbis,
            Descuento = nuevaVenta.Descuento,
            Total = nuevaVenta.Total,
            Vuelto = vuelto
        };
    }

    public async Task<string?> GenerarSiguienteNcfAsync(TipoComprobanteFiscal tipo)
    {
        var secuencia = await _context.ComprobanteFiscalSecuencias
            .FirstOrDefaultAsync(s => s.Tipo == tipo && s.Activo);

        if (secuencia == null) return null;
        if (secuencia.FechaVencimiento < DateTime.UtcNow) return null;
        if (secuencia.SecuenciaActual > secuencia.SecuenciaHasta) return null;

        string ncf = secuencia.ObtenerNcfActual();
        secuencia.SecuenciaActual++;
        await _context.SaveChangesAsync();

        return ncf;
    }

    public async Task<Venta?> ObtenerPorIdAsync(int ventaId)
    {
        return await _context.Ventas
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
            .Include(v => v.Usuario)
            .Include(v => v.Cliente)
            .Include(v => v.Turno)
            .Include(v => v.Pago)
                .ThenInclude(p => p!.Detalles)
            .FirstOrDefaultAsync(v => v.Id == ventaId);
    }

    public async Task<List<Venta>> ObtenerHistorialAsync(DateTime? desde = null, DateTime? hasta = null, int? usuarioId = null, int? turnoId = null)
    {
        var query = _context.Ventas
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
            .Include(v => v.Usuario)
            .Include(v => v.Cliente)
            .Include(v => v.Pago)
            .AsQueryable();

        if (desde.HasValue) query = query.Where(v => v.FechaVenta >= desde.Value);
        if (hasta.HasValue) query = query.Where(v => v.FechaVenta <= hasta.Value);
        if (usuarioId.HasValue) query = query.Where(v => v.UsuarioId == usuarioId.Value);
        if (turnoId.HasValue) query = query.Where(v => v.TurnoId == turnoId.Value);

        return await query.OrderByDescending(v => v.FechaVenta).ToListAsync();
    }

    public async Task<bool> AnularVentaAsync(int ventaId, string motivo, int usuarioId)
    {
        var venta = await _context.Ventas
            .Include(v => v.Detalles)
            .Include(v => v.Pago)
                .ThenInclude(p => p!.Detalles)
            .Include(v => v.Turno)
            .FirstOrDefaultAsync(v => v.Id == ventaId);

        if (venta == null || venta.Estado == EstadoVenta.Anulada) return false;

        // 1. Revertir Stock y Unidades Físicas
        foreach (var d in venta.Detalles)
        {
            var prod = await _context.Productos.FindAsync(d.ProductoId);
            if (prod != null)
            {
                prod.StockActual += d.Cantidad;
            }

            if (d.UnidadProductoId.HasValue)
            {
                var unidad = await _context.UnidadesProducto.FindAsync(d.UnidadProductoId.Value);
                if (unidad != null)
                {
                    unidad.Estado = EstadoUnidadProducto.EnStock;
                    unidad.FechaVenta = null;
                    unidad.VentaId = null;
                    unidad.Venta = null;
                }
            }
        }

        // 2. Anular Pago y Revertir Efectivo de Turno si aplica
        if (venta.Pago != null)
        {
            venta.Pago.Estado = EstadoPago.Cancelado;

            if (venta.Turno != null && venta.Turno.Estado == TurnoEstado.Abierto)
            {
                decimal efectivoCobrado = venta.Pago.Detalles
                    .Where(dp => dp.MetodoPago == MetodoPago.Efectivo)
                    .Sum(dp => dp.Monto);

                if (efectivoCobrado > 0)
                {
                    venta.Turno.TotalVentasEfectivo -= efectivoCobrado;
                    if (venta.Turno.TotalVentasEfectivo < 0) venta.Turno.TotalVentasEfectivo = 0;
                    venta.Turno.MontoEsperado = venta.Turno.MontoApertura + venta.Turno.TotalVentasEfectivo;
                }
            }
        }

        venta.Estado = EstadoVenta.Anulada;
        venta.Observaciones = $"{venta.Observaciones} | ANULADA por Usuario ID {usuarioId} el {DateTime.UtcNow:dd/MM/yyyy HH:mm}. Motivo: {motivo}".TrimStart(' ', '|');

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ComprobanteFiscalSecuencia>> ObtenerSecuenciasNcfAsync()
    {
        return await _context.ComprobanteFiscalSecuencias.OrderBy(s => s.Tipo).ToListAsync();
    }

    public async Task GuardarSecuenciaNcfAsync(ComprobanteFiscalSecuencia secuencia)
    {
        var existente = await _context.ComprobanteFiscalSecuencias.FindAsync(secuencia.Id);
        if (existente == null)
        {
            _context.ComprobanteFiscalSecuencias.Add(secuencia);
        }
        else
        {
            existente.SecuenciaActual = secuencia.SecuenciaActual;
            existente.SecuenciaHasta = secuencia.SecuenciaHasta;
            existente.FechaVencimiento = secuencia.FechaVencimiento;
            existente.Activo = secuencia.Activo;
            existente.Descripcion = secuencia.Descripcion;
        }
        await _context.SaveChangesAsync();
    }

    public async Task<TicketVentaDto> GenerarTicketVentaAsync(int ventaId)
    {
        var venta = await ObtenerPorIdAsync(ventaId);
        if (venta == null) throw new InvalidOperationException($"Venta {ventaId} no encontrada.");

        string metodosTexto = "No registrado";
        decimal montoEntregado = venta.Total;
        decimal montoVuelto = 0m;

        if (venta.Pago != null)
        {
            montoEntregado = venta.Pago.MontoPagado;
            montoVuelto = venta.Pago.MontoVuelto;
            metodosTexto = string.Join(", ", venta.Pago.Detalles.Select(d => $"{d.MetodoPago}: RD${d.Monto:N2}"));
        }

        string tipoDesc = venta.TipoComprobante switch
        {
            TipoComprobanteFiscal.CreditoFiscal_B01 => "FACTURA DE CRÉDITO FISCAL",
            TipoComprobanteFiscal.Consumo_B02 => "FACTURA PARA CONSUMIDOR FINAL",
            TipoComprobanteFiscal.RegimenEspecial_B14 => "REGÍMENES ESPECIALES",
            TipoComprobanteFiscal.Gubernamental_B15 => "GUBERNAMENTAL",
            _ => "TICKET DE VENTA"
        };

        var configNegocio = _configNegocioService != null 
            ? await _configNegocioService.ObtenerConfiguracionAsync() 
            : null;

        var ticket = new TicketVentaDto
        {
            NombreEmpresa = !string.IsNullOrWhiteSpace(configNegocio?.NombreEmpresa) ? configNegocio.NombreEmpresa : "TIENDA DE CELULARES",
            RncEmpresa = !string.IsNullOrWhiteSpace(configNegocio?.RncCedula) ? configNegocio.RncCedula : "000-0000000-0",
            TelefonoEmpresa = !string.IsNullOrWhiteSpace(configNegocio?.Telefono) ? configNegocio.Telefono : "(809) 000-0000",
            DireccionEmpresa = !string.IsNullOrWhiteSpace(configNegocio?.Direccion) ? (configNegocio.Direccion + (!string.IsNullOrWhiteSpace(configNegocio.Ciudad) ? $", {configNegocio.Ciudad}" : "")) : "República Dominicana",
            NumeroFactura = venta.NumeroFactura,
            Ncf = venta.Ncf,
            TipoComprobanteDescripcion = tipoDesc,
            Fecha = venta.FechaVenta,
            CajeroNombre = venta.Usuario?.NombreCompleto ?? "Cajero",
            CajaId = venta.Turno?.CajaId ?? 1,
            ClienteNombre = venta.Cliente?.NombreCompleto ?? venta.NombreClienteAnonimo ?? "Cliente de Contado",
            ClienteRnc = venta.Cliente?.RncOCedula ?? venta.RncCliente,
            Subtotal = venta.Subtotal,
            Itbis = venta.Itbis,
            Descuento = venta.Descuento,
            Total = venta.Total,
            MontoEntregado = montoEntregado,
            MontoVuelto = montoVuelto,
            MetodosPagoTexto = metodosTexto,
            Items = venta.Detalles.Select(d => new ItemCarritoVentaDto
            {
                ProductoId = d.ProductoId,
                NombreProducto = d.Producto?.Nombre ?? "Producto",
                Sku = d.Producto?.Sku ?? string.Empty,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                CostoUnitario = d.CostoUnitario,
                AplicaItbis = d.Itbis > 0,
                UnidadProductoId = d.UnidadProductoId,
                Imei = d.Imei
            }).ToList()
        };

        return ticket;
    }
}
