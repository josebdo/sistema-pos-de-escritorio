using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class AbrirTurnoModalForm : Form
{
    private readonly ITurnoService _turnoService;
    private readonly SesionUsuario _sesion;

    private NumericUpDown _numMontoApertura = null!;
    private TextBox _txtObservaciones = null!;
    private Label _lblError = null!;
    private Button _btnAbrir = null!;

    public AbrirTurnoModalForm(ITurnoService turnoService, SesionUsuario sesion)
    {
        _turnoService = turnoService;
        _sesion = sesion;
        InitializeCustomComponents();
    }

    private void InitializeCustomComponents()
    {
        Text = "Apertura de Turno de Caja";
        Size = new Size(460, 420);
        StartPosition = FormStartPosition.CenterScreen;
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

        var lblIcon = new Label
        {
            Text = "💵",
            Font = new Font("Segoe UI Emoji", 26F),
            AutoSize = false,
            Size = new Size(50, 40),
            Location = new Point(175, 10),
            TextAlign = ContentAlignment.MiddleCenter
        };
        card.Controls.Add(lblIcon);

        var lblTitulo = new Label
        {
            Text = "Apertura de Caja",
            Font = UITheme.SubtitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(20, 55),
            Size = new Size(360, 25),
            TextAlign = ContentAlignment.MiddleCenter
        };
        card.Controls.Add(lblTitulo);

        var lblCajero = new Label
        {
            Text = $"Cajero: {_sesion.NombreCompleto}",
            Font = UITheme.SmallFont,
            ForeColor = UITheme.TextMuted,
            Location = new Point(20, 80),
            Size = new Size(360, 20),
            TextAlign = ContentAlignment.MiddleCenter
        };
        card.Controls.Add(lblCajero);

        // Monto Inicial
        var lblMonto = new Label
        {
            Text = "Monto Inicial en Efectivo (RD$):",
            Font = UITheme.SectionFont,
            Location = new Point(20, 115),
            AutoSize = true
        };
        card.Controls.Add(lblMonto);

        _numMontoApertura = new NumericUpDown
        {
            Location = new Point(20, 140),
            Size = new Size(360, 32),
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            DecimalPlaces = 2,
            ThousandsSeparator = true,
            Maximum = 1000000m,
            Value = 1000m
        };
        card.Controls.Add(_numMontoApertura);

        // Observaciones
        var lblObs = new Label
        {
            Text = "Observaciones (opcional):",
            Font = UITheme.BodyFont,
            Location = new Point(20, 180),
            AutoSize = true
        };
        card.Controls.Add(lblObs);

        _txtObservaciones = new TextBox
        {
            Location = new Point(20, 202),
            Size = new Size(360, 28)
        };
        card.Controls.Add(_txtObservaciones);

        // Error
        _lblError = new Label
        {
            Text = string.Empty,
            Font = UITheme.SmallFont,
            ForeColor = UITheme.Danger,
            Location = new Point(20, 235),
            Size = new Size(360, 25)
        };
        card.Controls.Add(_lblError);

        // Botones
        var panelBotones = new FlowLayoutPanel
        {
            Location = new Point(20, 270),
            Size = new Size(360, 45),
            FlowDirection = FlowDirection.RightToLeft
        };

        var btnCancelar = new Button { Text = "Cancelar", Size = new Size(95, 38), DialogResult = DialogResult.Cancel };
        UITheme.AplicarBotonSecundario(btnCancelar);
        panelBotones.Controls.Add(btnCancelar);

        _btnAbrir = new Button { Text = "Abrir Turno", Size = new Size(120, 38) };
        UITheme.AplicarBotonPrimario(_btnAbrir);
        _btnAbrir.Click += async (s, e) => await ProcesarAperturaAsync();
        panelBotones.Controls.Add(_btnAbrir);

        card.Controls.Add(panelBotones);
    }

    private async Task ProcesarAperturaAsync()
    {
        _lblError.Text = string.Empty;
        var monto = _numMontoApertura.Value;

        if (monto < 0)
        {
            _lblError.Text = "El monto no puede ser negativo.";
            return;
        }

        _btnAbrir.Enabled = false;
        try
        {
            var turno = await _turnoService.AbrirTurnoAsync(_sesion.UsuarioId, monto, _txtObservaciones.Text);
            MessageBox.Show($"Turno de caja #{turno.Id} abierto exitosamente con {AppCulture.FormatearMoneda(monto)}.", "Turno Abierto", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _lblError.Text = ex.Message;
        }
        finally
        {
            _btnAbrir.Enabled = true;
        }
    }
}
