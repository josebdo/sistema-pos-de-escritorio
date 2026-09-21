using System.Globalization;

namespace SistemaCelulares.App.Common;

/// <summary>
/// Configuración de localización e internacionalización para República Dominicana.
/// </summary>
public static class AppCulture
{
    public static CultureInfo DominicoCulture { get; private set; } = null!;

    public static void ConfigurarCulturaDominicana()
    {
        // Configurar cultura regional para República Dominicana (es-DO)
        var culture = new CultureInfo("es-DO")
        {
            NumberFormat =
            {
                CurrencySymbol = "RD$",
                CurrencyPositivePattern = 0, // RD$n
                NumberDecimalSeparator = ".",
                NumberGroupSeparator = ",",
                CurrencyDecimalSeparator = ".",
                CurrencyGroupSeparator = ","
            },
            DateTimeFormat =
            {
                ShortDatePattern = "dd/MM/yyyy",
                LongDatePattern = "dddd, dd 'de' MMMM 'de' yyyy",
                ShortTimePattern = "hh:mm tt",
                LongTimePattern = "hh:mm:ss tt"
            }
        };

        DominicoCulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }

    public static string FormatearMoneda(decimal monto)
    {
        return monto.ToString("C2", DominicoCulture);
    }
}
