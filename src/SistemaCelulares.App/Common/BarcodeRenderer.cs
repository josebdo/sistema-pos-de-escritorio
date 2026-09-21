using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace SistemaCelulares.App.Common;

/// <summary>
/// Renderizador gráfico nativo para códigos de barras EAN-13 y generación de etiquetas de producto imprimibles.
/// </summary>
public static class BarcodeRenderer
{
    private static readonly string[] TableA =
    {
        "0001101", "0011001", "0010011", "0111101", "0100011",
        "0110001", "0101111", "0111011", "0110111", "0001011"
    };

    private static readonly string[] TableB =
    {
        "0100111", "0110011", "0011011", "0100001", "0011101",
        "0111001", "0000101", "0010001", "0001001", "0010111"
    };

    private static readonly string[] TableC =
    {
        "1110010", "1100110", "1101100", "1000010", "1011100",
        "1001110", "1010000", "1000100", "1001000", "1110100"
    };

    private static readonly string[] StructurePattern =
    {
        "AAAAAA", "AABABB", "AABBAB", "AABBBA", "ABAABB",
        "ABBAAB", "ABBBAA", "ABABAB", "ABABBA", "ABBABA"
    };

    /// <summary>
    /// Genera la secuencia binaria (módulos 0 y 1) correspondiente a un código EAN-13.
    /// </summary>
    public static string ObtenerPatronBinarioEan13(string ean13)
    {
        if (string.IsNullOrWhiteSpace(ean13) || ean13.Length != 13 || !ean13.All(char.IsDigit))
        {
            throw new ArgumentException("El código debe contener exactamente 13 dígitos numéricos.", nameof(ean13));
        }

        int primerDigito = ean13[0] - '0';
        string patronIzquierda = StructurePattern[primerDigito];

        var sb = new System.Text.StringBuilder();

        // 1. Guard Inicio: 101
        sb.Append("101");

        // 2. 6 dígitos de la izquierda
        for (int i = 0; i < 6; i++)
        {
            int digito = ean13[i + 1] - '0';
            char tabla = patronIzquierda[i];
            sb.Append(tabla == 'A' ? TableA[digito] : TableB[digito]);
        }

        // 3. Guard Central: 01010
        sb.Append("01010");

        // 4. 6 dígitos de la derecha (incluye dígito verificador)
        for (int i = 0; i < 6; i++)
        {
            int digito = ean13[i + 7] - '0';
            sb.Append(TableC[digito]);
        }

        // 5. Guard Fin: 101
        sb.Append("101");

        return sb.ToString();
    }

    /// <summary>
    /// Genera una imagen Bitmap nítida del código de barras EAN-13 con sus dígitos legibles debajo.
    /// </summary>
    public static Bitmap RenderizarEan13(string ean13, int moduleWidth = 3, int barHeight = 80)
    {
        string binario = ObtenerPatronBinarioEan13(ean13);

        int quietZone = 12 * moduleWidth;
        int barcodeWidth = binario.Length * moduleWidth;
        int totalWidth = barcodeWidth + (quietZone * 2);
        int textHeight = 25;
        int totalHeight = barHeight + textHeight + 10;

        var bitmap = new Bitmap(totalWidth, totalHeight);
        using var g = Graphics.FromImage(bitmap);

        g.Clear(Color.White);
        g.SmoothingMode = SmoothingMode.None;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var blackBrush = new SolidBrush(Color.Black);

        // Dibujar barras
        int currentX = quietZone;
        for (int i = 0; i < binario.Length; i++)
        {
            if (binario[i] == '1')
            {
                // Las barras de guard (inicio, centro, fin) bajan un poco más
                bool esGuard = (i < 3) || (i >= 45 && i < 50) || (i >= 92);
                int height = esGuard ? barHeight + 6 : barHeight;
                g.FillRectangle(blackBrush, currentX, 5, moduleWidth, height);
            }
            currentX += moduleWidth;
        }

        // Dibujar texto de los números EAN-13
        using var font = new Font("Consolas", 11.5F, FontStyle.Bold);
        using var fontFirst = new Font("Consolas", 10.5F, FontStyle.Bold);

        // Primer dígito en la zona izquierda
        g.DrawString(ean13[0].ToString(), fontFirst, blackBrush, 2, barHeight - 12);

        // Grupo izquierdo (6 dígitos)
        string leftText = ean13.Substring(1, 6);
        g.DrawString(leftText, font, blackBrush, quietZone + (4 * moduleWidth), barHeight + 3);

        // Grupo derecho (6 dígitos)
        string rightText = ean13.Substring(7, 6);
        g.DrawString(rightText, font, blackBrush, quietZone + (50 * moduleWidth), barHeight + 3);

        return bitmap;
    }

    /// <summary>
    /// Genera una etiqueta completa de producto lista para imprimir (Nombre de la tienda, Producto, SKU, Código de barras, Precio RD$).
    /// </summary>
    public static Bitmap GenerarEtiquetaProducto(
        string nombreProducto,
        string sku,
        string ean13,
        decimal precioVenta,
        string nombreTienda = "TIENDA DE CELULARES")
    {
        int labelWidth = 380;
        int labelHeight = 220;

        var bitmap = new Bitmap(labelWidth, labelHeight);
        using var g = Graphics.FromImage(bitmap);

        g.Clear(Color.White);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var brushDark = new SolidBrush(Color.FromArgb(15, 23, 42));
        using var brushMuted = new SolidBrush(Color.FromArgb(100, 116, 139));
        using var brushSuccess = new SolidBrush(Color.FromArgb(5, 150, 105));
        using var penBorder = new Pen(Color.FromArgb(226, 232, 240), 1.5f);

        // Borde exterior de etiqueta
        g.DrawRectangle(penBorder, 1, 1, labelWidth - 3, labelHeight - 3);

        // Encabezado tienda
        using var fontTienda = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        g.DrawString(nombreTienda.ToUpper(), fontTienda, brushMuted, new PointF(15, 8));

        // Nombre del producto
        using var fontProd = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        var rectNombre = new RectangleF(15, 25, labelWidth - 30, 36);
        var sf = new StringFormat { Trimming = StringTrimming.EllipsisWord, FormatFlags = StringFormatFlags.LineLimit };
        g.DrawString(nombreProducto, fontProd, brushDark, rectNombre, sf);

        // Código de barras EAN-13
        using var bmpBarcode = RenderizarEan13(ean13, moduleWidth: 2, barHeight: 50);
        int barcodeX = (labelWidth - bmpBarcode.Width) / 2;
        g.DrawImage(bmpBarcode, barcodeX, 65);

        // Pie: SKU y Precio en RD$
        using var fontSku = new Font("Segoe UI", 9F, FontStyle.Bold);
        g.DrawString($"SKU: {sku}", fontSku, brushDark, new PointF(15, 175));

        using var fontPrecio = new Font("Segoe UI", 14F, FontStyle.Bold);
        string precioTexto = precioVenta.ToString("C2", new CultureInfo("es-DO"));
        var sizePrecio = g.MeasureString(precioTexto, fontPrecio);
        g.DrawString(precioTexto, fontPrecio, brushSuccess, new PointF(labelWidth - sizePrecio.Width - 15, 168));

        return bitmap;
    }
}
