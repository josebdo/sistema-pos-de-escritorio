using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Models;
using SistemaCelulares.Infrastructure.Data;
using SistemaCelulares.Infrastructure.Security;
using SistemaCelulares.Infrastructure.Services;
using Xunit;

namespace SistemaCelulares.Tests;

public class VentasNcfTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task RegistrarVenta_ConStockSuficienteYConsumoB02_DescuentaStockYGeneraNcf()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var admin = await context.Usuarios.FirstAsync();
        var cat = await context.Categorias.FirstAsync();

        var producto = new Producto
        {
            Nombre = "Samsung Galaxy S24",
            Sku = "CEL-0010",
            PrecioCosto = 40000m,
            PrecioVenta = 55000m,
            StockActual = 5,
            CategoriaId = cat.Id
        };
        context.Productos.Add(producto);

        var turno = new Turno
        {
            UsuarioAperturaId = admin.Id,
            MontoApertura = 2000m,
            MontoEsperado = 2000m,
            Estado = TurnoEstado.Abierto,
            FechaApertura = DateTime.UtcNow
        };
        context.Turnos.Add(turno);
        await context.SaveChangesAsync();

        var pagoService = new PagoService(context);
        var ventaService = new VentaService(context, pagoService);

        var request = new RegistrarVentaRequestDto
        {
            UsuarioId = admin.Id,
            TurnoId = turno.Id,
            TipoComprobante = TipoComprobanteFiscal.Consumo_B02,
            NombreCliente = "Juan Pérez",
            Items = new List<ItemCarritoVentaDto>
            {
                new()
                {
                    ProductoId = producto.Id,
                    NombreProducto = producto.Nombre,
                    Sku = producto.Sku,
                    Cantidad = 2,
                    PrecioUnitario = 55000m,
                    CostoUnitario = 40000m,
                    AplicaItbis = true
                }
            },
            PagoRequest = new RegistrarPagoDto
            {
                MontoTotal = 110000m,
                UsuarioId = admin.Id,
                TurnoId = turno.Id,
                Metodos = new List<DetallePagoRequestDto>
                {
                    new()
                    {
                        Metodo = MetodoPago.Efectivo,
                        Monto = 110000m,
                        MontoEntregado = 110000m
                    }
                }
            }
        };

        // Act
        var res = await ventaService.RegistrarVentaAsync(request);

        // Assert
        Assert.True(res.Exitoso);
        Assert.NotNull(res.Ncf);
        Assert.StartsWith("B02", res.Ncf);
        Assert.Equal(110000m, res.Total);

        var prodActualizado = await context.Productos.FindAsync(producto.Id);
        Assert.Equal(3, prodActualizado!.StockActual); // 5 - 2 = 3

        var turnoActualizado = await context.Turnos.FindAsync(turno.Id);
        Assert.Equal(110000m, turnoActualizado!.TotalVentasEfectivo);
    }

    [Fact]
    public async Task RegistrarVenta_SinTurnoAbierto_RechazaVentaConMensajeApropiado()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var admin = await context.Usuarios.FirstAsync();

        var pagoService = new PagoService(context);
        var ventaService = new VentaService(context, pagoService);

        var request = new RegistrarVentaRequestDto
        {
            UsuarioId = admin.Id,
            TurnoId = 999, // Inexistente
            Items = new List<ItemCarritoVentaDto>
            {
                new() { ProductoId = 1, Cantidad = 1, PrecioUnitario = 1000m }
            }
        };

        // Act
        var res = await ventaService.RegistrarVentaAsync(request);

        // Assert
        Assert.False(res.Exitoso);
        Assert.Contains(res.Errores, e => e.Contains("turno"));
    }

    [Fact]
    public async Task RegistrarVenta_ConStockInsuficiente_FallaYNoAlteraInventario()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var admin = await context.Usuarios.FirstAsync();
        var cat = await context.Categorias.FirstAsync();

        var prod = new Producto
        {
            Nombre = "Cargador Rápido 20W",
            Sku = "ACC-0099",
            StockActual = 2,
            PrecioVenta = 850m,
            CategoriaId = cat.Id
        };
        context.Productos.Add(prod);

        var turno = new Turno
        {
            UsuarioAperturaId = admin.Id,
            MontoApertura = 1000m,
            Estado = TurnoEstado.Abierto
        };
        context.Turnos.Add(turno);
        await context.SaveChangesAsync();

        var pagoService = new PagoService(context);
        var ventaService = new VentaService(context, pagoService);

        var request = new RegistrarVentaRequestDto
        {
            UsuarioId = admin.Id,
            TurnoId = turno.Id,
            Items = new List<ItemCarritoVentaDto>
            {
                new()
                {
                    ProductoId = prod.Id,
                    NombreProducto = prod.Nombre,
                    Cantidad = 5, // Pide 5, hay 2
                    PrecioUnitario = 850m
                }
            }
        };

        // Act
        var res = await ventaService.RegistrarVentaAsync(request);

        // Assert
        Assert.False(res.Exitoso);
        Assert.Contains(res.Errores, e => e.Contains("Stock insuficiente"));

        var prodDb = await context.Productos.FindAsync(prod.Id);
        Assert.Equal(2, prodDb!.StockActual); // Intacto
    }

    [Fact]
    public async Task RegistrarVenta_CreditoFiscalB01SinRnc_FallaValidacion()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var admin = await context.Usuarios.FirstAsync();
        var cat = await context.Categorias.FirstAsync();

        var prod = new Producto { Nombre = "Funda", Sku = "ACC-0100", StockActual = 10, PrecioVenta = 300m, CategoriaId = cat.Id };
        context.Productos.Add(prod);

        var turno = new Turno { UsuarioAperturaId = admin.Id, MontoApertura = 1000m, Estado = TurnoEstado.Abierto };
        context.Turnos.Add(turno);
        await context.SaveChangesAsync();

        var pagoService = new PagoService(context);
        var ventaService = new VentaService(context, pagoService);

        var request = new RegistrarVentaRequestDto
        {
            UsuarioId = admin.Id,
            TurnoId = turno.Id,
            TipoComprobante = TipoComprobanteFiscal.CreditoFiscal_B01,
            RncCliente = "", // Vacío
            Items = new List<ItemCarritoVentaDto>
            {
                new() { ProductoId = prod.Id, NombreProducto = prod.Nombre, Cantidad = 1, PrecioUnitario = 300m }
            }
        };

        // Act
        var res = await ventaService.RegistrarVentaAsync(request);

        // Assert
        Assert.False(res.Exitoso);
        Assert.Contains(res.Errores, e => e.Contains("obligatorio registrar el RNC"));
    }

    [Fact]
    public async Task RegistrarVenta_CreditoFiscalB01ConRnc_GeneraNcfB01Correctamente()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var admin = await context.Usuarios.FirstAsync();
        var cat = await context.Categorias.FirstAsync();

        var prod = new Producto { Nombre = "Pantalla OLED", Sku = "REP-0010", StockActual = 5, PrecioVenta = 4500m, CategoriaId = cat.Id };
        context.Productos.Add(prod);

        var turno = new Turno { UsuarioAperturaId = admin.Id, MontoApertura = 1000m, Estado = TurnoEstado.Abierto };
        context.Turnos.Add(turno);
        await context.SaveChangesAsync();

        var pagoService = new PagoService(context);
        var ventaService = new VentaService(context, pagoService);

        var request = new RegistrarVentaRequestDto
        {
            UsuarioId = admin.Id,
            TurnoId = turno.Id,
            TipoComprobante = TipoComprobanteFiscal.CreditoFiscal_B01,
            NombreCliente = "Distribuidora Nacional SRL",
            RncCliente = "130-99881-2",
            Items = new List<ItemCarritoVentaDto>
            {
                new() { ProductoId = prod.Id, NombreProducto = prod.Nombre, Cantidad = 1, PrecioUnitario = 4500m }
            },
            PagoRequest = new RegistrarPagoDto
            {
                MontoTotal = 4500m,
                Metodos = new List<DetallePagoRequestDto>
                {
                    new() { Metodo = MetodoPago.Transferencia, Monto = 4500m, EsVerificado = true }
                }
            }
        };

        // Act
        var res = await ventaService.RegistrarVentaAsync(request);

        // Assert
        Assert.True(res.Exitoso);
        Assert.NotNull(res.Ncf);
        Assert.StartsWith("B01", res.Ncf);
    }

    [Fact]
    public async Task AnularVenta_VentaValida_RevierteStockYMarcaAnulada()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var admin = await context.Usuarios.FirstAsync();
        var cat = await context.Categorias.FirstAsync();

        var prod = new Producto { Nombre = "Batería iPhone", Sku = "REP-0020", StockActual = 8, PrecioVenta = 1200m, CategoriaId = cat.Id };
        context.Productos.Add(prod);

        var turno = new Turno { UsuarioAperturaId = admin.Id, MontoApertura = 1000m, Estado = TurnoEstado.Abierto };
        context.Turnos.Add(turno);
        await context.SaveChangesAsync();

        var pagoService = new PagoService(context);
        var ventaService = new VentaService(context, pagoService);

        var request = new RegistrarVentaRequestDto
        {
            UsuarioId = admin.Id,
            TurnoId = turno.Id,
            TipoComprobante = TipoComprobanteFiscal.Consumo_B02,
            Items = new List<ItemCarritoVentaDto>
            {
                new() { ProductoId = prod.Id, NombreProducto = prod.Nombre, Cantidad = 3, PrecioUnitario = 1200m }
            },
            PagoRequest = new RegistrarPagoDto
            {
                MontoTotal = 3600m,
                Metodos = new List<DetallePagoRequestDto>
                {
                    new() { Metodo = MetodoPago.Efectivo, Monto = 3600m, MontoEntregado = 4000m }
                }
            }
        };

        var regRes = await ventaService.RegistrarVentaAsync(request);
        Assert.Equal(5, (await context.Productos.FindAsync(prod.Id))!.StockActual); // 8 - 3 = 5

        // Act
        bool anulada = await ventaService.AnularVentaAsync(regRes.VentaId!.Value, "Cliente devolvió la mercancía", admin.Id);

        // Assert
        Assert.True(anulada);
        var ventaDb = await ventaService.ObtenerPorIdAsync(regRes.VentaId!.Value);
        Assert.Equal(EstadoVenta.Anulada, ventaDb!.Estado);

        var prodRevertido = await context.Productos.FindAsync(prod.Id);
        Assert.Equal(8, prodRevertido!.StockActual); // Revertido a 8
    }

    [Fact]
    public async Task GenerarTicketVenta_VentaExistente_GeneraTicketConDesgloseItbisYDatosEmpresa()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var admin = await context.Usuarios.FirstAsync();
        var cat = await context.Categorias.FirstAsync();

        var prod = new Producto { Nombre = "Audífonos Bluetooth", Sku = "ACC-0500", StockActual = 10, PrecioVenta = 1180m, CategoriaId = cat.Id };
        context.Productos.Add(prod);

        var turno = new Turno { UsuarioAperturaId = admin.Id, MontoApertura = 500m, Estado = TurnoEstado.Abierto };
        context.Turnos.Add(turno);
        await context.SaveChangesAsync();

        var pagoService = new PagoService(context);
        var ventaService = new VentaService(context, pagoService);

        var request = new RegistrarVentaRequestDto
        {
            UsuarioId = admin.Id,
            TurnoId = turno.Id,
            TipoComprobante = TipoComprobanteFiscal.Consumo_B02,
            NombreCliente = "Pedro Santana",
            Items = new List<ItemCarritoVentaDto>
            {
                new() { ProductoId = prod.Id, NombreProducto = prod.Nombre, Cantidad = 1, PrecioUnitario = 1180m, AplicaItbis = true }
            },
            PagoRequest = new RegistrarPagoDto
            {
                MontoTotal = 1180m,
                Metodos = new List<DetallePagoRequestDto>
                {
                    new() { Metodo = MetodoPago.Efectivo, Monto = 1180m, MontoEntregado = 1500m }
                }
            }
        };

        var reg = await ventaService.RegistrarVentaAsync(request);

        // Act
        var ticket = await ventaService.GenerarTicketVentaAsync(reg.VentaId!.Value);

        // Assert
        Assert.NotNull(ticket);
        Assert.Equal(reg.NumeroFactura, ticket.NumeroFactura);
        Assert.Equal(1180m, ticket.Total);
        Assert.Equal(180m, ticket.Itbis);
        Assert.Equal(1000m, ticket.Subtotal);
        Assert.Equal(1500m, ticket.MontoEntregado);
        Assert.Equal(320m, ticket.MontoVuelto);
        Assert.Single(ticket.Items);
    }
}
