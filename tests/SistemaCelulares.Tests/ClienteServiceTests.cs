using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Infrastructure.Data;
using SistemaCelulares.Infrastructure.Security;
using SistemaCelulares.Infrastructure.Services;
using Xunit;

namespace SistemaCelulares.Tests;

public class ClienteServiceTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CrearCliente_DatosValidos_GuardaCorrectamente()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ClienteService(context);

        var cliente = new Cliente
        {
            NombreCompleto = "Juan Perez",
            Telefono = "809-555-0101",
            Email = "juan@example.com",
            EsFrecuente = true,
            PorcentajeDescuento = 5.0m
        };

        // Act
        var creado = await service.CrearAsync(cliente);

        // Assert
        Assert.NotNull(creado);
        Assert.True(creado.Id > 0);
        Assert.Equal("Juan Perez", creado.NombreCompleto);
        Assert.True(creado.EsFrecuente);
        Assert.Equal(5.0m, creado.PorcentajeDescuento);
    }

    [Fact]
    public async Task BuscarClientes_PorTermino_EncuentraCoincidencias()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ClienteService(context);

        await service.CrearAsync(new Cliente { NombreCompleto = "Carlos Lopez", Telefono = "829-111-2233" });
        await service.CrearAsync(new Cliente { NombreCompleto = "Maria Santos", Telefono = "809-999-8877" });

        // Act
        var resultados = await service.BuscarAsync("111");

        // Assert
        Assert.Single(resultados);
        Assert.Equal("Carlos Lopez", resultados.First().NombreCompleto);
    }

    [Fact]
    public async Task ActualizarCliente_ModificaCamposCorrectamente()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ClienteService(context);

        var cliente = await service.CrearAsync(new Cliente { NombreCompleto = "Pedro Gomez", Telefono = "809-000-0000" });

        cliente.NombreCompleto = "Pedro Gomez Actualizado";
        cliente.EsFrecuente = true;
        cliente.PorcentajeDescuento = 10m;

        // Act
        await service.ActualizarAsync(cliente);
        var actualizado = await service.ObtenerPorIdAsync(cliente.Id);

        // Assert
        Assert.NotNull(actualizado);
        Assert.Equal("Pedro Gomez Actualizado", actualizado.NombreCompleto);
        Assert.True(actualizado.EsFrecuente);
        Assert.Equal(10m, actualizado.PorcentajeDescuento);
    }

    [Fact]
    public async Task ObtenerHistorial_RetornaVentasYReparaciones()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);

        var admin = await context.Usuarios.FirstAsync();
        var cliente = new Cliente { NombreCompleto = "Cliente Historial", Telefono = "809-123-4567" };
        context.Clientes.Add(cliente);
        await context.SaveChangesAsync();

        var turno = new Turno
        {
            UsuarioAperturaId = admin.Id,
            MontoApertura = 1000m,
            MontoEsperado = 1000m,
            Estado = TurnoEstado.Abierto,
            FechaApertura = DateTime.UtcNow
        };
        context.Turnos.Add(turno);
        await context.SaveChangesAsync();

        var venta = new Venta
        {
            NumeroFactura = "FAC-0001",
            ClienteId = cliente.Id,
            UsuarioId = admin.Id,
            TurnoId = turno.Id,
            Total = 1500m,
            Subtotal = 1500m
        };
        context.Ventas.Add(venta);

        var orden = new OrdenReparacion
        {
            NumeroOrden = "REP-2026-0001",
            ClienteId = cliente.Id,
            Marca = "Samsung",
            Modelo = "A54",
            DescripcionProblema = "Pantalla rota",
            UsuarioRecepcionId = admin.Id,
            PrecioEstimado = 2500m,
            PrecioFinal = 2500m,
            Estado = EstadoReparacion.EnReparacion
        };
        context.OrdenesReparacion.Add(orden);
        await context.SaveChangesAsync();

        var service = new ClienteService(context);

        // Act
        var ventas = await service.ObtenerHistorialVentasAsync(cliente.Id);
        var reparaciones = await service.ObtenerHistorialReparacionesAsync(cliente.Id);

        // Assert
        Assert.Single(ventas);
        Assert.Single(reparaciones);
        Assert.Equal(1500m, ventas.First().Total);
        Assert.Equal("Samsung", reparaciones.First().Marca);
    }
}
