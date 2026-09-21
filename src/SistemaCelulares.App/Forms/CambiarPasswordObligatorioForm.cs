using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class CambiarPasswordObligatorioForm : Form
{
    private readonly IAuthService _authService;
    private readonly SesionUsuario _sesion;

    private TextBox _txtNueva = null!;
    private TextBox _txtConfirmar = null!;
    private Label _lblError = null!;
    private Button _btnGuardar = null!;

    public CambiarPasswordObligatorioForm(IAuthService authService, SesionUsuario sesion)
    {
        _authService = authService;
        _sesion = sesion;
        InitializeCustomComponents();
    }

    private void InitializeCustomComponents()
    {
        Text = "Cambio Obligatorio de Contraseña";
        Size = new Size(440, 480);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.AppBg;
        Font = UITheme.BodyFont;

        var card = new Panel
        {
            Size = new Size(380, 400),
            Location = new Point(22, 20),
            BackColor = Color.White,
            Padding = new Padding(20)
        };
        Controls.Add(card);

        var lblIcon = new Label
        {
            Text = "🔒",
            Font = new Font("Segoe UI Emoji", 28F),
            AutoSize = false,
            Size = new Size(50, 45),
            Location = new Point(165, 10),
            TextAlign = ContentAlignment.MiddleCenter
        };
        card.Controls.Add(lblIcon);

        var lblTitulo = new Label
        {
            Text = "Actualización de Seguridad",
            Font = UITheme.SubtitleFont,
            ForeColor = UITheme.DarkBg,
            AutoSize = false,
            Size = new Size(340, 25),
            Location = new Point(20, 60),
            TextAlign = ContentAlignment.MiddleCenter
        };
        card.Controls.Add(lblTitulo);

        var lblAviso = new Label
        {
            Text = $"Hola {_sesion.NombreCompleto}, por motivos de seguridad debe establecer una nueva contraseña personal antes de ingresar al sistema.",
            Font = UITheme.SmallFont,
            ForeColor = UITheme.TextMuted,
            AutoSize = false,
            Size = new Size(340, 40),
            Location = new Point(20, 90),
            TextAlign = ContentAlignment.MiddleCenter
        };
        card.Controls.Add(lblAviso);

        // Nueva Contraseña
        var lblNueva = new Label
        {
            Text = "Nueva Contraseña (mínimo 6 caracteres)",
            Font = UITheme.SectionFont,
            Location = new Point(20, 140),
            AutoSize = true
        };
        card.Controls.Add(lblNueva);

        _txtNueva = new TextBox
        {
            Location = new Point(20, 165),
            Size = new Size(340, 30),
            Font = new Font("Segoe UI", 10.5F),
            UseSystemPasswordChar = true
        };
        card.Controls.Add(_txtNueva);

        // Confirmar Contraseña
        var lblConf = new Label
        {
            Text = "Confirmar Nueva Contraseña",
            Font = UITheme.SectionFont,
            Location = new Point(20, 205),
            AutoSize = true
        };
        card.Controls.Add(lblConf);

        _txtConfirmar = new TextBox
        {
            Location = new Point(20, 230),
            Size = new Size(340, 30),
            Font = new Font("Segoe UI", 10.5F),
            UseSystemPasswordChar = true
        };
        card.Controls.Add(_txtConfirmar);

        // Error
        _lblError = new Label
        {
            Text = string.Empty,
            Font = UITheme.SmallFont,
            ForeColor = UITheme.Danger,
            Location = new Point(20, 270),
            Size = new Size(340, 35),
            TextAlign = ContentAlignment.TopCenter
        };
        card.Controls.Add(_lblError);

        // Botón Guardar
        _btnGuardar = new Button
        {
            Text = "Establecer Contraseña y Entrar",
            Location = new Point(20, 315),
            Size = new Size(340, 42)
        };
        UITheme.AplicarBotonPrimario(_btnGuardar);
        _btnGuardar.Click += async (s, e) => await GuardarPasswordAsync();
        card.Controls.Add(_btnGuardar);

        AcceptButton = _btnGuardar;
    }

    private async Task GuardarPasswordAsync()
    {
        _lblError.Text = string.Empty;
        var nueva = _txtNueva.Text;
        var conf = _txtConfirmar.Text;

        if (string.IsNullOrWhiteSpace(nueva) || nueva.Length < 6)
        {
            _lblError.Text = "La nueva contraseña debe tener al menos 6 caracteres.";
            return;
        }

        if (nueva != conf)
        {
            _lblError.Text = "Las contraseñas no coinciden.";
            return;
        }

        _btnGuardar.Enabled = false;
        try
        {
            var ok = await _authService.CambiarPasswordObligatorioAsync(_sesion.UsuarioId, nueva);
            if (ok)
            {
                MessageBox.Show("Contraseña actualizada con éxito. ¡Bienvenido al sistema!", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                _lblError.Text = "No se pudo actualizar la contraseña. Verifique el usuario.";
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
