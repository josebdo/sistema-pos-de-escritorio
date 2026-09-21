using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Infrastructure.Data;
using SistemaCelulares.Infrastructure.Security;
using SistemaCelulares.Infrastructure.Services;
using Xunit;

namespace SistemaCelulares.Tests;

public class TurnoServiceTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task AbrirTurnoAsync_CreaTurnoAbiertoCorrectamente()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var turnoService = new TurnoService(context);

        var cajero = await context.Usuarios.FirstAsync(u => u.NombreUsuario == "cajero");

        // Act
        var turno = await turnoService.AbrirTurnoAsync(cajero.Id, 2500.00m, "Apertura turno de la mañana");

        // Assert
        Assert.NotNull(turno);
        Assert.Equal(TurnoEstado.Abierto, turno.Estado);
        Assert.Equal(2500.00m, turno.MontoApertura);
        Assert.Equal(2500.00m, turno.MontoEsperado);
        Assert.Null(turno.FechaCierre);
        Assert.Null(turno.MontoCierre);
        Assert.True(await turnoService.TieneTurnoAbiertoAsync(cajero.Id));
    }

    [Fact]
    public async Task AbrirTurnoAsync_SiUsuarioYaTieneTurnoAbierto_LanzaExcepcion()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var turnoService = new TurnoService(context);
        var cajero = await context.Usuarios.FirstAsync(u => u.NombreUsuario == "cajero");

        await turnoService.AbrirTurnoAsync(cajero.Id, 1000m);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => turnoService.AbrirTurnoAsync(cajero.Id, 500m));
    }

    [Fact]
    public async Task AbrirTurnoAsync_MontoNegativo_LanzaExcepcion()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var turnoService = new TurnoService(context);
        var cajero = await context.Usuarios.FirstAsync(u => u.NombreUsuario == "cajero");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => turnoService.AbrirTurnoAsync(cajero.Id, -100m));
    }

    [Fact]
    public async Task CerrarTurnoAsync_CuadreExacto_CalculaDiferenciaCero()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var turnoService = new TurnoService(context);
        var cajero = await context.Usuarios.FirstAsync(u => u.NombreUsuario == "cajero");

        var turno = await turnoService.AbrirTurnoAsync(cajero.Id, 2000m);
        turno.TotalVentasEfectivo = 3500m; // Se simulan ventas
        await context.SaveChangesAsync();

        // Monto esperado: 2000 + 3500 = 5500
        // Act - Se entregan exactamente 5500
        var turnoCerrado = await turnoService.CerrarTurnoAsync(turno.Id, cajero.Id, 5500m, "Cuadre conforme");

        // Assert
        Assert.Equal(TurnoEstado.Cerrado, turnoCerrado.Estado);
        Assert.Equal(5500m, turnoCerrado.MontoEsperado);
        Assert.Equal(5500m, turnoCerrado.MontoCierre);
        Assert.Equal(0m, turnoCerrado.Diferencia);
        Assert.NotNull(turnoCerrado.FechaCierre);
    }

    [Fact]
    public async Task CerrarTurnoAsync_Faltante_CalculaDiferenciaNegativa()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var turnoService = new TurnoService(context);
        var cajero = await context.Usuarios.FirstAsync(u => u.NombreUsuario == "cajero");

        var turno = await turnoService.AbrirTurnoAsync(cajero.Id, 1000m);
        turno.TotalVentasEfectivo = 1000m; // Esperado = 2000
        await context.SaveChangesAsync();

        // Act - Cajero cuenta 1800 (Faltan 200)
        var turnoCerrado = await turnoService.CerrarTurnoAsync(turno.Id, cajero.Id, 1800m, "Faltan 200 pesos");

        // Assert
        Assert.Equal(2000m, turnoCerrado.MontoEsperado);
        Assert.Equal(1800m, turnoCerrado.MontoCierre);
        Assert.Equal(-200m, turnoCerrado.Diferencia);
    }

    [Fact]
    public async Task CerrarTurnoAsync_Sobrante_CalculaDiferenciaPositiva()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var turnoService = new TurnoService(context);
        var cajero = await context.Usuarios.FirstAsync(u => u.NombreUsuario == "cajero");

        var turno = await turnoService.AbrirTurnoAsync(cajero.Id, 1000m);
        turno.TotalVentasEfectivo = 1000m; // Esperado = 2000
        await context.SaveChangesAsync();

        // Act - Cajero cuenta 2150 (Sobran 150)
        var turnoCerrado = await turnoService.CerrarTurnoAsync(turno.Id, cajero.Id, 2150m, "Sobran 150 pesos");

        // Assert
        Assert.Equal(2000m, turnoCerrado.MontoEsperado);
        Assert.Equal(2150m, turnoCerrado.MontoCierre);
        Assert.Equal(150m, turnoCerrado.Diferencia);
    }

    [Fact]
    public async Task CerrarTurnoAsync_SiTurnoYaCerrado_LanzaExcepcion()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var turnoService = new TurnoService(context);
        var cajero = await context.Usuarios.FirstAsync(u => u.NombreUsuario == "cajero");

        var turno = await turnoService.AbrirTurnoAsync(cajero.Id, 1000m);
        await turnoService.CerrarTurnoAsync(turno.Id, cajero.Id, 1000m);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => turnoService.CerrarTurnoAsync(turno.Id, cajero.Id, 1000m));
    }
}
