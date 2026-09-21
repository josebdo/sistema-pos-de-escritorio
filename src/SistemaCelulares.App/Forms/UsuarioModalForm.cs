using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;

namespace SistemaCelulares.App.Forms;

public class UsuarioModalForm : Form
{
    private readonly IUsuarioService _usuarioService;
    private readonly IRolService _rolService;
    private readonly int? _usuarioIdParaEditar;

    private TextBox _txtNombreCompleto = null!;
    private TextBox _txtNombreUsuario = null!;
    private TextBox _txtPasswordTemporal = null!;
    private TextBox _txtEmail = null!;
    private TextBox _txtTelefono = null!;
    private ComboBox _cbRoles = null!;
    private Label _lblPassLabel = null!;
    private Label _lblError = null!;
    private Button _btnGuardar = null!;

    public UsuarioModalForm(IUsuarioService usuarioService, IRolService rolService, int? usuarioId = null)
    {
        _usuarioService = usuarioService;
        _rolService = rolService;
        _usuarioIdParaEditar = usuarioId;
        InitializeCustomComponents();
        Load += async (s, e) => await CargarDatosInicialesAsync();
    }

    private void InitializeCustomComponents()
    {
        Text = _usuarioIdParaEditar.HasValue ? "Editar Usuario / Empleado" : "Crear Nuevo Usuario / Empleado";
        Size = new Size(480, 560);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.AppBg;
        Font = UITheme.BodyFont;

        var card = new Panel
        {
            Size = new Size(420, 480),
            Location = new Point(22, 16),
            BackColor = Color.White,
            Padding = new Padding(20)
        };
        Controls.Add(card);

        var lblTitulo = new Label
        {
            Text = _usuarioIdParaEditar.HasValue ? "Modificar Empleado" : "Registrar Nuevo Empleado",
            Font = UITheme.SubtitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(20, 15),
            AutoSize = true
        };
        card.Controls.Add(lblTitulo);

        // Nombre Completo
        var lblNom = new Label { Text = "Nombre Completo *", Font = UITheme.SectionFont, Location = new Point(20, 50), AutoSize = true };
        card.Controls.Add(lblNom);
        _txtNombreCompleto = new TextBox { Location = new Point(20, 72), Size = new Size(380, 28) };
        card.Controls.Add(_txtNombreCompleto);

        // Nombre de Usuario
        var lblUser = new Label { Text = "Nombre de Usuario (Login) *", Font = UITheme.SectionFont, Location = new Point(20, 110), AutoSize = true };
        card.Controls.Add(lblUser);
        _txtNombreUsuario = new TextBox { Location = new Point(20, 132), Size = new Size(380, 28) };
        card.Controls.Add(_txtNombreUsuario);

        // Rol
        var lblRol = new Label { Text = "Rol asignado *", Font = UITheme.SectionFont, Location = new Point(20, 170), AutoSize = true };
        card.Controls.Add(lblRol);
        _cbRoles = new ComboBox { Location = new Point(20, 192), Size = new Size(380, 28), DropDownStyle = ComboBoxStyle.DropDownList };
        card.Controls.Add(_cbRoles);

        // Password Temporal (solo al crear)
        _lblPassLabel = new Label { Text = "Contraseña Temporal (mín. 6 car.) *", Font = UITheme.SectionFont, Location = new Point(20, 230), AutoSize = true };
        card.Controls.Add(_lblPassLabel);
        _txtPasswordTemporal = new TextBox { Location = new Point(20, 252), Size = new Size(380, 28), Text = "Temporal123!" };
        card.Controls.Add(_txtPasswordTemporal);

        // Email y Teléfono
        var lblEmail = new Label { Text = "Correo Electrónico (opcional)", Font = UITheme.BodyFont, Location = new Point(20, 290), AutoSize = true };
        card.Controls.Add(lblEmail);
        _txtEmail = new TextBox { Location = new Point(20, 312), Size = new Size(180, 28) };
        card.Controls.Add(_txtEmail);

        var lblTel = new Label { Text = "Teléfono (opcional)", Font = UITheme.BodyFont, Location = new Point(210, 290), AutoSize = true };
        card.Controls.Add(lblTel);
        _txtTelefono = new TextBox { Location = new Point(210, 312), Size = new Size(190, 28) };
        card.Controls.Add(_txtTelefono);

        // Error
        _lblError = new Label { Text = string.Empty, Font = UITheme.SmallFont, ForeColor = UITheme.Danger, Location = new Point(20, 350), Size = new Size(380, 35) };
        card.Controls.Add(_lblError);

        // Botones
        var panelBotones = new FlowLayoutPanel
        {
            Location = new Point(20, 405),
            Size = new Size(380, 45),
            FlowDirection = FlowDirection.RightToLeft
        };

        var btnCancelar = new Button { Text = "Cancelar", Size = new Size(95, 38), DialogResult = DialogResult.Cancel };
        UITheme.AplicarBotonSecundario(btnCancelar);
        panelBotones.Controls.Add(btnCancelar);

        _btnGuardar = new Button { Text = "Guardar", Size = new Size(110, 38) };
        UITheme.AplicarBotonPrimario(_btnGuardar);
        _btnGuardar.Click += async (s, e) => await GuardarUsuarioAsync();
        panelBotones.Controls.Add(_btnGuardar);

        card.Controls.Add(panelBotones);
    }

