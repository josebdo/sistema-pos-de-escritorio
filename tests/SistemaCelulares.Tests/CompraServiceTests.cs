using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Models;
using SistemaCelulares.Infrastructure.Data;
using SistemaCelulares.Infrastructure.Security;
using SistemaCelulares.Infrastructure.Services;
using Xunit;

namespace SistemaCelulares.Tests;

public class CompraServiceTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task RegistrarCompra_IncrementaStockYActualizaUltimoCosto_SinModificarPrecioVenta()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var compraService = new CompraService(context);

        var admin = await context.Usuarios.FirstAsync(u => u.NombreUsuario == "admin");
        var proveedor = await context.Proveedores.FirstAsync();
        var producto = await context.Productos.FirstAsync(p => p.Sku == "CEL-0001");

        var stockInicial = producto.StockActual; // 8
        var costoAnterior = producto.PrecioCosto; // 14500
        var precioVentaOriginal = producto.PrecioVenta; // 19500

        var nuevoCosto = 15200.00m;
        var cantidadComprada = 5;

        var items = new List<ItemCompraDto>
        {
            new(producto.Id, cantidadComprada, nuevoCosto)
        };

        // Act
        var compra = await compraService.RegistrarCompraAsync(
            proveedor.Id,
            admin.Id,
            "FACT-2026-001",
            "Compra de lote de smartphones",
            items
        );

        // Assert
        Assert.NotNull(compra);
        Assert.Equal(nuevoCosto * cantidadComprada, compra.Total);
        Assert.Single(compra.Detalles);

        // Verificar el estado del producto
        var prodActualizado = await context.Productos.FindAsync(producto.Id);
        Assert.NotNull(prodActualizado);
        Assert.Equal(stockInicial + cantidadComprada, prodActualizado.StockActual);
        Assert.Equal(nuevoCosto, prodActualizado.PrecioCosto); // Último costo actualizado
        Assert.Equal(precioVentaOriginal, prodActualizado.PrecioVenta); // Precio de venta INTACTO
    }

    [Fact]
    public async Task RegistrarCompra_ConMultiplesItems_CalculaTotalCorrectamente()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var compraService = new CompraService(context);

        var admin = await context.Usuarios.FirstAsync(u => u.NombreUsuario == "admin");
        var proveedor = await context.Proveedores.FirstAsync();

        var prod1 = await context.Productos.FirstAsync(p => p.Sku == "CEL-0001");
        var prod2 = await context.Productos.FirstAsync(p => p.Sku == "CAR-0001");

        var items = new List<ItemCompraDto>
        {
            new(prod1.Id, 2, 14000m), // Subtotal: 28000
            new(prod2.Id, 10, 400m)    // Subtotal: 4000
        };

        // Act
        var compra = await compraService.RegistrarCompraAsync(
            proveedor.Id,
            admin.Id,
            "FAC-999",
            "Compra múltiple",
            items
        );

        // Assert
        Assert.Equal(32000m, compra.Total);
        Assert.Equal(2, compra.Detalles.Count);
    }

    [Fact]
    public async Task RegistrarCompra_SinItems_LanzaExcepcion()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var compraService = new CompraService(context);

        var admin = await context.Usuarios.FirstAsync(u => u.NombreUsuario == "admin");
        var proveedor = await context.Proveedores.FirstAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            compraService.RegistrarCompraAsync(proveedor.Id, admin.Id, "FAC-001", null, new List<ItemCompraDto>()));
    }

    [Fact]
    public async Task ObtenerHistorialComprasPorProducto_RetornaDetallesCorrectos()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var compraService = new CompraService(context);

        var admin = await context.Usuarios.FirstAsync(u => u.NombreUsuario == "admin");
        var proveedor = await context.Proveedores.FirstAsync();
        var prod = await context.Productos.FirstAsync(p => p.Sku == "CAR-0001");

        await compraService.RegistrarCompraAsync(proveedor.Id, admin.Id, "FAC-A", null, new[] { new ItemCompraDto(prod.Id, 5, 420m) });
        await compraService.RegistrarCompraAsync(proveedor.Id, admin.Id, "FAC-B", null, new[] { new ItemCompraDto(prod.Id, 8, 410m) });

        // Act
        var historial = await compraService.ObtenerHistorialComprasPorProductoAsync(prod.Id);

        // Assert
        Assert.Equal(2, historial.Count);
        Assert.Equal(410m, historial[0].CostoUnitario); // La más reciente primero
        Assert.Equal(420m, historial[1].CostoUnitario);
    }
}
