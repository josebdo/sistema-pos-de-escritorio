using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Interfaces;

namespace SistemaCelulares.App.Forms;

public class ProveedorModalForm : Form
{
    private readonly IProveedorService _proveedorService;
    private readonly int? _proveedorIdParaEditar;

    private TextBox _txtNombre = null!;
    private TextBox _txtRnc = null!;
    private TextBox _txtTelefono = null!;
    private TextBox _txtEmail = null!;
    private TextBox _txtContacto = null!;
    private TextBox _txtDireccion = null!;
    private Label _lblError = null!;
    private Button _btnGuardar = null!;

    public ProveedorModalForm(IProveedorService proveedorService, int? proveedorId = null)
    {
        _proveedorService = proveedorService;
        _proveedorIdParaEditar = proveedorId;

        InitializeCustomComponents();
        Load += async (s, e) => await CargarDatosAsync();
    }

    private void InitializeCustomComponents()
    {
        Text = _proveedorIdParaEditar.HasValue ? "Editar Proveedor" : "Registrar Nuevo Proveedor";
        Size = new Size(520, 560);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.AppBg;
        Font = UITheme.BodyFont;

        var card = new Panel
        {
            Size = new Size(460, 480),
            Location = new Point(22, 16),
            BackColor = Color.White,
            Padding = new Padding(20)
        };
        Controls.Add(card);

        var lblTitulo = new Label
        {
            Text = _proveedorIdParaEditar.HasValue ? "Modificar Datos del Proveedor" : "Nuevo Proveedor Comercial",
            Font = UITheme.SubtitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(20, 15),
            AutoSize = true
        };
        card.Controls.Add(lblTitulo);

        // Nombre
        var lblNom = new Label { Text = "Razón Social / Nombre del Proveedor *", Font = UITheme.SectionFont, Location = new Point(20, 50), AutoSize = true };
        card.Controls.Add(lblNom);

        _txtNombre = new TextBox { Location = new Point(20, 72), Size = new Size(420, 28) };
        card.Controls.Add(_txtNombre);

        // RNC y Teléfono
        var lblRnc = new Label { Text = "RNC Dominicano *", Font = UITheme.SectionFont, Location = new Point(20, 110), AutoSize = true };
        card.Controls.Add(lblRnc);

        _txtRnc = new TextBox { Location = new Point(20, 132), Size = new Size(200, 28) };
        card.Controls.Add(_txtRnc);

        var lblTel = new Label { Text = "Teléfono de Contacto *", Font = UITheme.SectionFont, Location = new Point(230, 110), AutoSize = true };
        card.Controls.Add(lblTel);

        _txtTelefono = new TextBox { Location = new Point(230, 132), Size = new Size(210, 28) };
        card.Controls.Add(_txtTelefono);

        // Email y Persona de Contacto
        var lblEmail = new Label { Text = "Correo Electrónico *", Font = UITheme.SectionFont, Location = new Point(20, 170), AutoSize = true };
        card.Controls.Add(lblEmail);

        _txtEmail = new TextBox { Location = new Point(20, 192), Size = new Size(200, 28) };
        card.Controls.Add(_txtEmail);

        var lblCon = new Label { Text = "Persona de Contacto / Vendedor *", Font = UITheme.SectionFont, Location = new Point(230, 170), AutoSize = true };
        card.Controls.Add(lblCon);

        _txtContacto = new TextBox { Location = new Point(230, 192), Size = new Size(210, 28) };
        card.Controls.Add(_txtContacto);

        // Dirección
        var lblDir = new Label { Text = "Dirección Física *", Font = UITheme.SectionFont, Location = new Point(20, 230), AutoSize = true };
        card.Controls.Add(lblDir);

        _txtDireccion = new TextBox { Location = new Point(20, 252), Size = new Size(420, 50), Multiline = true };
        card.Controls.Add(_txtDireccion);

        // Error
        _lblError = new Label { Text = string.Empty, Font = UITheme.SmallFont, ForeColor = UITheme.Danger, Location = new Point(20, 315), Size = new Size(420, 25) };
        card.Controls.Add(_lblError);

        // Botones
        var panelBotones = new FlowLayoutPanel
        {
            Location = new Point(20, 350),
            Size = new Size(420, 45),
            FlowDirection = FlowDirection.RightToLeft
        };

        var btnCancelar = new Button { Text = "Cancelar", Size = new Size(95, 38), DialogResult = DialogResult.Cancel };
        UITheme.AplicarBotonSecundario(btnCancelar);
        panelBotones.Controls.Add(btnCancelar);

        _btnGuardar = new Button { Text = "Guardar", Size = new Size(120, 38) };
        UITheme.AplicarBotonPrimario(_btnGuardar);
        _btnGuardar.Click += async (s, e) => await GuardarProveedorAsync();
        panelBotones.Controls.Add(_btnGuardar);

        card.Controls.Add(panelBotones);
    }

    private async Task CargarDatosAsync()
    {
        if (_proveedorIdParaEditar.HasValue)
        {
            var p = await _proveedorService.ObtenerPorIdAsync(_proveedorIdParaEditar.Value);
            if (p != null)
            {
                _txtNombre.Text = p.Nombre;
                _txtRnc.Text = p.Rnc ?? string.Empty;
                _txtTelefono.Text = p.Telefono ?? string.Empty;
                _txtEmail.Text = p.Email ?? string.Empty;
                _txtContacto.Text = p.Contacto ?? string.Empty;
                _txtDireccion.Text = p.Direccion ?? string.Empty;
            }
        }
    }

    private async Task GuardarProveedorAsync()
    {
        _lblError.Text = string.Empty;
        var nombre = _txtNombre.Text.Trim();
        var rnc = _txtRnc.Text.Trim();
        var tel = _txtTelefono.Text.Trim();
        var email = _txtEmail.Text.Trim();
        var contacto = _txtContacto.Text.Trim();
        var dir = _txtDireccion.Text.Trim();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            MessageBox.Show("El nombre o razón social del proveedor es obligatorio.", "Campo Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtNombre.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(rnc))
        {
            MessageBox.Show("El RNC del proveedor es obligatorio.", "Campo Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtRnc.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(tel))
        {
            MessageBox.Show("El teléfono del proveedor es obligatorio.", "Campo Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtTelefono.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            MessageBox.Show("El correo electrónico del proveedor es obligatorio.", "Campo Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtEmail.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(contacto))
        {
            MessageBox.Show("La persona de contacto o vendedor es obligatoria.", "Campo Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtContacto.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(dir))
        {
            MessageBox.Show("La dirección física del proveedor es obligatoria.", "Campo Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtDireccion.Focus();
            return;
        }

        _btnGuardar.Enabled = false;
        try
        {
            if (_proveedorIdParaEditar.HasValue)
            {
                var ok = await _proveedorService.ActualizarProveedorAsync(
                    _proveedorIdParaEditar.Value,
                    nombre,
                    rnc,
                    tel,
                    email,
                    dir,
                    contacto
                );

                if (ok)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            else
            {
                await _proveedorService.CrearProveedorAsync(
                    nombre,
                    rnc,
                    tel,
                    email,
                    dir,
                    contacto
                );

                DialogResult = DialogResult.OK;
                Close();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar proveedor: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnGuardar.Enabled = true;
        }
    }
}
