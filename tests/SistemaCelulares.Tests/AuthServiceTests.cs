using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Models;
using SistemaCelulares.Infrastructure.Data;
using SistemaCelulares.Infrastructure.Security;
using SistemaCelulares.Infrastructure.Services;
using Xunit;

namespace SistemaCelulares.Tests;

public class AuthServiceTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task LoginAsync_ConUsuarioNuevoConPasswordTemporal_DebeRetornarDebeCambiarPassword()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var authService = new AuthService(context, hasher);

        // Act - Intentar login con 'cajero' que tiene DebeCambiarPassword = true
        var result = await authService.LoginAsync("cajero", "Cajero123!");

        // Assert
        Assert.True(result.EsExitoso);
        Assert.Equal(LoginStatus.DebeCambiarPassword, result.Status);
        Assert.NotNull(result.Sesion);
        Assert.Equal("Cajero", result.Sesion.RolNombre);
    }

    [Fact]
    public async Task CambiarPasswordObligatorioAsync_DebeActualizarPasswordYDesactivarFlag()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var authService = new AuthService(context, hasher);

        var loginResult = await authService.LoginAsync("cajero", "Cajero123!");
        Assert.Equal(LoginStatus.DebeCambiarPassword, loginResult.Status);

        // Act - Cambiar contraseña
        var cambioExitoso = await authService.CambiarPasswordObligatorioAsync(loginResult.Sesion!.UsuarioId, "MiNuevaClaveSegura2026!");

        // Assert
        Assert.True(cambioExitoso);

        // Segundo intento de login con la nueva contraseña
        var nuevoLogin = await authService.LoginAsync("cajero", "MiNuevaClaveSegura2026!");
        Assert.Equal(LoginStatus.Exitoso, nuevoLogin.Status);

        // Verificar que el viejo password ya no funciona
        var viejoLogin = await authService.LoginAsync("cajero", "Cajero123!");
        Assert.Equal(LoginStatus.CredencialesInvalidas, viejoLogin.Status);
    }

    [Fact]
    public async Task ResetPasswordAsync_SuperAdminReseteaUsuario_DebeForzarDebeCambiarPassword()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var authService = new AuthService(context, hasher);

        var superAdmin = await context.Usuarios.FirstAsync(u => u.NombreUsuario == "superadmin");
        var admin = await context.Usuarios.FirstAsync(u => u.NombreUsuario == "admin");

        // Primero el admin cambia su clave
        await authService.CambiarPasswordObligatorioAsync(admin.Id, "AdminNuevaClave!");

        // Super Admin resetea la contraseña de Admin a una temporal
        var reseteado = await authService.ResetPasswordAsync(superAdmin.Id, admin.Id, "TemporalReset123!");
        Assert.True(reseteado);

        // Act - Admin intenta ingresar con la clave temporal
        var loginAdmin = await authService.LoginAsync("admin", "TemporalReset123!");

        // Assert
        Assert.Equal(LoginStatus.DebeCambiarPassword, loginAdmin.Status);
    }

    [Fact]
    public async Task LoginAsync_UsuarioInactivo_DebeRetornarUsuarioInactivo()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var authService = new AuthService(context, hasher);

        var cajero = await context.Usuarios.FirstAsync(u => u.NombreUsuario == "cajero");
        cajero.Activo = false;
        await context.SaveChangesAsync();

        // Act
        var result = await authService.LoginAsync("cajero", "Cajero123!");

        // Assert
        Assert.False(result.EsExitoso);
        Assert.Equal(LoginStatus.UsuarioInactivo, result.Status);
    }
}
