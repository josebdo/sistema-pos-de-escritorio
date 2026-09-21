using SistemaCelulares.Infrastructure.Security;
using Xunit;

namespace SistemaCelulares.Tests;

public class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_DebeGenerarHashValidoYNoTextoPlano()
    {
        // Arrange
        var rawPassword = "MiPasswordSeguro123!";

        // Act
        var hash = _hasher.HashPassword(rawPassword);

        // Assert
        Assert.NotNull(hash);
        Assert.StartsWith("$2", hash); // BCrypt prefix
        Assert.NotEqual(rawPassword, hash);
    }

    [Fact]
    public void VerifyPassword_ConPasswordCorrecto_DebeRetornarTrue()
    {
        // Arrange
        var rawPassword = "ClaveCajero123!";
        var hash = _hasher.HashPassword(rawPassword);

        // Act
        var resultado = _hasher.VerifyPassword(rawPassword, hash);

        // Assert
        Assert.True(resultado);
    }

    [Fact]
    public void VerifyPassword_ConPasswordIncorrecto_DebeRetornarFalse()
    {
        // Arrange
        var rawPassword = "ClaveCajero123!";
        var hash = _hasher.HashPassword(rawPassword);

        // Act
        var resultado = _hasher.VerifyPassword("ClaveEquivocada", hash);

        // Assert
        Assert.False(resultado);
    }
}
