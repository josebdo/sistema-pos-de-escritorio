using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Models;
using SistemaCelulares.Infrastructure.Data;
using SistemaCelulares.Infrastructure.Security;
using SistemaCelulares.Infrastructure.Services;
using Xunit;

namespace SistemaCelulares.Tests;

public class MultiCajaMigrationTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void ConfiguracionRed_Default_EsCajaUnicaLocal()
    {
        // Arrange
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var service = new ConfiguracionRedService(tempDir);

            // Act
            var config = service.ObtenerConfiguracion();

            // Assert
            Assert.Equal(ModoOperacionCaja.CajaUnicaLocal, config.ModoOperacion);
            Assert.Equal(1, config.CajaId);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ConfiguracionRed_GuardarYRecuperar_PersisteCorrectamente()
    {
        // Arrange
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var service = new ConfiguracionRedService(tempDir);
            var nuevaConfig = new ConfiguracionRedDto
            {
                ModoOperacion = ModoOperacionCaja.CajaClienteLan,
                CajaId = 3,
                NombreCaja = "Caja Secundaria 3",
                ServidorIpOHost = "192.168.1.100",
                Puerto = 1433
            };

            // Act
            await service.GuardarConfiguracionAsync(nuevaConfig);
            var recuperada = service.ObtenerConfiguracion();

            // Assert
            Assert.Equal(ModoOperacionCaja.CajaClienteLan, recuperada.ModoOperacion);
            Assert.Equal(3, recuperada.CajaId);
            Assert.Equal("Caja Secundaria 3", recuperada.NombreCaja);
            Assert.Equal("192.168.1.100", recuperada.ServidorIpOHost);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GenerarSnapshot_ConDatosIniciales_GeneraSnapshotCompletoConHashValido()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var migrationService = new DataMigrationService(context);

        // Act
        var snapshot = await migrationService.GenerarSnapshotAsync();

        // Assert
        Assert.NotNull(snapshot);
        Assert.NotEmpty(snapshot.HashSha256);
        Assert.NotEmpty(snapshot.Roles);
        Assert.NotEmpty(snapshot.Usuarios);
        Assert.NotEmpty(snapshot.Categorias);
        Assert.NotEmpty(snapshot.Productos);
        Assert.NotEmpty(snapshot.Proveedores);
        Assert.NotEmpty(snapshot.CategoriasFinancieras);

        string hashCalculado = migrationService.CalcularHash(snapshot);
        Assert.Equal(hashCalculado, snapshot.HashSha256);
    }

    [Fact]
    public async Task ImportarSnapshot_EnBaseDeDatosVacia_ImportaTodosLosRegistrosConExito()
    {
        // Arrange
        using var contextOrigen = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(contextOrigen, hasher);
        var migrationOrigen = new DataMigrationService(contextOrigen);
        var snapshot = await migrationOrigen.GenerarSnapshotAsync();

        using var contextDestino = GetInMemoryDbContext();
        var migrationDestino = new DataMigrationService(contextDestino);

        // Act
        bool resultado = await migrationDestino.ImportarSnapshotAsync(snapshot);

        // Assert
        Assert.True(resultado);

        var categoriasDestino = await contextDestino.Categorias.ToListAsync();
        var productosDestino = await contextDestino.Productos.ToListAsync();
        var proveedoresDestino = await contextDestino.Proveedores.ToListAsync();
        var catFinancierasDestino = await contextDestino.CategoriasFinancieras.ToListAsync();

        Assert.Equal(snapshot.Categorias.Count, categoriasDestino.Count);
        Assert.Equal(snapshot.Productos.Count, productosDestino.Count);
        Assert.Equal(snapshot.Proveedores.Count, proveedoresDestino.Count);
        Assert.Equal(snapshot.CategoriasFinancieras.Count, catFinancierasDestino.Count);
    }

    [Fact]
    public async Task ImportarSnapshot_ConHashCorrupto_RechazaImportacionPorSeguridad()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var migrationService = new DataMigrationService(context);
        var snapshot = await migrationService.GenerarSnapshotAsync();

        // Corromper el hash
        snapshot.HashSha256 = "hash_invalido_alterado_123456789";

        using var contextDestino = GetInMemoryDbContext();
        var migrationDestino = new DataMigrationService(contextDestino);

        // Act
        bool resultado = await migrationDestino.ImportarSnapshotAsync(snapshot);

        // Assert
        Assert.False(resultado);
        Assert.Empty(await contextDestino.Productos.ToListAsync());
    }

    [Fact]
    public async Task ImportarSnapshot_ConProductosExistentesYSobreescribir_ActualizaPreciosYStock()
    {
        // Arrange
        using var contextOrigen = GetInMemoryDbContext();
        var cat = new Categoria { Nombre = "Celulares", PrefijoSku = "CEL" };
        contextOrigen.Categorias.Add(cat);
        await contextOrigen.SaveChangesAsync();

        var prod = new Producto
        {
            Nombre = "iPhone 15 Pro",
            Sku = "CEL-0001",
            PrecioCosto = 55000m,
            PrecioVenta = 68000m,
            StockActual = 10,
            CategoriaId = cat.Id
        };
        contextOrigen.Productos.Add(prod);
        await contextOrigen.SaveChangesAsync();

        var migrationOrigen = new DataMigrationService(contextOrigen);
        var snapshot = await migrationOrigen.GenerarSnapshotAsync();

        // En el destino, el producto existe pero con precio y stock viejo
        using var contextDestino = GetInMemoryDbContext();
        var catDestino = new Categoria { Nombre = "Celulares", PrefijoSku = "CEL" };
        contextDestino.Categorias.Add(catDestino);
        await contextDestino.SaveChangesAsync();

        contextDestino.Productos.Add(new Producto
        {
            Nombre = "iPhone 15 Pro",
            Sku = "CEL-0001",
            PrecioCosto = 50000m,
            PrecioVenta = 60000m,
            StockActual = 2,
            CategoriaId = catDestino.Id
        });
        await contextDestino.SaveChangesAsync();

        var migrationDestino = new DataMigrationService(contextDestino);

        // Act
        bool resultado = await migrationDestino.ImportarSnapshotAsync(snapshot, sobreescribirExistente: true);

        // Assert
        Assert.True(resultado);
        var prodActualizado = await contextDestino.Productos.FirstAsync(p => p.Sku == "CEL-0001");
        Assert.Equal(55000m, prodActualizado.PrecioCosto);
        Assert.Equal(68000m, prodActualizado.PrecioVenta);
        Assert.Equal(10, prodActualizado.StockActual);
    }

    [Fact]
    public async Task ProbarConexion_CajaUnicaLocal_RetornaTrueInmediatamente()
    {
        // Arrange
        var service = new ConfiguracionRedService();
        var config = new ConfiguracionRedDto { ModoOperacion = ModoOperacionCaja.CajaUnicaLocal };

        // Act
        bool res = await service.ProbarConexionAsync(config);

        // Assert
        Assert.True(res);
    }
}
