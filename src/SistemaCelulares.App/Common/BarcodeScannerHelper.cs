using System.Windows.Forms;

namespace SistemaCelulares.App.Common;

/// <summary>
/// Helper para la captura y procesamiento de lecturas de código de barras
/// emitidas por lectores físicos USB (emulación de teclado con sufijo Enter).
/// </summary>
public static class BarcodeScannerHelper
{
    /// <summary>
    /// Configura un TextBox para escuchar escaneos de códigos de barras al presionar Enter,
    /// suprimiendo el sonido de campana de Windows y ejecutando la acción de búsqueda.
    /// </summary>
    /// <param name="textBox">El control TextBox donde se escanea o ingresa el código</param>
    /// <param name="onBarcodeScanned">Función asíncrona a ejecutar con el código leído</param>
    /// <param name="autoSelectOrClear">Si es true, selecciona o limpia el texto tras la lectura</param>
    public static void ConfigurarParaEscaneo(
        TextBox textBox,
        Func<string, Task> onBarcodeScanned,
        bool autoSelectOrClear = true)
    {
        textBox.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true; // Evita el beep de Windows en TextBox de una sola línea

                var codigo = textBox.Text.Trim();
                if (!string.IsNullOrWhiteSpace(codigo))
                {
                    try
                    {
                        await onBarcodeScanned(codigo);
                    }
                    finally
                    {
                        if (autoSelectOrClear)
                        {
                            textBox.SelectAll();
                        }
                    }
                }
            }
        };
    }
}
