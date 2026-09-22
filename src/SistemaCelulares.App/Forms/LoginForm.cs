using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class LoginForm : Form
{
    private readonly IAuthService _authService;
    public SesionUsuario? SesionIniciada { get; private set; }

    private TextBox _txtUsuario = null!;
    private TextBox _txtPassword = null!;
    private Label _lblError = null!;
    private Button _btnLogin = null!;
    private Button _btnVerPassword = null!;
    private bool _passwordOculto = true;

    public LoginForm(IAuthService authService)
    {
        _authService = authService;
        InitializeCustomComponents();
    }

    private void InitializeCustomComponents()
    {
        Text = "Veyra POS - Inicio de Sesión";
        Size = new Size(460, 580);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.AppBg;
        Font = UITheme.BodyFont;

        // Tarjeta Central
        var card = new Panel
        {
            Size = new Size(390, 490),
            Location = new Point(30, 25),
            BackColor = Color.White,
            Padding = new Padding(24)
        };
        Controls.Add(card);

        // Header / Logo
        var lblIcon = new Label
        {
            Text = "📱",
            Font = new Font("Segoe UI Emoji", 32F),
            AutoSize = false,
            Size = new Size(60, 55),
            Location = new Point(165, 15),
            TextAlign = ContentAlignment.MiddleCenter
        };
        card.Controls.Add(lblIcon);

        var lblTitulo = new Label
        {
            Text = "Veyra POS",
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            ForeColor = Color.FromArgb(26, 35, 126),
            AutoSize = false,
            Size = new Size(342, 36),
            Location = new Point(24, 70),
            TextAlign = ContentAlignment.MiddleCenter
        };
        card.Controls.Add(lblTitulo);

        var lblSubtitulo = new Label
        {
            Text = "Sistema Punto de Venta y Gestión de Celulares",
            Font = UITheme.SmallFont,
            ForeColor = UITheme.TextMuted,
            AutoSize = false,
            Size = new Size(342, 20),
            Location = new Point(24, 106),
            TextAlign = ContentAlignment.MiddleCenter
        };
        card.Controls.Add(lblSubtitulo);

        // Campo Usuario
        var lblUser = new Label
        {
            Text = "Usuario",
            Font = UITheme.SectionFont,
            ForeColor = UITheme.TextPrimary,
            Location = new Point(24, 135),
            AutoSize = true
        };
        card.Controls.Add(lblUser);

        _txtUsuario = new TextBox
        {
            Location = new Point(24, 160),
            Size = new Size(342, 35),
            Font = new Font("Segoe UI", 11F),
            BorderStyle = BorderStyle.FixedSingle
        };
        card.Controls.Add(_txtUsuario);

        // Campo Contraseña
        var lblPass = new Label
        {
            Text = "Contraseña",
            Font = UITheme.SectionFont,
            ForeColor = UITheme.TextPrimary,
            Location = new Point(24, 210),
            AutoSize = true
        };
        card.Controls.Add(lblPass);

        var panelPass = new Panel
        {
            Location = new Point(24, 235),
            Size = new Size(342, 35),
            BorderStyle = BorderStyle.FixedSingle
        };

        _txtPassword = new TextBox
        {
            Location = new Point(2, 4),
            Size = new Size(295, 30),
            Font = new Font("Segoe UI", 11F),
            UseSystemPasswordChar = true,
            BorderStyle = BorderStyle.None
        };
        panelPass.Controls.Add(_txtPassword);

        _btnVerPassword = new Button
        {
            Text = "👁",
            Location = new Point(300, 1),
            Size = new Size(38, 30),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            BackColor = Color.Transparent
        };
        _btnVerPassword.FlatAppearance.BorderSize = 0;
        _btnVerPassword.Click += (s, e) =>
        {
            _passwordOculto = !_passwordOculto;
            _txtPassword.UseSystemPasswordChar = _passwordOculto;
        };
        panelPass.Controls.Add(_btnVerPassword);
        card.Controls.Add(panelPass);

        // Mensaje de Error
        _lblError = new Label
        {
            Text = string.Empty,
            Font = UITheme.SmallFont,
            ForeColor = UITheme.Danger,
            Location = new Point(24, 280),
            Size = new Size(342, 35),
            TextAlign = ContentAlignment.TopCenter
        };
        card.Controls.Add(_lblError);

        // Botón Login
        _btnLogin = new Button
        {
            Text = "Iniciar Sesión",
            Location = new Point(24, 320),
            Size = new Size(342, 45),
            Cursor = Cursors.Hand
        };
        UITheme.AplicarBotonPrimario(_btnLogin);
        _btnLogin.Click += async (s, e) => await ProcesarLoginAsync();
        card.Controls.Add(_btnLogin);

        // Acceso rápido / Tips de roles iniciales para prueba
        var lblQuick = new Label
        {
            Text = "Usuarios de prueba:\n• superadmin (SuperAdmin123!)\n• admin (Admin123!)\n• cajero (Cajero123!)",
            Font = new Font("Segoe UI", 8F),
            ForeColor = UITheme.TextMuted,
            Location = new Point(24, 380),
            Size = new Size(342, 60),
            TextAlign = ContentAlignment.MiddleCenter
        };
        card.Controls.Add(lblQuick);

        AcceptButton = _btnLogin;
    }

    private async Task ProcesarLoginAsync()
    {
        _lblError.Text = string.Empty;
        var usuario = _txtUsuario.Text.Trim();
        var password = _txtPassword.Text;

        if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(password))
        {
            _lblError.Text = "Por favor ingrese usuario y contraseña.";
            return;
        }

        _btnLogin.Enabled = false;
        _btnLogin.Text = "Verificando...";

        try
        {
            var resultado = await _authService.LoginAsync(usuario, password);

            if (resultado.Status == LoginStatus.DebeCambiarPassword)
            {
                // Abrir pantalla obligatoria de cambio de contraseña
                var modalCambio = new CambiarPasswordObligatorioForm(_authService, resultado.Sesion!);
                var dialogRes = modalCambio.ShowDialog(this);
                if (dialogRes == DialogResult.OK)
                {
                    SesionIniciada = resultado.Sesion;
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    _lblError.Text = "Debe cambiar su contraseña para ingresar al sistema.";
                    _txtPassword.Clear();
                }
            }
            else if (resultado.Status == LoginStatus.Exitoso)
            {
                SesionIniciada = resultado.Sesion;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                _lblError.Text = resultado.Mensaje;
            }
        }
        catch (Exception ex)
        {
            _lblError.Text = $"Error: {ex.Message}";
        }
        finally
        {
            _btnLogin.Enabled = true;
            _btnLogin.Text = "Iniciar Sesión";
        }
    }
}
