using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Models;
using SistemaCelulares.Infrastructure.Data;
using SistemaCelulares.Infrastructure.Security;
using SistemaCelulares.Infrastructure.Services;
using Xunit;

namespace SistemaCelulares.Tests;

public class MetodosPagoTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void CalcularVueltoEfectivo_PagoExacto_RetornaVueltoCeroYESuficiente()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new PagoService(context);

        // Act
        var res = service.CalcularVueltoEfectivo(1500m, 1500m);

        // Assert
        Assert.True(res.EsSuficiente);
        Assert.Equal(0m, res.Vuelto);
        Assert.Equal(0m, res.MontoFaltante);
    }

    [Fact]
    public void CalcularVueltoEfectivo_PagoMayor_CalculaVueltoCorrectamente()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new PagoService(context);

        // Act
        var res = service.CalcularVueltoEfectivo(1250m, 2000m);

        // Assert
        Assert.True(res.EsSuficiente);
        Assert.Equal(750m, res.Vuelto);
        Assert.Equal(0m, res.MontoFaltante);
    }

    [Fact]
    public void CalcularVueltoEfectivo_PagoMenor_RetornaInsuficienteYMontoFaltante()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new PagoService(context);

        // Act
        var res = service.CalcularVueltoEfectivo(1000m, 800m);

        // Assert
        Assert.False(res.EsSuficiente);
        Assert.Equal(0m, res.Vuelto);
        Assert.Equal(200m, res.MontoFaltante);
    }

    [Fact]
    public async Task ProcesarPago_EfectivoValido_CreaPagoYActualizaVentasEfectivoEnTurno()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var admin = await context.Usuarios.FirstAsync();

        var turno = new Turno
        {
            UsuarioAperturaId = admin.Id,
            MontoApertura = 3000m,
            MontoEsperado = 3000m,
            Estado = TurnoEstado.Abierto,
            FechaApertura = DateTime.UtcNow
        };
        context.Turnos.Add(turno);
        await context.SaveChangesAsync();

        var service = new PagoService(context);
        var request = new RegistrarPagoDto
        {
            MontoTotal = 1500m,
            UsuarioId = admin.Id,
            TurnoId = turno.Id,
            Metodos = new List<DetallePagoRequestDto>
            {
                new()
                {
                    Metodo = MetodoPago.Efectivo,
                    Monto = 1500m,
                    MontoEntregado = 2000m
                }
            }
        };

        // Act
        var res = await service.ProcesarPagoAsync(request);

        // Assert
        Assert.True(res.Exitoso);
        Assert.Equal(EstadoPago.Completado, res.Estado);
        Assert.Equal(1500m, res.MontoTotal);
        Assert.Equal(2000m, res.MontoPagado);
        Assert.Equal(500m, res.MontoVuelto);

        var turnoDb = await context.Turnos.FindAsync(turno.Id);
        Assert.Equal(1500m, turnoDb!.TotalVentasEfectivo);
        Assert.Equal(4500m, turnoDb.MontoEsperado);
    }

    [Fact]
    public async Task ProcesarPago_EfectivoInsuficiente_FallaConMensajeError()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var admin = await context.Usuarios.FirstAsync();
        var service = new PagoService(context);

        var request = new RegistrarPagoDto
        {
            MontoTotal = 1000m,
            UsuarioId = admin.Id,
            Metodos = new List<DetallePagoRequestDto>
            {
                new()
                {
                    Metodo = MetodoPago.Efectivo,
                    Monto = 1000m,
                    MontoEntregado = 500m // insuficiente
                }
            }
        };

        // Act
        var res = await service.ProcesarPagoAsync(request);

        // Assert
        Assert.False(res.Exitoso);
        Assert.Contains(res.Errores, e => e.Contains("menor al monto"));
    }

    [Fact]
    public async Task ProcesarPago_TransferenciaVerificada_QuedaEnEstadoCompletado()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var admin = await context.Usuarios.FirstAsync();
        var service = new PagoService(context);

        var request = new RegistrarPagoDto
        {
            MontoTotal = 4500m,
            UsuarioId = admin.Id,
            Metodos = new List<DetallePagoRequestDto>
            {
                new()
                {
                    Metodo = MetodoPago.Transferencia,
                    Monto = 4500m,
                    BancoDestino = "Banco Popular Dominicano",
                    NumeroReferencia = "REF-889900",
                    EsVerificado = true
                }
            }
        };

        // Act
        var res = await service.ProcesarPagoAsync(request);

        // Assert
        Assert.True(res.Exitoso);
        Assert.Equal(EstadoPago.Completado, res.Estado);
    }

    [Fact]
    public async Task ProcesarPago_TransferenciaNoVerificada_QuedaEnEstadoPendienteVerificacion()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var admin = await context.Usuarios.FirstAsync();
        var service = new PagoService(context);

        var request = new RegistrarPagoDto
        {
            MontoTotal = 8000m,
            UsuarioId = admin.Id,
            Metodos = new List<DetallePagoRequestDto>
            {
                new()
                {
                    Metodo = MetodoPago.Transferencia,
                    Monto = 8000m,
                    BancoDestino = "Banreservas",
                    NumeroReferencia = "TRANSF-0012",
                    EsVerificado = false
                }
            }
        };

        // Act
        var res = await service.ProcesarPagoAsync(request);

        // Assert
        Assert.True(res.Exitoso);
        Assert.Equal(EstadoPago.PendienteVerificacion, res.Estado);

        var pendientes = await service.ObtenerPagosPendientesVerificacionAsync();
        Assert.Single(pendientes);
    }

    [Fact]
    public async Task VerificarTransferencia_DetallePendiente_PasaACompletado()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var admin = await context.Usuarios.FirstAsync();
        var service = new PagoService(context);

        var request = new RegistrarPagoDto
        {
            MontoTotal = 3000m,
            UsuarioId = admin.Id,
            Metodos = new List<DetallePagoRequestDto>
            {
                new()
                {
                    Metodo = MetodoPago.Transferencia,
                    Monto = 3000m,
                    BancoDestino = "Banco BHD",
                    NumeroReferencia = "BHD-777",
                    EsVerificado = false
                }
            }
        };

        var regRes = await service.ProcesarPagoAsync(request);
        var pago = await service.ObtenerPorIdAsync(regRes.PagoId!.Value);
        var detalle = pago!.Detalles.First();

        // Act
        bool verificado = await service.VerificarTransferenciaAsync(detalle.Id);

        // Assert
        Assert.True(verificado);
        var pagoActualizado = await service.ObtenerPorIdAsync(pago.Id);
        Assert.Equal(EstadoPago.Completado, pagoActualizado!.Estado);
    }

    [Fact]
    public async Task ProcesarPago_TarjetaDebitoYCredito_RegistraAutorizacionYTipo()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var admin = await context.Usuarios.FirstAsync();
        var service = new PagoService(context);

        var request = new RegistrarPagoDto
        {
            MontoTotal = 6200m,
            UsuarioId = admin.Id,
            Metodos = new List<DetallePagoRequestDto>
            {
                new()
                {
                    Metodo = MetodoPago.Tarjeta,
                    Monto = 6200m,
                    TipoTarjeta = SubtipoTarjeta.Credito,
                    NumeroAutorizacionPos = "Cardnet - AUTH998811"
                }
            }
        };

        // Act
        var res = await service.ProcesarPagoAsync(request);

        // Assert
        Assert.True(res.Exitoso);
        Assert.Equal(EstadoPago.Completado, res.Estado);

        var pagoDb = await service.ObtenerPorIdAsync(res.PagoId!.Value);
        Assert.Equal(MetodoPago.Tarjeta, pagoDb!.MetodoPrincipal);
        Assert.Equal(SubtipoTarjeta.Credito, pagoDb.Detalles.First().TipoTarjeta);
        Assert.Equal("Cardnet - AUTH998811", pagoDb.Detalles.First().NumeroAutorizacionPos);
    }

    [Fact]
    public async Task ProcesarPago_PagoMixto_DesglosaImportesYValidaSumaExacta()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var admin = await context.Usuarios.FirstAsync();
        var service = new PagoService(context);

        var request = new RegistrarPagoDto
        {
            MontoTotal = 10000m,
            UsuarioId = admin.Id,
            Metodos = new List<DetallePagoRequestDto>
            {
                new()
                {
                    Metodo = MetodoPago.Efectivo,
                    Monto = 3000m,
                    MontoEntregado = 3500m
                },
                new()
                {
                    Metodo = MetodoPago.Transferencia,
                    Monto = 5000m,
                    BancoDestino = "Banreservas",
                    NumeroReferencia = "REF-1234",
                    EsVerificado = true
                },
                new()
                {
                    Metodo = MetodoPago.Tarjeta,
                    Monto = 2000m,
                    TipoTarjeta = SubtipoTarjeta.Debito,
                    NumeroAutorizacionPos = "Azul - 001928"
                }
            }
        };

        // Act
        var res = await service.ProcesarPagoAsync(request);

        // Assert
        Assert.True(res.Exitoso);
        Assert.Equal(MetodoPago.Mixto, (await service.ObtenerPorIdAsync(res.PagoId!.Value))!.MetodoPrincipal);
        Assert.Equal(10000m, res.MontoTotal);
        Assert.Equal(10500m, res.MontoPagado);
        Assert.Equal(500m, res.MontoVuelto);
    }

    [Fact]
    public async Task ProcesarPago_PagoMixtoDescuadrado_FallaValidacion()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var admin = await context.Usuarios.FirstAsync();
        var service = new PagoService(context);

        var request = new RegistrarPagoDto
        {
            MontoTotal = 5000m,
            UsuarioId = admin.Id,
            Metodos = new List<DetallePagoRequestDto>
            {
                new() { Metodo = MetodoPago.Efectivo, Monto = 2000m, MontoEntregado = 2000m },
                new() { Metodo = MetodoPago.Tarjeta, Monto = 2000m, TipoTarjeta = SubtipoTarjeta.Debito }
                // Total = 4000, pero MontoTotal = 5000
            }
        };

        // Act
        var res = await service.ProcesarPagoAsync(request);

        // Assert
        Assert.False(res.Exitoso);
        Assert.Contains(res.Errores, e => e.Contains("no coincide"));
    }
}
