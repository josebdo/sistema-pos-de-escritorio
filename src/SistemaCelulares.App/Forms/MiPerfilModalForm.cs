using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class MiPerfilModalForm : Form
{
    private readonly IUsuarioService _usuarioService;
    private readonly IAuthService _authService;
    private readonly SesionUsuario _sesion;

    private TextBox _txtNombreCompleto = null!;
    private TextBox _txtNombreUsuario = null!;
    private TextBox _txtEmail = null!;
    
    private TextBox _txtPasswordActual = null!;
    private TextBox _txtPasswordNueva = null!;
    private TextBox _txtPasswordConfirmar = null!;
    private Label _lblError = null!;
    private Button _btnGuardar = null!;
    private Button _btnCancelar = null!;

    public bool PerfilActualizado { get; private set; }

    public MiPerfilModalForm(IUsuarioService usuarioService, IAuthService authService, SesionUsuario sesion)
    {
        _usuarioService = usuarioService;
        _authService = authService;
        _sesion = sesion;

        InitializeCustomComponents();
        Load += async (s, e) => await CargarDatosPerfilAsync();
    }

    private void InitializeCustomComponents()
    {
        Text = "Mi Perfil y Seguridad";
        Size = new Size(480, 620);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.AppBg;
        Font = UITheme.BodyFont;

        var card = new Panel
        {
            Size = new Size(424, 545),
            Location = new Point(20, 15),
            BackColor = Color.White,
            Padding = new Padding(20)
        };
        card.Paint += (s, e) => e.Graphics.DrawRectangle(new Pen(UITheme.Border), 0, 0, card.Width - 1, card.Height - 1);
        Controls.Add(card);

        // Header
        var lblTitulo = new Label
        {
            Text = "👤 Mi Perfil y Seguridad",
            Font = UITheme.SubtitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(20, 15),
            AutoSize = true
        };
        card.Controls.Add(lblTitulo);

        var lblSub = new Label
        {
            Text = "Modifique su nombre visible o actualice su contraseña de acceso",
            Font = UITheme.SmallFont,
            ForeColor = UITheme.TextSecondary,
            Location = new Point(20, 40),
            AutoSize = true
        };
        card.Controls.Add(lblSub);

        // Nombre Completo (Editable por cualquier usuario)
        var lblNom = new Label { Text = "Nombre Completo *", Font = UITheme.SectionFont, Location = new Point(20, 70), AutoSize = true };
        card.Controls.Add(lblNom);
        _txtNombreCompleto = new TextBox { Location = new Point(20, 92), Size = new Size(384, 26), Font = UITheme.BodyFont };
        card.Controls.Add(_txtNombreCompleto);

        // Nombre de Usuario y Correo (Solo lectura para no-admin)
        var lblUser = new Label { Text = "Usuario (Login)", Font = UITheme.SectionFont, Location = new Point(20, 126), AutoSize = true };
        card.Controls.Add(lblUser);
        _txtNombreUsuario = new TextBox { Location = new Point(20, 146), Size = new Size(184, 26), Font = UITheme.BodyFont, ReadOnly = true, BackColor = Color.FromArgb(245, 246, 248) };
        card.Controls.Add(_txtNombreUsuario);

        var lblEmail = new Label { Text = "Correo Electrónico", Font = UITheme.SectionFont, Location = new Point(216, 126), AutoSize = true };
        card.Controls.Add(lblEmail);
        _txtEmail = new TextBox { Location = new Point(216, 146), Size = new Size(188, 26), Font = UITheme.BodyFont, ReadOnly = true, BackColor = Color.FromArgb(245, 246, 248) };
        card.Controls.Add(_txtEmail);

        var lblAvisoAdmin = new Label
        {
            Text = "🔒 El usuario y correo solo pueden ser modificados por el Administrador.",
            Font = new Font("Segoe UI", 7.8F, FontStyle.Italic),
            ForeColor = UITheme.TextMuted,
            Location = new Point(20, 175),
            AutoSize = true
        };
        card.Controls.Add(lblAvisoAdmin);

        // Separador
        var sep = new Panel { Location = new Point(20, 198), Size = new Size(384, 1), BackColor = UITheme.Border };
        card.Controls.Add(sep);

        // Sección Contraseña
        var lblSecPass = new Label { Text = "🔑 Cambiar Contraseña (Opcional)", Font = UITheme.SectionFont, ForeColor = UITheme.Primary, Location = new Point(20, 210), AutoSize = true };
        card.Controls.Add(lblSecPass);

        var lblPassAct = new Label { Text = "Contraseña actual", Font = UITheme.SmallFont, ForeColor = UITheme.TextSecondary, Location = new Point(20, 235), AutoSize = true };
        card.Controls.Add(lblPassAct);
        _txtPasswordActual = new TextBox { Location = new Point(20, 253), Size = new Size(384, 26), PasswordChar = '●', Font = UITheme.BodyFont };
        card.Controls.Add(_txtPasswordActual);

        var lblPassNew = new Label { Text = "Nueva contraseña (mínimo 6 caracteres)", Font = UITheme.SmallFont, ForeColor = UITheme.TextSecondary, Location = new Point(20, 287), AutoSize = true };
        card.Controls.Add(lblPassNew);
        _txtPasswordNueva = new TextBox { Location = new Point(20, 305), Size = new Size(384, 26), PasswordChar = '●', Font = UITheme.BodyFont };
        card.Controls.Add(_txtPasswordNueva);

        var lblPassConf = new Label { Text = "Confirmar nueva contraseña", Font = UITheme.SmallFont, ForeColor = UITheme.TextSecondary, Location = new Point(20, 339), AutoSize = true };
        card.Controls.Add(lblPassConf);
        _txtPasswordConfirmar = new TextBox { Location = new Point(20, 357), Size = new Size(384, 26), PasswordChar = '●', Font = UITheme.BodyFont };
        card.Controls.Add(_txtPasswordConfirmar);

        // Label de error
        _lblError = new Label
        {
            Text = string.Empty,
            Font = UITheme.SmallFont,
            ForeColor = UITheme.Danger,
            Location = new Point(20, 395),
            Size = new Size(384, 40),
            TextAlign = ContentAlignment.MiddleLeft
        };
        card.Controls.Add(_lblError);

        // Botones Guardar / Cancelar
        _btnGuardar = new Button
        {
            Text = "💾 Guardar cambios",
            Size = new Size(185, 38),
            Location = new Point(20, 480),
            Cursor = Cursors.Hand
        };
        UITheme.AplicarBotonPrimario(_btnGuardar);
        _btnGuardar.Click += async (s, e) => await GuardarCambiosAsync();
        card.Controls.Add(_btnGuardar);

        _btnCancelar = new Button
        {
            Text = "Cancelar",
            Size = new Size(110, 38),
            Location = new Point(294, 480),
            Cursor = Cursors.Hand
        };
        UITheme.AplicarBotonSecundario(_btnCancelar);
        _btnCancelar.Click += (s, e) => DialogResult = DialogResult.Cancel;
        card.Controls.Add(_btnCancelar);
    }

    private async Task CargarDatosPerfilAsync()
    {
        try
        {
            var user = await _usuarioService.ObtenerPorIdAsync(_sesion.UsuarioId);
            if (user != null)
            {
                _txtNombreCompleto.Text = user.NombreCompleto;
                _txtNombreUsuario.Text = user.NombreUsuario;
                _txtEmail.Text = string.IsNullOrWhiteSpace(user.Email) ? "Sin correo asignado" : user.Email;
            }
            else
            {
                _txtNombreCompleto.Text = _sesion.NombreCompleto;
                _txtNombreUsuario.Text = _sesion.NombreUsuario;
            }
        }
        catch
        {
            _txtNombreCompleto.Text = _sesion.NombreCompleto;
            _txtNombreUsuario.Text = _sesion.NombreUsuario;
        }
    }

    private async Task GuardarCambiosAsync()
    {
        _lblError.Text = string.Empty;

        string nuevoNombre = _txtNombreCompleto.Text.Trim();
        if (string.IsNullOrWhiteSpace(nuevoNombre))
        {
            _lblError.Text = "El nombre completo no puede estar vacío.";
            _txtNombreCompleto.Focus();
            return;
        }

        string passAct = _txtPasswordActual.Text;
        string passNew = _txtPasswordNueva.Text;
        string passConf = _txtPasswordConfirmar.Text;

        bool deseaCambiarPass = !string.IsNullOrEmpty(passAct) || !string.IsNullOrEmpty(passNew) || !string.IsNullOrEmpty(passConf);

        if (deseaCambiarPass)
        {
            if (string.IsNullOrEmpty(passAct))
            {
                _lblError.Text = "Debe ingresar su contraseña actual para cambiarla.";
                _txtPasswordActual.Focus();
                return;
            }

            if (string.IsNullOrEmpty(passNew) || passNew.Length < 6)
            {
                _lblError.Text = "La nueva contraseña debe tener al menos 6 caracteres.";
                _txtPasswordNueva.Focus();
                return;
            }

            if (passNew != passConf)
            {
                _lblError.Text = "La nueva contraseña y su confirmación no coinciden.";
                _txtPasswordConfirmar.Focus();
                return;
            }
        }

        try
        {
            _btnGuardar.Enabled = false;

            // 1. Cambiar Contraseña si se solicitó
            if (deseaCambiarPass)
            {
                bool passOk = await _authService.CambiarPasswordVoluntarioAsync(_sesion.UsuarioId, passAct, passNew);
                if (!passOk)
                {
                    _lblError.Text = "La contraseña actual es incorrecta.";
                    _txtPasswordActual.SelectAll();
                    _txtPasswordActual.Focus();
                    _btnGuardar.Enabled = true;
                    return;
                }
            }

            // 2. Actualizar Nombre Completo
            await _usuarioService.ActualizarMiPerfilAsync(_sesion.UsuarioId, nuevoNombre);

            PerfilActualizado = true;
            MessageBox.Show("Perfil y datos de seguridad actualizados con éxito.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _lblError.Text = $"Error: {ex.Message}";
            _btnGuardar.Enabled = true;
        }
    }
}
