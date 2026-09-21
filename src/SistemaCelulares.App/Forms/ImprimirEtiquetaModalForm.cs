using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Entities;

namespace SistemaCelulares.App.Forms;

public class ImprimirEtiquetaModalForm : Form
{
    private readonly Producto _producto;
    private readonly Bitmap _etiquetaBitmap;

    private PictureBox _picPreview = null!;
    private NumericUpDown _numCopias = null!;
    private Button _btnImprimir = null!;
    private Button _btnGuardarImagen = null!;
    private Button _btnCopiar = null!;

    public ImprimirEtiquetaModalForm(Producto producto)
    {
        _producto = producto;

        if (string.IsNullOrWhiteSpace(_producto.CodigoBarras))
        {
            throw new InvalidOperationException("El producto no posee un código de barras asignado.");
        }

        _etiquetaBitmap = BarcodeRenderer.GenerarEtiquetaProducto(
            _producto.Nombre,
            _producto.Sku,
            _producto.CodigoBarras,
            _producto.PrecioVenta
        );

        InitializeCustomComponents();
    }

    private void InitializeCustomComponents()
    {
        Text = $"Etiqueta de Código de Barras - {_producto.Sku}";
        Size = new Size(520, 480);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.AppBg;
        Font = UITheme.BodyFont;

        var panelCard = new Panel
        {
            Location = new Point(20, 15),
            Size = new Size(465, 340),
            BackColor = Color.White,
            Padding = new Padding(15)
        };
        Controls.Add(panelCard);

        var lblTitulo = new Label
        {
            Text = "🏷️ Vista Previa de la Etiqueta",
            Font = UITheme.SectionFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(15, 10),
            AutoSize = true
        };
        panelCard.Controls.Add(lblTitulo);

        _picPreview = new PictureBox
        {
            Location = new Point(40, 40),
            Size = new Size(380, 220),
            Image = _etiquetaBitmap,
            SizeMode = PictureBoxSizeMode.CenterImage,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White
        };
        panelCard.Controls.Add(_picPreview);

        // Opciones de Copias
        var lblCopias = new Label
        {
            Text = "Cantidad de copias a imprimir:",
            Font = UITheme.SmallFont,
            Location = new Point(40, 280),
            AutoSize = true
        };
        panelCard.Controls.Add(lblCopias);

        _numCopias = new NumericUpDown
        {
            Location = new Point(220, 276),
            Size = new Size(70, 26),
            Minimum = 1,
            Maximum = 100,
            Value = 1
        };
        panelCard.Controls.Add(_numCopias);

        // Botones de acción
        var panelBotones = new FlowLayoutPanel
        {
            Location = new Point(20, 370),
            Size = new Size(465, 50),
            FlowDirection = FlowDirection.RightToLeft
        };
        Controls.Add(panelBotones);

        var btnCerrar = new Button
        {
            Text = "Cerrar",
            Size = new Size(85, 36),
            DialogResult = DialogResult.OK
        };
        UITheme.AplicarBotonSecundario(btnCerrar);
        panelBotones.Controls.Add(btnCerrar);

        _btnImprimir = new Button
        {
            Text = "🖨️ Imprimir",
            Size = new Size(125, 36)
        };
        UITheme.AplicarBotonPrimario(_btnImprimir);
        _btnImprimir.Click += (s, e) => ImprimirEtiqueta();
        panelBotones.Controls.Add(_btnImprimir);

        _btnGuardarImagen = new Button
        {
            Text = "💾 Guardar PNG",
            Size = new Size(125, 36)
        };
        UITheme.AplicarBotonSecundario(_btnGuardarImagen);
        _btnGuardarImagen.Click += (s, e) => GuardarImagen();
        panelBotones.Controls.Add(_btnGuardarImagen);

        _btnCopiar = new Button
        {
            Text = "📋 Copiar",
            Size = new Size(90, 36)
        };
        UITheme.AplicarBotonSecundario(_btnCopiar);
        _btnCopiar.Click += (s, e) =>
        {
            Clipboard.SetImage(_etiquetaBitmap);
            MessageBox.Show("Etiqueta copiada al portapapeles.", "Copiado", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        panelBotones.Controls.Add(_btnCopiar);
    }

    private void GuardarImagen()
    {
        using var sfd = new SaveFileDialog
        {
            Filter = "Imagen PNG (*.png)|*.png",
            FileName = $"Etiqueta_{_producto.Sku}_{_producto.CodigoBarras}.png"
        };

        if (sfd.ShowDialog(this) == DialogResult.OK)
        {
            _etiquetaBitmap.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png);
            MessageBox.Show("Etiqueta guardada con éxito.", "Guardado", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void ImprimirEtiqueta()
    {
        try
        {
            var printDoc = new PrintDocument();
            int copiasImpresas = 0;
            int totalCopias = (int)_numCopias.Value;

            printDoc.PrintPage += (sender, ev) =>
            {
                if (ev.Graphics != null)
                {
                    ev.Graphics.DrawImage(_etiquetaBitmap, ev.MarginBounds.Left, ev.MarginBounds.Top);
                }

                copiasImpresas++;
                ev.HasMorePages = copiasImpresas < totalCopias;
            };

            using var printDialog = new PrintDialog
            {
                Document = printDoc,
                UseEXDialog = true
            };

            if (printDialog.ShowDialog(this) == DialogResult.OK)
            {
                printDoc.Print();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al imprimir etiqueta: {ex.Message}", "Error de Impresión", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
