using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Interfaces;

namespace SistemaCelulares.App.Forms;

public class ResetPasswordDialog : Form
{
    private readonly IAuthService _authService;
    private readonly int _adminId;
    private readonly int _targetUsuarioId;
    private readonly string _targetUsuarioNombre;

    private TextBox _txtPasswordTemporal = null!;
    private Button _btnReset = null!;
    private Label _lblError = null!;

    public ResetPasswordDialog(IAuthService authService, int adminId, int targetUsuarioId, string targetUsuarioNombre)
    {
        _authService = authService;
        _adminId = adminId;
        _targetUsuarioId = targetUsuarioId;
        _targetUsuarioNombre = targetUsuarioNombre;
        InitializeCustomComponents();
    }

    private void InitializeCustomComponents()
    {
        Text = "Resetear Contraseña";
        Size = new Size(420, 320);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.AppBg;
        Font = UITheme.BodyFont;

        var card = new Panel
        {
            Size = new Size(360, 250),
            Location = new Point(22, 15),
            BackColor = Color.White,
            Padding = new Padding(16)
        };
        Controls.Add(card);

        var lblTitulo = new Label
        {
            Text = $"Resetear clave para: {_targetUsuarioNombre}",
            Font = UITheme.SubtitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(16, 12),
            Size = new Size(328, 25)
        };
        card.Controls.Add(lblTitulo);

        var lblAviso = new Label
        {
            Text = "Se asignará una contraseña temporal. El usuario estará obligado a cambiarla en su siguiente inicio de sesión.",
            Font = UITheme.SmallFont,
            ForeColor = UITheme.TextMuted,
            Location = new Point(16, 40),
            Size = new Size(328, 35)
        };
        card.Controls.Add(lblAviso);

        var lblClave = new Label
        {
            Text = "Nueva Contraseña Temporal (mín. 6 car.)",
            Font = UITheme.SectionFont,
            Location = new Point(16, 85),
            AutoSize = true
        };
        card.Controls.Add(lblClave);

        _txtPasswordTemporal = new TextBox
        {
            Location = new Point(16, 110),
            Size = new Size(328, 30),
            Font = new Font("Segoe UI", 10F),
            Text = "TempPass2026!"
        };
        card.Controls.Add(_txtPasswordTemporal);

        _lblError = new Label
        {
            Text = string.Empty,
            Font = UITheme.SmallFont,
            ForeColor = UITheme.Danger,
            Location = new Point(16, 145),
            Size = new Size(328, 25)
        };
        card.Controls.Add(_lblError);

        var panelBotones = new FlowLayoutPanel
        {
            Location = new Point(16, 180),
            Size = new Size(328, 45),
            FlowDirection = FlowDirection.RightToLeft
        };

        var btnCancelar = new Button
        {
            Text = "Cancelar",
            Size = new Size(95, 36),
            DialogResult = DialogResult.Cancel
        };
        UITheme.AplicarBotonSecundario(btnCancelar);
        panelBotones.Controls.Add(btnCancelar);

        _btnReset = new Button
        {
            Text = "Confirmar Reset",
            Size = new Size(130, 36)
        };
        UITheme.AplicarBotonPeligro(_btnReset);
        _btnReset.Click += async (s, e) => await EjecutarResetAsync();
        panelBotones.Controls.Add(_btnReset);

        card.Controls.Add(panelBotones);
    }

    private async Task EjecutarResetAsync()
    {
        _lblError.Text = string.Empty;
        var tempPass = _txtPasswordTemporal.Text.Trim();

        if (string.IsNullOrWhiteSpace(tempPass) || tempPass.Length < 6)
        {
            _lblError.Text = "La contraseña temporal debe tener al menos 6 caracteres.";
            return;
        }

        _btnReset.Enabled = false;
        try
        {
            var res = await _authService.ResetPasswordAsync(_adminId, _targetUsuarioId, tempPass);
            if (res)
            {
                MessageBox.Show($"La contraseña fue reseteada con éxito.\nContraseña temporal: {tempPass}\n\nEl usuario deberá cambiarla al iniciar sesión.", "Reset Exitoso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                _lblError.Text = "No se pudo realizar el reseteo.";
            }
        }
        catch (Exception ex)
        {
            _lblError.Text = ex.Message;
        }
        finally
        {
            _btnReset.Enabled = true;
        }
    }
}
