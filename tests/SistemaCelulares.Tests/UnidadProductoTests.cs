using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Models;
using SistemaCelulares.Infrastructure.Data;
using SistemaCelulares.Infrastructure.Security;
using SistemaCelulares.Infrastructure.Services;
using Xunit;

namespace SistemaCelulares.Tests;

public class UnidadProductoTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task RegistrarUnidadImei_CreaUnidadEnStock()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var cat = new Categoria { Nombre = "Celulares", PrefijoSku = "CEL" };
        context.Categorias.Add(cat);
        await context.SaveChangesAsync();

        var prodService = new ProductoService(context);
        var prod = await prodService.CrearProductoAsync("Samsung S24", cat.Id, 45000m, 55000m, 0, 2, requiereSerie: true);

        // Act
        var u1 = await prodService.RegistrarUnidadImeiAsync(prod.Id, "350000000000001");
        var u2 = await prodService.RegistrarUnidadImeiAsync(prod.Id, "350000000000002");

        // Assert
        Assert.NotNull(u1);
        Assert.NotNull(u2);
        Assert.Equal(EstadoUnidadProducto.EnStock, u1.Estado);
        Assert.Equal(EstadoUnidadProducto.EnStock, u2.Estado);

        // El stock del producto debe haberse actualizado
        var prodActualizado = await prodService.ObtenerPorIdAsync(prod.Id);
        Assert.NotNull(prodActualizado);
        Assert.Equal(2, prodActualizado.StockActual);
    }

    [Fact]
    public async Task RegistrarUnidadImei_ImeiDuplicado_LanzaExcepcion()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var cat = new Categoria { Nombre = "Celulares", PrefijoSku = "CEL" };
        context.Categorias.Add(cat);
        await context.SaveChangesAsync();

        var prodService = new ProductoService(context);
        var prod = await prodService.CrearProductoAsync("iPhone 15", cat.Id, 50000m, 62000m, 0, 2, requiereSerie: true);

        await prodService.RegistrarUnidadImeiAsync(prod.Id, "359999999999999");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await prodService.RegistrarUnidadImeiAsync(prod.Id, "359999999999999");
        });
    }

    [Fact]
    public async Task CompraCelular_ConImeis_GeneraUnidadesEnStock()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var admin = await context.Usuarios.FirstAsync();
        var cat = await context.Categorias.FirstAsync();

        var prov = new Proveedor { Nombre = "Distribuidor Movil" };
        context.Proveedores.Add(prov);
        await context.SaveChangesAsync();

        var prodService = new ProductoService(context);
        var celular = await prodService.CrearProductoAsync("Motorola Edge 40", cat.Id, 18000m, 24000m, 0, 1, requiereSerie: true);

        var compraService = new CompraService(context);
        var itemCompra = new ItemCompraDto(celular.Id, 2, 18000m, new List<string> { "354444444444441", "354444444444442" });

        // Act
        var compra = await compraService.RegistrarCompraAsync(prov.Id, admin.Id, "FAC-COMPRA-01", null, new List<ItemCompraDto> { itemCompra });

        // Assert
        Assert.NotNull(compra);
        var prodDb = await context.Productos.FindAsync(celular.Id);
        Assert.Equal(2, prodDb!.StockActual);

        var unidadesDb = await context.UnidadesProducto.Where(u => u.ProductoId == celular.Id).ToListAsync();
        Assert.Equal(2, unidadesDb.Count);
        Assert.All(unidadesDb, u => Assert.Equal(EstadoUnidadProducto.EnStock, u.Estado));
    }

    [Fact]
    public async Task VentaCelular_ConImei_PasaAEstadoVendido()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var cajero = await context.Usuarios.FirstAsync(u => u.NombreUsuario == "cajero");
        var cat = await context.Categorias.FirstAsync();

        var cliente = new Cliente { NombreCompleto = "Cliente Comprador", Telefono = "809-555-1234" };
        context.Clientes.Add(cliente);
        await context.SaveChangesAsync();

        var turno = new Turno
        {
            UsuarioAperturaId = cajero.Id,
            MontoApertura = 500m,
            MontoEsperado = 500m,
            Estado = TurnoEstado.Abierto,
            FechaApertura = DateTime.UtcNow
        };
        context.Turnos.Add(turno);
        await context.SaveChangesAsync();

        var prodService = new ProductoService(context);
        var celular = await prodService.CrearProductoAsync("Samsung A15", cat.Id, 8000m, 11000m, 0, 1, requiereSerie: true);
        await prodService.RegistrarUnidadImeiAsync(celular.Id, "358888888888888");

        var pagoService = new PagoService(context);
        var ventaService = new VentaService(context, pagoService);

        var request = new RegistrarVentaRequestDto
        {
            ClienteId = cliente.Id,
            UsuarioId = cajero.Id,
            TurnoId = turno.Id,
            TipoComprobante = TipoComprobanteFiscal.Consumo_B02,
            NombreCliente = cliente.NombreCompleto,
            Items = new List<ItemCarritoVentaDto>
            {
                new()
                {
                    ProductoId = celular.Id,
                    NombreProducto = celular.Nombre,
                    Sku = celular.Sku,
                    Cantidad = 1,
                    PrecioUnitario = 11000m,
                    CostoUnitario = 8000m,
                    AplicaItbis = false,
                    RequiereSerie = true,
                    Imei = "358888888888888"
                }
            },
            PagoRequest = new RegistrarPagoDto
            {
                MontoTotal = 11000m,
                UsuarioId = cajero.Id,
                TurnoId = turno.Id,
                Metodos = new List<DetallePagoRequestDto>
                {
                    new()
                    {
                        Metodo = MetodoPago.Efectivo,
                        Monto = 11000m,
                        MontoEntregado = 11000m
                    }
                }
            }
        };

        // Act
        var resultado = await ventaService.RegistrarVentaAsync(request);

        // Assert
        Assert.True(resultado.Exitoso);
        var unidad = await context.UnidadesProducto.FirstAsync(u => u.Imei == "358888888888888");
        Assert.Equal(EstadoUnidadProducto.Vendido, unidad.Estado);
        Assert.Equal(resultado.VentaId, unidad.VentaId);
        Assert.NotNull(unidad.FechaVenta);

        var ventaDb = await context.Ventas.Include(v => v.Detalles).FirstAsync(v => v.Id == resultado.VentaId);
        var detalle = ventaDb.Detalles.First();
        Assert.Equal("358888888888888", detalle.Imei);
        Assert.Equal(unidad.Id, detalle.UnidadProductoId);

        // Anular venta debe devolver IMEI a EnStock
        await ventaService.AnularVentaAsync(resultado.VentaId!.Value, "Cliente devolvió el equipo", cajero.Id);
        var unidadCancelada = await context.UnidadesProducto.FirstAsync(u => u.Imei == "358888888888888");
        Assert.Equal(EstadoUnidadProducto.EnStock, unidadCancelada.Estado);
        Assert.Null(unidadCancelada.VentaId);
    }
}
