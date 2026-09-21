using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Interfaces;

namespace SistemaCelulares.App.Forms;

public class CategoriaModalForm : Form
{
    private readonly ICategoriaService _categoriaService;
    private readonly int? _categoriaIdParaEditar;

    private TextBox _txtNombre = null!;
    private TextBox _txtPrefijo = null!;
    private TextBox _txtDescripcion = null!;
    private Label _lblError = null!;
    private Button _btnGuardar = null!;

    public CategoriaModalForm(ICategoriaService categoriaService, int? categoriaId = null)
    {
        _categoriaService = categoriaService;
        _categoriaIdParaEditar = categoriaId;
        InitializeCustomComponents();
        Load += async (s, e) => await CargarDatosAsync();
    }

    private void InitializeCustomComponents()
    {
        Text = _categoriaIdParaEditar.HasValue ? "Editar Categoría" : "Nueva Categoría de Productos";
        Size = new Size(460, 420);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.AppBg;
        Font = UITheme.BodyFont;

        var card = new Panel
        {
            Size = new Size(400, 340),
            Location = new Point(22, 18),
            BackColor = Color.White,
            Padding = new Padding(20)
        };
        Controls.Add(card);

        var lblTitulo = new Label
        {
            Text = _categoriaIdParaEditar.HasValue ? "Modificar Categoría" : "Registrar Categoría",
            Font = UITheme.SubtitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(20, 15),
            AutoSize = true
        };
        card.Controls.Add(lblTitulo);

        // Nombre
        var lblNom = new Label { Text = "Nombre de la Categoría *", Font = UITheme.SectionFont, Location = new Point(20, 50), AutoSize = true };
        card.Controls.Add(lblNom);

        _txtNombre = new TextBox { Location = new Point(20, 72), Size = new Size(360, 28) };
        _txtNombre.TextChanged += (s, e) =>
        {
            if (!_categoriaIdParaEditar.HasValue && string.IsNullOrWhiteSpace(_txtPrefijo.Text))
            {
                var n = _txtNombre.Text.Trim();
                if (n.Length >= 3) _txtPrefijo.Text = n[..3].ToUpper();
            }
        };
        card.Controls.Add(_txtNombre);

        // Prefijo SKU
        var lblPre = new Label { Text = "Prefijo para SKU (ej. CEL, ACC, REP) *", Font = UITheme.SectionFont, Location = new Point(20, 110), AutoSize = true };
        card.Controls.Add(lblPre);

        _txtPrefijo = new TextBox { Location = new Point(20, 132), Size = new Size(360, 28), CharacterCasing = CharacterCasing.Upper };
        card.Controls.Add(_txtPrefijo);

        // Descripción
        var lblDesc = new Label { Text = "Descripción (opcional)", Font = UITheme.BodyFont, Location = new Point(20, 170), AutoSize = true };
        card.Controls.Add(lblDesc);

        _txtDescripcion = new TextBox { Location = new Point(20, 192), Size = new Size(360, 28) };
        card.Controls.Add(_txtDescripcion);

        // Error
        _lblError = new Label { Text = string.Empty, Font = UITheme.SmallFont, ForeColor = UITheme.Danger, Location = new Point(20, 230), Size = new Size(360, 25) };
        card.Controls.Add(_lblError);

        // Botones
        var panelBotones = new FlowLayoutPanel
        {
            Location = new Point(20, 265),
            Size = new Size(360, 45),
            FlowDirection = FlowDirection.RightToLeft
        };

        var btnCancelar = new Button { Text = "Cancelar", Size = new Size(95, 38), DialogResult = DialogResult.Cancel };
        UITheme.AplicarBotonSecundario(btnCancelar);
        panelBotones.Controls.Add(btnCancelar);

        _btnGuardar = new Button { Text = "Guardar", Size = new Size(110, 38) };
        UITheme.AplicarBotonPrimario(_btnGuardar);
        _btnGuardar.Click += async (s, e) => await GuardarCategoriaAsync();
        panelBotones.Controls.Add(_btnGuardar);

        card.Controls.Add(panelBotones);
    }

    private async Task CargarDatosAsync()
    {
        if (_categoriaIdParaEditar.HasValue)
        {
            var cat = await _categoriaService.ObtenerPorIdAsync(_categoriaIdParaEditar.Value);
            if (cat != null)
            {
                _txtNombre.Text = cat.Nombre;
                _txtPrefijo.Text = cat.PrefijoSku;
                _txtDescripcion.Text = cat.Descripcion ?? string.Empty;
            }
        }
    }

    private async Task GuardarCategoriaAsync()
    {
        _lblError.Text = string.Empty;
        var nombre = _txtNombre.Text.Trim();
        var prefijo = _txtPrefijo.Text.Trim().ToUpper();
        var desc = _txtDescripcion.Text.Trim();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            _lblError.Text = "El nombre de la categoría es obligatorio.";
            return;
        }

        if (string.IsNullOrWhiteSpace(prefijo))
        {
            _lblError.Text = "El prefijo para SKU es obligatorio.";
            return;
        }

        _btnGuardar.Enabled = false;
        try
        {
            if (_categoriaIdParaEditar.HasValue)
            {
                var ok = await _categoriaService.ActualizarCategoriaAsync(_categoriaIdParaEditar.Value, nombre, desc, prefijo);
                if (ok)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            else
            {
                await _categoriaService.CrearCategoriaAsync(nombre, desc, prefijo);
                DialogResult = DialogResult.OK;
                Close();
            }
        }
        catch (Exception ex)
        {
            _lblError.Text = ex.Message;
        }
        finally
        {
            _btnGuardar.Enabled = true;
        }
    }
}
