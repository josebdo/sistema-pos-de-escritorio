using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Infrastructure.Data;
using SistemaCelulares.Infrastructure.Security;
using SistemaCelulares.Infrastructure.Services;
using Xunit;

namespace SistemaCelulares.Tests;

public class ProductoServiceTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CrearCategoria_GeneraPrefijoSkuAutomatico()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var catService = new CategoriaService(context);

        // Act
        var cat = await catService.CrearCategoriaAsync("Baterías y Pilas", "Repuestos de energía");

        // Assert
        Assert.NotNull(cat);
        Assert.Equal("BAT", cat.PrefijoSku);
    }

    [Fact]
    public async Task CrearProducto_SinSku_GeneraCorrelativoPorCategoria()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var prodService = new ProductoService(context);

        var catCel = await context.Categorias.FirstAsync(c => c.PrefijoSku == "CEL");

        // Act - Se crea un producto sin pasar SKU explícito (debe ser CEL-0003 ya que CEL-0001 y CEL-0002 existen en seed)
        var nuevo = await prodService.CrearProductoAsync(
            "Xiaomi Redmi Note 13 256GB",
            catCel.Id,
            10500m,
            14500m,
            10,
            3
        );

        // Assert
        Assert.NotNull(nuevo);
        Assert.Equal("CEL-0003", nuevo.Sku);
        Assert.True(nuevo.Activo);
    }

    [Fact]
    public async Task CrearProducto_SkuDuplicado_LanzaExcepcion()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var prodService = new ProductoService(context);

        var catCel = await context.Categorias.FirstAsync(c => c.PrefijoSku == "CEL");

        // Act & Assert - Intentar registrar con CEL-0001 que ya existe en el seed
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            prodService.CrearProductoAsync(
                "Celular Duplicado",
                catCel.Id,
                5000m,
                8000m,
                5,
                2,
                sku: "CEL-0001"
            )
        );
    }

    [Fact]
    public async Task CrearProducto_PrecioNegativo_LanzaExcepcion()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var prodService = new ProductoService(context);
        var catCel = await context.Categorias.FirstAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            prodService.CrearProductoAsync("Test Negativo", catCel.Id, -50m, 100m, 5, 2));
    }

    [Fact]
    public async Task CambiarEstadoActivo_DesactivarProducto_PreservaEntidadParaTrazabilidad()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var prodService = new ProductoService(context);

        var producto = await context.Productos.FirstAsync(p => p.Sku == "CEL-0001");

        // Act - Desactivar
        var resultado = await prodService.CambiarEstadoActivoAsync(producto.Id, false);

        // Assert
        Assert.True(resultado);

        // El producto sigue existiendo en base de datos pero inactivo
        var prodEnDb = await prodService.ObtenerPorIdAsync(producto.Id);
        Assert.NotNull(prodEnDb);
        Assert.False(prodEnDb.Activo);

        // Al consultar solo activos no debe aparecer
        var activos = await prodService.ObtenerProductosAsync(soloActivos: true);
        Assert.DoesNotContain(activos, p => p.Id == producto.Id);
    }

    [Fact]
    public async Task ObtenerProductosBajoStock_RetornaSoloAquellosEnOMenorQueCantidadMinima()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var prodService = new ProductoService(context);

        // Modificamos el stock de un producto para que esté por debajo del mínimo (ej. Stock 1, Min 2)
        var prod = await context.Productos.FirstAsync(p => p.Sku == "CEL-0001");
        prod.StockActual = 1;
        prod.CantidadMinima = 2;
        await context.SaveChangesAsync();

        // Act
        var bajoStock = await prodService.ObtenerProductosBajoStockAsync();

        // Assert
        Assert.NotEmpty(bajoStock);
        Assert.Contains(bajoStock, p => p.Sku == "CEL-0001");
    }

    [Fact]
    public async Task AjustarStock_ActualizaInventarioCorrectamente()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var prodService = new ProductoService(context);

        var prod = await context.Productos.FirstAsync(p => p.Sku == "CAR-0001");

        // Act
        var ajustado = await prodService.AjustarStockAsync(prod.Id, 45, "Recepción manual de almacén");

        // Assert
        Assert.True(ajustado);
        var actualizado = await prodService.ObtenerPorIdAsync(prod.Id);
        Assert.Equal(45, actualizado!.StockActual);
    }
}
