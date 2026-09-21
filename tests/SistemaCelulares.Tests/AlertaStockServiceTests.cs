using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Models;
using SistemaCelulares.Infrastructure.Data;
using SistemaCelulares.Infrastructure.Security;
using SistemaCelulares.Infrastructure.Services;
using Xunit;

namespace SistemaCelulares.Tests;

public class AlertaStockServiceTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task ObtenerAlertasStock_DetectaAgotadosCriticosYBajos()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var alertaService = new AlertaStockService(context);

        var cat = await context.Categorias.FirstAsync();

        // 1. Producto Agotado (Stock = 0, Min = 3)
        context.Productos.Add(new Producto
        {
            Nombre = "Funda Agotada",
            Sku = "TEST-0001",
            CategoriaId = cat.Id,
            StockActual = 0,
            CantidadMinima = 3,
            PrecioCosto = 150m,
            PrecioVenta = 300m,
            Activo = true
        });

        // 2. Producto Crítico (Stock = 1, Min = 4)
        context.Productos.Add(new Producto
        {
            Nombre = "Cargador Crítico",
            Sku = "TEST-0002",
            CategoriaId = cat.Id,
            StockActual = 1,
            CantidadMinima = 4,
            PrecioCosto = 400m,
            PrecioVenta = 800m,
            Activo = true
        });

        // 3. Producto Bajo (Stock = 2, Min = 2)
        context.Productos.Add(new Producto
        {
            Nombre = "Cable Bajo",
            Sku = "TEST-0003",
            CategoriaId = cat.Id,
            StockActual = 2,
            CantidadMinima = 2,
            PrecioCosto = 200m,
            PrecioVenta = 450m,
            Activo = true
        });

        // 4. Producto Normal (Stock = 10, Min = 2) - NO DEBE ALERTAR
        context.Productos.Add(new Producto
        {
            Nombre = "Teléfono Normal",
            Sku = "TEST-0004",
            CategoriaId = cat.Id,
            StockActual = 10,
            CantidadMinima = 2,
            PrecioCosto = 10000m,
            PrecioVenta = 15000m,
            Activo = true
        });

        await context.SaveChangesAsync();

        // Act
        var alertas = await alertaService.ObtenerAlertasStockAsync();

        // Assert
        Assert.NotEmpty(alertas);

        var agotado = alertas.FirstOrDefault(a => a.Sku == "TEST-0001");
        Assert.NotNull(agotado);
        Assert.Equal(NivelCriticidadStock.Agotado, agotado.Criticidad);
        Assert.Equal(3, agotado.UnidadesFaltantes);
        Assert.Equal(450m, agotado.InversionReposicion); // 3 * 150

        var critico = alertas.FirstOrDefault(a => a.Sku == "TEST-0002");
        Assert.NotNull(critico);
        Assert.Equal(NivelCriticidadStock.Critico, critico.Criticidad);
        Assert.Equal(3, critico.UnidadesFaltantes); // 4 - 1
        Assert.Equal(1200m, critico.InversionReposicion); // 3 * 400

        var bajo = alertas.FirstOrDefault(a => a.Sku == "TEST-0003");
        Assert.NotNull(bajo);
        Assert.Equal(NivelCriticidadStock.Bajo, bajo.Criticidad);

        Assert.DoesNotContain(alertas, a => a.Sku == "TEST-0004");
    }

    [Fact]
    public async Task ContarProductosCriticos_RetornaConteoExacto()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var alertaService = new AlertaStockService(context);

        var cat = await context.Categorias.FirstAsync();

        // En seed hay 3 productos con stock suficiente. Agregamos 2 críticos:
        context.Productos.Add(new Producto { Nombre = "P1", Sku = "P1", CategoriaId = cat.Id, StockActual = 1, CantidadMinima = 3, Activo = true });
        context.Productos.Add(new Producto { Nombre = "P2", Sku = "P2", CategoriaId = cat.Id, StockActual = 0, CantidadMinima = 2, Activo = true });
        context.Productos.Add(new Producto { Nombre = "P3 Inactivo", Sku = "P3", CategoriaId = cat.Id, StockActual = 0, CantidadMinima = 2, Activo = false }); // Inactivo no cuenta
        await context.SaveChangesAsync();

        // Act
        var conteo = await alertaService.ContarProductosCriticosAsync();

        // Assert
        Assert.Equal(2, conteo);
    }
}
