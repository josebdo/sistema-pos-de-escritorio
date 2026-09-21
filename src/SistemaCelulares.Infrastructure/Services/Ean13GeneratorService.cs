using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Infrastructure.Data;

namespace SistemaCelulares.Infrastructure.Services;

public class Ean13GeneratorService : IEan13GeneratorService
{
    private readonly AppDbContext _context;

    public Ean13GeneratorService(AppDbContext context)
    {
        _context = context;
    }

    public int CalcularDigitoVerificador(string primeros12Digitos)
    {
        if (string.IsNullOrWhiteSpace(primeros12Digitos) || primeros12Digitos.Length != 12 || !primeros12Digitos.All(char.IsDigit))
        {
            throw new ArgumentException("La secuencia para calcular el dígito verificador debe contener exactamente 12 dígitos numéricos.", nameof(primeros12Digitos));
        }

        int suma = 0;
        for (int i = 0; i < 12; i++)
        {
            int digito = primeros12Digitos[i] - '0';
            // Posiciones impares (índices 0, 2, 4, 6, 8, 10 en base 0) peso 1
            // Posiciones pares (índices 1, 3, 5, 7, 9, 11 en base 0) peso 3
            suma += (i % 2 == 0) ? digito : digito * 3;
        }

        int mod = suma % 10;
        return (mod == 0) ? 0 : 10 - mod;
    }

    public bool ValidarEan13(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo) || codigo.Length != 13 || !codigo.All(char.IsDigit))
        {
            return false;
        }

        var primeros12 = codigo.Substring(0, 12);
        int digitoVerificadorEsperado = CalcularDigitoVerificador(primeros12);
        int digitoReal = codigo[12] - '0';

        return digitoVerificadorEsperado == digitoReal;
    }

    public async Task<string> GenerarEan13InternoAsync(int prefijoInterno = 20, CancellationToken cancellationToken = default)
    {
        if (prefijoInterno < 20 || prefijoInterno > 29)
        {
            throw new ArgumentOutOfRangeException(nameof(prefijoInterno), "El prefijo para uso interno GS1 debe estar en el rango 20 a 29.");
        }

        var prefijoStr = prefijoInterno.ToString("D2");

        // Obtener códigos de barras existentes con el prefijo interno
        var codigosExistentes = await _context.Productos
            .Where(p => p.CodigoBarras != null && p.CodigoBarras.StartsWith(prefijoStr) && p.CodigoBarras.Length == 13)
            .Select(p => p.CodigoBarras!)
            .ToListAsync(cancellationToken);

        long maxCorrelativo = 0;
        foreach (var cb in codigosExistentes)
        {
            // Extraer los 10 dígitos del correlativo interno (posiciones 2 a 12)
            var correlativoStr = cb.Substring(2, 10);
            if (long.TryParse(correlativoStr, out long num))
            {
                if (num > maxCorrelativo) maxCorrelativo = num;
            }
        }

        long siguiente = maxCorrelativo + 1;
        var primeros12 = $"{prefijoStr}{siguiente:D10}";
        var digitoVerificador = CalcularDigitoVerificador(primeros12);
        var ean13Final = $"{primeros12}{digitoVerificador}";

        // Asegurar que no colisione con ningún otro código de barras existente
        while (await _context.Productos.AnyAsync(p => p.CodigoBarras == ean13Final, cancellationToken))
        {
            siguiente++;
            primeros12 = $"{prefijoStr}{siguiente:D10}";
            digitoVerificador = CalcularDigitoVerificador(primeros12);
            ean13Final = $"{primeros12}{digitoVerificador}";
        }

        return ean13Final;
    }
}
