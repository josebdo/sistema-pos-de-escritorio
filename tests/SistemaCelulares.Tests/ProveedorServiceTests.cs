using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Infrastructure.Data;
using SistemaCelulares.Infrastructure.Security;
using SistemaCelulares.Infrastructure.Services;
using Xunit;

namespace SistemaCelulares.Tests;

public class ProveedorServiceTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CrearProveedor_DatosValidos_RegistraCorrectamente()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var provService = new ProveedorService(context);

        // Act
        var prov = await provService.CrearProveedorAsync(
            "Global Phone Supply SRL",
            "130-99887-1",
            "809-555-1122",
            "info@globalphone.do",
            "Santo Domingo",
            "Ana Gómez"
        );

        // Assert
        Assert.NotNull(prov);
        Assert.Equal("Global Phone Supply SRL", prov.Nombre);
        Assert.Equal("130-99887-1", prov.Rnc);
        Assert.True(prov.Activo);
    }

    [Fact]
    public async Task CrearProveedor_RncDuplicado_LanzaExcepcion()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var provService = new ProveedorService(context);

        // Act & Assert - RNC de Distribuidora Celular Dominicana SRL existente en seed
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provService.CrearProveedorAsync(
                "Proveedor Duplicado",
                "101-84920-1"
            )
        );
    }

    [Fact]
    public async Task ActualizarProveedor_RncDuplicadoEnOtroProveedor_LanzaExcepcion()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var provService = new ProveedorService(context);

        var prov1 = await context.Proveedores.FirstAsync(p => p.Rnc == "101-84920-1");
        var prov2 = await context.Proveedores.FirstAsync(p => p.Rnc == "131-72948-2");

        // Act & Assert - Intentar poner el RNC del prov1 en prov2
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provService.ActualizarProveedorAsync(
                prov2.Id,
                prov2.Nombre,
                rnc: "101-84920-1"
            )
        );
    }

    [Fact]
    public async Task ObtenerProveedores_FiltroBusqueda_RetornaCoincidencias()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var provService = new ProveedorService(context);

        // Act
        var resultados = await provService.ObtenerProveedoresAsync(busqueda: "Tech Import");

        // Assert
        Assert.NotEmpty(resultados);
        Assert.Contains(resultados, p => p.Nombre.Contains("Tech Import"));
    }

    [Fact]
    public async Task CambiarEstadoActivo_DesactivarProveedor_PreservaParaTrazabilidad()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var provService = new ProveedorService(context);

        var prov = await context.Proveedores.FirstAsync();

        // Act
        var ok = await provService.CambiarEstadoActivoAsync(prov.Id, false);

        // Assert
        Assert.True(ok);
        var desactivado = await provService.ObtenerPorIdAsync(prov.Id);
        Assert.NotNull(desactivado);
        Assert.False(desactivado.Activo);

        // No debe aparecer al listar solo activos
        var activos = await provService.ObtenerProveedoresAsync(soloActivos: true);
        Assert.DoesNotContain(activos, p => p.Id == prov.Id);
    }
}
