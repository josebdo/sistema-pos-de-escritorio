namespace SistemaCelulares.Core.Interfaces;

public interface IEan13GeneratorService
{
    /// <summary>
    /// Calcula el 13.° dígito verificador para una secuencia de 12 dígitos usando el algoritmo GS1 Modulo 10.
    /// </summary>
    int CalcularDigitoVerificador(string primeros12Digitos);

    /// <summary>
    /// Valida si una cadena es un código de barras EAN-13 válido (13 dígitos numéricos y dígito verificador correcto).
    /// </summary>
    bool ValidarEan13(string codigo);

    /// <summary>
    /// Genera un código EAN-13 único dentro del rango de uso interno GS1 (prefijo 20-29) sin colisiones en la base de datos.
    /// </summary>
    Task<string> GenerarEan13InternoAsync(int prefijoInterno = 20, CancellationToken cancellationToken = default);
}
