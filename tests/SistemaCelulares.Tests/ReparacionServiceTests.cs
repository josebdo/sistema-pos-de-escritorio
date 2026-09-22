using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Models;
using SistemaCelulares.Infrastructure.Data;
using SistemaCelulares.Infrastructure.Security;
using SistemaCelulares.Infrastructure.Services;
using Xunit;

namespace SistemaCelulares.Tests;

public class ReparacionServiceTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task RegistrarRecepcion_GeneraNumeroOrdenYEstadoEnReparacion()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var admin = await context.Usuarios.FirstAsync();

        var cliente = new Cliente { NombreCompleto = "Ana Diaz", Telefono = "809-555-4321" };
        context.Clientes.Add(cliente);
        await context.SaveChangesAsync();

        var service = new ReparacionService(context);
        var orden = new OrdenReparacion
        {
            ClienteId = cliente.Id,
            Marca = "Xiaomi",
            Modelo = "Redmi Note 12",
            ImeiOSerie = "864209876543210",
            DescripcionProblema = "No da imagen en pantalla",
            PrecioEstimado = 2800m,
            UsuarioRecepcionId = admin.Id
        };

        // Act
        var creada = await service.RegistrarRecepcionAsync(orden);

        // Assert
        Assert.NotNull(creada);
        Assert.StartsWith("REP-", creada.NumeroOrden);
        Assert.Equal(EstadoReparacion.EnReparacion, creada.Estado);
        Assert.Equal(2800m, creada.PrecioEstimado);
        Assert.Equal(2800m, creada.PrecioFinal);
    }

    [Fact]
    public async Task ActualizarDiagnosticoYEstado_CambiaAListaParaEntrega()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var admin = await context.Usuarios.FirstAsync();

        var cliente = new Cliente { NombreCompleto = "Pedro", Telefono = "809-111-2222" };
        context.Clientes.Add(cliente);
        await context.SaveChangesAsync();

        var service = new ReparacionService(context);
        var orden = await service.RegistrarRecepcionAsync(new OrdenReparacion
        {
            ClienteId = cliente.Id,
            Marca = "Apple",
            Modelo = "iPhone 12",
            ImeiOSerie = "359876543210123",
            DescripcionProblema = "Cambio de bateria",
            PrecioEstimado = 3500m,
            UsuarioRecepcionId = admin.Id
        });

        // Act
        await service.ActualizarDiagnosticoAsync(orden.Id, "Batería reemplazada con éxito. Probada al 100%.", 3500m);
        await service.CambiarEstadoAsync(orden.Id, EstadoReparacion.ListaParaEntrega);

        // Assert
        var lista = await service.ObtenerPorIdAsync(orden.Id);
        Assert.NotNull(lista);
        Assert.Equal(EstadoReparacion.ListaParaEntrega, lista.Estado);
        Assert.Equal("Batería reemplazada con éxito. Probada al 100%.", lista.NotasDiagnostico);
    }

    [Fact]
    public async Task CobrarYEntregar_CompletaOrdenYAsociaPago()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var cajero = await context.Usuarios.FirstAsync(u => u.NombreUsuario == "cajero");

        var cliente = new Cliente { NombreCompleto = "Maria", Telefono = "809-777-8888" };
        context.Clientes.Add(cliente);
        await context.SaveChangesAsync();

        var turno = new Turno
        {
            UsuarioAperturaId = cajero.Id,
            MontoApertura = 1000m,
            MontoEsperado = 1000m,
            Estado = TurnoEstado.Abierto,
            FechaApertura = DateTime.UtcNow
        };
        context.Turnos.Add(turno);
        await context.SaveChangesAsync();

        var pagoService = new PagoService(context);
        var dtoPago = new RegistrarPagoDto
        {
            MontoTotal = 1500m,
            UsuarioId = cajero.Id,
            TurnoId = turno.Id,
            Metodos = new List<DetallePagoRequestDto>
            {
                new() { Metodo = MetodoPago.Efectivo, Monto = 1500m, MontoEntregado = 1500m }
            }
        };

        var resultadoPago = await pagoService.ProcesarPagoAsync(dtoPago);
        Assert.True(resultadoPago.Exitoso);

        var pago = await context.Pagos.FindAsync(resultadoPago.PagoId);
        Assert.NotNull(pago);

        var reparacionService = new ReparacionService(context);
        var orden = await reparacionService.RegistrarRecepcionAsync(new OrdenReparacion
        {
            ClienteId = cliente.Id,
            Marca = "Samsung",
            Modelo = "A32",
            ImeiOSerie = "351234567890123",
            DescripcionProblema = "Conector de carga dañado",
            PrecioEstimado = 1500m,
            PrecioFinal = 1500m,
            UsuarioRecepcionId = cajero.Id
        });

        await reparacionService.CambiarEstadoAsync(orden.Id, EstadoReparacion.ListaParaEntrega);

        // Act
        var entregada = await reparacionService.CobrarYEntregarAsync(orden.Id, cajero.Id, pago);

        // Assert
        Assert.Equal(EstadoReparacion.Entregada, entregada.Estado);
        Assert.NotNull(entregada.FechaEntrega);
        Assert.Equal(cajero.Id, entregada.UsuarioEntregaId);
        Assert.NotNull(entregada.PagoId);
    }
}
