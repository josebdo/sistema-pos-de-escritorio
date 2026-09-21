using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Infrastructure.Data;
using SistemaCelulares.Infrastructure.Security;
using SistemaCelulares.Infrastructure.Services;
using Xunit;

namespace SistemaCelulares.Tests;

public class GeneracionCodigoBarrasTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Theory]
    [InlineData("400638133393", 1)] // 4006381333931
    [InlineData("742100123456", 1)] // 7421001234561
    [InlineData("200000000001", 5)] // 2000000000015
    [InlineData("880609055812", 2)] // 8806090558122
    public void CalcularDigitoVerificador_CalculaCorrectamenteSegunEstandardGs1(string primeros12, int digitoEsperado)
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var generator = new Ean13GeneratorService(context);

        // Act
        int digito = generator.CalcularDigitoVerificador(primeros12);

        // Assert
        Assert.Equal(digitoEsperado, digito);
    }

    [Theory]
    [InlineData("4006381333931", true)]
    [InlineData("8806090558122", true)]
    [InlineData("2000000000015", true)]
    [InlineData("4006381333932", false)] // Dígito verificador incorrecto
    [InlineData("123456", false)]        // Longitud incorrecta
    [InlineData("400638133393A", false)] // Caracteres no numéricos
    public void ValidarEan13_ConCodigosValidosEInvalidos_RetornaResultadoEsperado(string codigo, bool esperadoValido)
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var generator = new Ean13GeneratorService(context);

        // Act
        bool esValido = generator.ValidarEan13(codigo);

        // Assert
        Assert.Equal(esperadoValido, esValido);
    }

    [Fact]
    public async Task GenerarEan13Interno_PrefijoValido_GeneraCodigoDe13DigitosValidoSinColisiones()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var generator = new Ean13GeneratorService(context);

        // Act
        var ean13 = await generator.GenerarEan13InternoAsync(20);

        // Assert
        Assert.NotNull(ean13);
        Assert.Equal(13, ean13.Length);
        Assert.StartsWith("20", ean13);
        Assert.True(generator.ValidarEan13(ean13));
    }

    [Fact]
    public async Task GenerarEan13Interno_PrefijoInvalido_LanzaExcepcion()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var generator = new Ean13GeneratorService(context);

        // Act & Assert (Rango válido 20-29)
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            generator.GenerarEan13InternoAsync(15));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            generator.GenerarEan13InternoAsync(35));
    }

    [Fact]
    public async Task GenerarEan13Interno_MultiplesGeneraciones_IncrementaCorrelativoYNoColisiona()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        await DbInitializer.InitializeAsync(context, hasher);
        var generator = new Ean13GeneratorService(context);
        var productoService = new ProductoService(context);

        var categoria = await context.Categorias.FirstAsync();

        // Act - Generar 1er código y registrar producto
        var ean1 = await generator.GenerarEan13InternoAsync(20);
        await productoService.CrearProductoAsync("Prod 1", categoria.Id, 100m, 200m, 5, 1, sku: null, codigoBarras: ean1);

        // Act - Generar 2do código y registrar producto
        var ean2 = await generator.GenerarEan13InternoAsync(20);
        await productoService.CrearProductoAsync("Prod 2", categoria.Id, 100m, 200m, 5, 1, sku: null, codigoBarras: ean2);

        // Assert
        Assert.NotEqual(ean1, ean2);
        Assert.True(generator.ValidarEan13(ean1));
        Assert.True(generator.ValidarEan13(ean2));
    }
}
