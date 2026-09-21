using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Infrastructure.Data;
using SistemaCelulares.Infrastructure.Security;
using SistemaCelulares.Infrastructure.Services;
using Xunit;

namespace SistemaCelulares.Tests;

public class LecturaCodigoBarrasTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task BuscarPorCodigoBarrasOSku_ConCodigoBarrasExistente_RetornaProductoCorrecto()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var productoService = new ProductoService(context);

        // Act - Samsung Galaxy A54 tiene CodigoBarras = "7421001234567"
        var producto = await productoService.BuscarPorCodigoBarrasOSkuAsync("7421001234567");

        // Assert
        Assert.NotNull(producto);
        Assert.Equal("CEL-0001", producto.Sku);
        Assert.Equal("Samsung Galaxy A54 5G 128GB", producto.Nombre);
        Assert.Equal("7421001234567", producto.CodigoBarras);
        Assert.NotNull(producto.Categoria);
    }

    [Fact]
    public async Task BuscarPorCodigoBarrasOSku_ConSkuExistente_RetornaProductoCorrecto()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var productoService = new ProductoService(context);

        // Act - Búsqueda por SKU directo
        var producto = await productoService.BuscarPorCodigoBarrasOSkuAsync("CEL-0002");

        // Assert
        Assert.NotNull(producto);
        Assert.Equal("CEL-0002", producto.Sku);
        Assert.Equal("iPhone 13 128GB Midnight", producto.Nombre);
    }

    [Fact]
    public async Task BuscarPorCodigoBarrasOSku_ConCodigoInexistente_RetornaNull()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var productoService = new ProductoService(context);

        // Act
        var producto = await productoService.BuscarPorCodigoBarrasOSkuAsync("9999999999999");

        // Assert
        Assert.Null(producto);
    }

    [Fact]
    public async Task BuscarPorCodigoBarrasOSku_ConEspaciosYMinusculas_NormalizaYEncuentra()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var productoService = new ProductoService(context);

        // Act - Código de cargador rápido con espacios
        var producto = await productoService.BuscarPorCodigoBarrasOSkuAsync("  8806090558122  ");

        // Assert
        Assert.NotNull(producto);
        Assert.Equal("CAR-0001", producto.Sku);
        Assert.Equal("Cargador Rápido 25W Tipo C", producto.Nombre);
    }

    [Fact]
    public async Task CrearProducto_ConCodigoBarrasDuplicado_LanzaExcepcion()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var productoService = new ProductoService(context);

        var categoria = await context.Categorias.FirstAsync();

        // Act & Assert - Intentar registrar producto con código ya existente "7421001234567"
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            productoService.CrearProductoAsync(
                "Samsung Clon",
                categoria.Id,
                1000m,
                1500m,
                5,
                1,
                sku: null,
                codigoBarras: "7421001234567"
            ));
    }
}
