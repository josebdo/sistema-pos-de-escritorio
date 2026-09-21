using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Models;
using SistemaCelulares.Infrastructure.Data;
using SistemaCelulares.Infrastructure.Security;
using SistemaCelulares.Infrastructure.Services;
using Xunit;

namespace SistemaCelulares.Tests;

public class FinanzasServiceTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task RegistrarMovimiento_GastoEIngreso_CreaMovimientoYValidaMonto()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var finanzasService = new FinanzasService(context);

        var admin = await context.Usuarios.FirstAsync(u => u.NombreUsuario == "admin");
        var catLuz = await context.CategoriasFinancieras.FirstAsync(c => c.Nombre == "Electricidad / Luz");
        var catTecnico = await context.CategoriasFinancieras.FirstAsync(c => c.Nombre == "Servicio Técnico y Reparaciones");

        // Act - Registrar Gasto
        var gasto = await finanzasService.RegistrarMovimientoAsync(
            TipoMovimientoFinanciero.Gasto,
            catLuz.Id,
            4500.00m,
            DateTime.Today,
            "Pago de factura eléctrica EDESUR",
            admin.Id,
            "FAC-EDE-8812"
        );

        // Act - Registrar Ingreso
        var ingreso = await finanzasService.RegistrarMovimientoAsync(
            TipoMovimientoFinanciero.Ingreso,
            catTecnico.Id,
            2500.00m,
            DateTime.Today,
            "Cambio de pantalla iPhone 11",
            admin.Id,
            "REC-001"
        );

        // Assert
        Assert.NotNull(gasto);
        Assert.Equal(4500.00m, gasto.Monto);
        Assert.Equal(TipoMovimientoFinanciero.Gasto, gasto.Tipo);

        Assert.NotNull(ingreso);
        Assert.Equal(2500.00m, ingreso.Monto);
        Assert.Equal(TipoMovimientoFinanciero.Ingreso, ingreso.Tipo);

        var todos = await finanzasService.ObtenerMovimientosAsync();
        Assert.Equal(2, todos.Count);
    }

    [Fact]
    public async Task RegistrarMovimiento_MontoInvalidoODescripcionVacia_LanzaExcepcion()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var finanzasService = new FinanzasService(context);

        var admin = await context.Usuarios.FirstAsync(u => u.NombreUsuario == "admin");
        var cat = await context.CategoriasFinancieras.FirstAsync(c => c.Tipo == TipoMovimientoFinanciero.Gasto);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            finanzasService.RegistrarMovimientoAsync(TipoMovimientoFinanciero.Gasto, cat.Id, -50m, DateTime.Today, "Concepto", admin.Id));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            finanzasService.RegistrarMovimientoAsync(TipoMovimientoFinanciero.Gasto, cat.Id, 100m, DateTime.Today, "   ", admin.Id));
    }

    [Fact]
    public async Task RegistrarMovimiento_TipoIncompatibleConCategoria_LanzaExcepcion()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var finanzasService = new FinanzasService(context);

        var admin = await context.Usuarios.FirstAsync(u => u.NombreUsuario == "admin");
        var catGasto = await context.CategoriasFinancieras.FirstAsync(c => c.Tipo == TipoMovimientoFinanciero.Gasto);

        // Intentar registrar como Ingreso en categoría de Gasto
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            finanzasService.RegistrarMovimientoAsync(TipoMovimientoFinanciero.Ingreso, catGasto.Id, 1000m, DateTime.Today, "Ingreso inválido", admin.Id));
    }

    [Fact]
    public async Task ObtenerBalanceNetoPeriodo_CalculaIngresosGastosComprasYUtilidadCorrectamente()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);

        var finanzasService = new FinanzasService(context);
        var compraService = new CompraService(context);

        var admin = await context.Usuarios.FirstAsync(u => u.NombreUsuario == "admin");
        var proveedor = await context.Proveedores.FirstAsync();
        var producto = await context.Productos.FirstAsync();

        var catGasto = await context.CategoriasFinancieras.FirstAsync(c => c.Tipo == TipoMovimientoFinanciero.Gasto);
        var catIngreso = await context.CategoriasFinancieras.FirstAsync(c => c.Tipo == TipoMovimientoFinanciero.Ingreso);

        var hoy = DateTime.Today;

        // 1. Registrar gasto operativo RD$ 3,000
        await finanzasService.RegistrarMovimientoAsync(TipoMovimientoFinanciero.Gasto, catGasto.Id, 3000m, hoy, "Gasto operativo", admin.Id);

        // 2. Registrar ingreso adicional RD$ 10,000
        await finanzasService.RegistrarMovimientoAsync(TipoMovimientoFinanciero.Ingreso, catIngreso.Id, 10000m, hoy, "Servicio técnico", admin.Id);

        // 3. Registrar compra de mercancía RD$ 4,000 (2 unidades a 2,000)
        await compraService.RegistrarCompraAsync(proveedor.Id, admin.Id, "FAC-TEST", "Compra test", new[] { new ItemCompraDto(producto.Id, 2, 2000m) });

        // Act
        var balance = await finanzasService.ObtenerBalanceNetoPeriodoAsync(hoy.AddDays(-1), hoy.AddDays(1));

        // Assert
        Assert.Equal(10000m, balance.TotalOtrosIngresos);
        Assert.Equal(10000m, balance.TotalIngresosGlobales);
        Assert.Equal(3000m, balance.TotalGastosOperativos);
        Assert.Equal(4000m, balance.TotalComprasMercancia);
        Assert.Equal(7000m, balance.TotalGastos); // 3000 + 4000
        Assert.Equal(3000m, balance.BalanceNeto); // 10000 - 7000
        Assert.True(balance.GastosPorCategoria.ContainsKey(catGasto.Nombre));
        Assert.True(balance.GastosPorCategoria.ContainsKey("Compras de Inventario / Proveedores"));
    }

    [Fact]
    public async Task CrearCategoriaFinanciera_NombreDuplicadoMismoTipo_LanzaExcepcion()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var finanzasService = new FinanzasService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            finanzasService.CrearCategoriaFinancieraAsync("Alquiler de Local", TipoMovimientoFinanciero.Gasto));
    }

    [Fact]
    public async Task CambiarEstadoActivoCategoria_DesactivaCorrectamente()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var finanzasService = new FinanzasService(context);

        var cat = await context.CategoriasFinancieras.FirstAsync();

        // Act
        var ok = await finanzasService.CambiarEstadoActivoCategoriaAsync(cat.Id, false);
        var activas = await finanzasService.ObtenerCategoriasFinancierasAsync(soloActivas: true);

        // Assert
        Assert.True(ok);
        Assert.DoesNotContain(activas, c => c.Id == cat.Id);
    }
}