    private async Task CargarDatosInicialesAsync()
    {
        var roles = await _rolService.ObtenerRolesAsync(soloActivos: true);
        _cbRoles.DisplayMember = nameof(Rol.Nombre);
        _cbRoles.ValueMember = nameof(Rol.Id);
        _cbRoles.DataSource = roles;

        if (_usuarioIdParaEditar.HasValue)
        {
            var usuario = await _usuarioService.ObtenerPorIdAsync(_usuarioIdParaEditar.Value);
            if (usuario != null)
            {
                _txtNombreCompleto.Text = usuario.NombreCompleto;
                _txtNombreUsuario.Text = usuario.NombreUsuario;
                _txtNombreUsuario.ReadOnly = true; // No editable para conservar integridad
                _txtEmail.Text = usuario.Email ?? string.Empty;
                _txtTelefono.Text = usuario.Telefono ?? string.Empty;
                _cbRoles.SelectedValue = usuario.RolId;

                // Ocultar campo de contraseña temporal en edición
                _lblPassLabel.Visible = false;
                _txtPasswordTemporal.Visible = false;
            }
        }
    }

    private async Task GuardarUsuarioAsync()
    {
        _lblError.Text = string.Empty;
        var nombre = _txtNombreCompleto.Text.Trim();
        var usuario = _txtNombreUsuario.Text.Trim();
        var email = _txtEmail.Text.Trim();
        var telefono = _txtTelefono.Text.Trim();

        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(usuario))
        {
            _lblError.Text = "El nombre completo y nombre de usuario son obligatorios.";
            return;
        }

        if (_cbRoles.SelectedValue == null)
        {
            _lblError.Text = "Debe seleccionar un rol.";
            return;
        }

        var rolId = (int)_cbRoles.SelectedValue;

        _btnGuardar.Enabled = false;
        try
        {
            if (_usuarioIdParaEditar.HasValue)
            {
                var ok = await _usuarioService.ActualizarUsuarioAsync(_usuarioIdParaEditar.Value, nombre, rolId, email, telefono);
                if (ok)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            else
            {
                var pass = _txtPasswordTemporal.Text.Trim();
                if (string.IsNullOrWhiteSpace(pass) || pass.Length < 6)
                {
                    _lblError.Text = "La contraseña temporal debe tener al menos 6 caracteres.";
                    _btnGuardar.Enabled = true;
                    return;
                }

                await _usuarioService.CrearUsuarioAsync(nombre, usuario, pass, rolId, email, telefono);
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
