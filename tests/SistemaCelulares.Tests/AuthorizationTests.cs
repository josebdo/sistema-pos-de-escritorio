using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Constants;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Models;
using SistemaCelulares.Infrastructure.Data;
using SistemaCelulares.Infrastructure.Security;
using SistemaCelulares.Infrastructure.Services;
using Xunit;

namespace SistemaCelulares.Tests;

public class AuthorizationTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void SuperAdmin_TieneAccesoATodosLosPermisosPorDefinicion()
    {
        // Arrange
        var sesionSuperAdmin = new SesionUsuario(1, "Super Admin", "superadmin", 1, Rol.SuperAdmin, Array.Empty<string>());

        // Act & Assert
        Assert.True(sesionSuperAdmin.TienePermiso(Permisos.UsuariosCrear));
        Assert.True(sesionSuperAdmin.TienePermiso(Permisos.RolesEliminar));
        Assert.True(sesionSuperAdmin.TienePermiso(Permisos.FinanzasReportesVer));
        Assert.True(sesionSuperAdmin.TienePermiso("CualquierPermisoFuturo"));
    }

    [Fact]
    public void Cajero_SoloTienePermisosDeCajaYVentas()
    {
        // Arrange
        var permisosCajero = new[]
        {
            Permisos.TurnosAbrir,
            Permisos.TurnosCerrar,
            Permisos.ProductosVer,
            Permisos.VentasRegistrar,
            Permisos.VentasHistorial
        };

        var sesionCajero = new SesionUsuario(3, "Cajero Juan", "cajero1", 3, Rol.Cajero, permisosCajero);

        // Act & Assert
        Assert.True(sesionCajero.TienePermiso(Permisos.VentasRegistrar));
        Assert.True(sesionCajero.TienePermiso(Permisos.TurnosAbrir));
        Assert.False(sesionCajero.TienePermiso(Permisos.UsuariosCrear));
        Assert.False(sesionCajero.TienePermiso(Permisos.FinanzasReportesVer));
        Assert.False(sesionCajero.TienePermiso(Permisos.RolesEliminar));
    }

    [Fact]
    public async Task RolService_CrearRolPersonalizado_AsignaPermisosCorrectamente()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var rolService = new RolService(context);

        var permisosSupervisor = new[] { Permisos.ProductosVer, Permisos.ProductosCrear, Permisos.AlertasStockVer };

        // Act
        var nuevoRol = await rolService.CrearRolPersonalizadoAsync("Supervisor Inventario", "Encargado de stock", permisosSupervisor);

        // Assert
        Assert.NotNull(nuevoRol);
        Assert.Equal("Supervisor Inventario", nuevoRol.Nombre);
        Assert.False(nuevoRol.EsFijo);
        Assert.Equal(3, nuevoRol.RolPermisos.Count);
    }

    [Fact]
    public async Task RolService_NoPermiteEliminarRolFijo()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var rolService = new RolService(context);

        var rolAdmin = await context.Roles.FirstAsync(r => r.Nombre == Rol.Admin);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => rolService.EliminarRolAsync(rolAdmin.Id));
    }
}
