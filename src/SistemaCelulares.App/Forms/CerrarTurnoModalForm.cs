using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class CerrarTurnoModalForm : Form
{
    private readonly ITurnoService _turnoService;
    private readonly SesionUsuario _sesion;
    private readonly Turno _turno;

    private Label _lblMontoApertura = null!;
    private Label _lblVentasEfectivo = null!;
    private Label _lblMontoEsperado = null!;
    private NumericUpDown _numMontoCierre = null!;
    private Label _lblDiferencia = null!;
    private TextBox _txtObservaciones = null!;
    private Label _lblError = null!;
    private Button _btnCerrar = null!;

    public CerrarTurnoModalForm(ITurnoService turnoService, SesionUsuario sesion, Turno turno)
    {
        _turnoService = turnoService;
        _sesion = sesion;
        _turno = turno;
        InitializeCustomComponents();
        CalcularDiferencia();
    }

    private void InitializeCustomComponents()
    {
        Text = "Cierre y Arqueo de Turno de Caja";
        Size = new Size(520, 640);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.AppBg;
        Font = UITheme.BodyFont;

        var card = new Panel
        {
            Size = new Size(440, 480),
            Location = new Point(22, 16),
            BackColor = Color.White,
            Padding = new Padding(20)
        };
        Controls.Add(card);

        var lblTitulo = new Label
        {
            Text = "Cierre y Arqueo de Caja",
            Font = UITheme.SubtitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(20, 15),
            Size = new Size(400, 25),
            TextAlign = ContentAlignment.MiddleCenter
        };
        card.Controls.Add(lblTitulo);

        var lblDetalle = new Label
        {
            Text = $"Cajero: {_turno.UsuarioApertura?.NombreCompleto ?? _sesion.NombreCompleto} | Apertura: {_turno.FechaApertura.ToLocalTime():dd/MM/yyyy hh:mm tt}",
            Font = UITheme.SmallFont,
            ForeColor = UITheme.TextMuted,
            Location = new Point(20, 42),
            Size = new Size(400, 20),
            TextAlign = ContentAlignment.MiddleCenter
        };
        card.Controls.Add(lblDetalle);

        // Panel de Resumen Calculado
        var panelResumen = new Panel
        {
            Location = new Point(20, 75),
            Size = new Size(400, 115),
            BackColor = UITheme.PrimaryLight,
            Padding = new Padding(15)
        };

        var lblTitApertura = new Label { Text = "Monto Inicial (Apertura):", Location = new Point(15, 12), AutoSize = true, Font = UITheme.BodyFont };
        panelResumen.Controls.Add(lblTitApertura);
        _lblMontoApertura = new Label { Text = AppCulture.FormatearMoneda(_turno.MontoApertura), Location = new Point(240, 12), AutoSize = true, Font = UITheme.SectionFont, ForeColor = UITheme.DarkBg };
        panelResumen.Controls.Add(_lblMontoApertura);

        var lblTitVentas = new Label { Text = "(+) Ventas en Efectivo:", Location = new Point(15, 42), AutoSize = true, Font = UITheme.BodyFont };
        panelResumen.Controls.Add(lblTitVentas);
        _lblVentasEfectivo = new Label { Text = AppCulture.FormatearMoneda(_turno.TotalVentasEfectivo), Location = new Point(240, 42), AutoSize = true, Font = UITheme.SectionFont, ForeColor = UITheme.Success };
        panelResumen.Controls.Add(_lblVentasEfectivo);

        var lblTitEsp = new Label { Text = "(=) Total Esperado en Caja:", Location = new Point(15, 75), AutoSize = true, Font = UITheme.SectionFont };
        panelResumen.Controls.Add(lblTitEsp);
        var esperado = _turno.MontoApertura + _turno.TotalVentasEfectivo;
        _lblMontoEsperado = new Label { Text = AppCulture.FormatearMoneda(esperado), Location = new Point(240, 75), AutoSize = true, Font = UITheme.SubtitleFont, ForeColor = UITheme.Primary };
        panelResumen.Controls.Add(_lblMontoEsperado);

        card.Controls.Add(panelResumen);

        // Dinero Físico Contado (Arqueo)
        var lblContado = new Label
        {
            Text = "Efectivo Contado Físicamente (RD$):",
            Font = UITheme.SectionFont,
            Location = new Point(20, 205),
            AutoSize = true
        };
        card.Controls.Add(lblContado);

        _numMontoCierre = new NumericUpDown
        {
            Location = new Point(20, 230),
            Size = new Size(400, 32),
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            DecimalPlaces = 2,
            ThousandsSeparator = true,
            Maximum = 2000000m,
            Value = esperado
        };
        _numMontoCierre.ValueChanged += (s, e) => CalcularDiferencia();
        card.Controls.Add(_numMontoCierre);

        // Panel de Diferencia
        _lblDiferencia = new Label
        {
            Location = new Point(20, 275),
            Size = new Size(400, 30),
            Font = UITheme.SectionFont,
            TextAlign = ContentAlignment.MiddleCenter
        };
        card.Controls.Add(_lblDiferencia);

        // Observaciones
        var lblObs = new Label { Text = "Observaciones de Cierre (opcional):", Font = UITheme.BodyFont, Location = new Point(20, 315), AutoSize = true };
        card.Controls.Add(lblObs);

        _txtObservaciones = new TextBox
        {
            Location = new Point(20, 337),
            Size = new Size(400, 28)
        };
        card.Controls.Add(_txtObservaciones);

        // Error
        _lblError = new Label { Text = string.Empty, Font = UITheme.SmallFont, ForeColor = UITheme.Danger, Location = new Point(20, 375), Size = new Size(400, 25) };
        card.Controls.Add(_lblError);

        // Botones
        var panelBotones = new FlowLayoutPanel
        {
            Location = new Point(20, 410),
            Size = new Size(400, 45),
            FlowDirection = FlowDirection.RightToLeft
        };

        var btnCancelar = new Button { Text = "Cancelar", Size = new Size(95, 38), DialogResult = DialogResult.Cancel };
        UITheme.AplicarBotonSecundario(btnCancelar);
        panelBotones.Controls.Add(btnCancelar);

        _btnCerrar = new Button { Text = "Confirmar Cierre", Size = new Size(140, 38) };
        UITheme.AplicarBotonPeligro(_btnCerrar);
        _btnCerrar.Click += async (s, e) => await ProcesarCierreAsync();
        panelBotones.Controls.Add(_btnCerrar);

        card.Controls.Add(panelBotones);
    }

    private void CalcularDiferencia()
    {
        var esperado = _turno.MontoApertura + _turno.TotalVentasEfectivo;
        var contado = _numMontoCierre.Value;
        var diff = contado - esperado;

        if (diff == 0)
        {
            _lblDiferencia.Text = "✅ Cuadre Exacto (Diferencia: RD$0.00)";
            _lblDiferencia.ForeColor = UITheme.Success;
            _lblDiferencia.BackColor = Color.FromArgb(236, 253, 245);
        }
        else if (diff < 0)
        {
            _lblDiferencia.Text = $"⚠️ Faltante en Caja: {AppCulture.FormatearMoneda(Math.Abs(diff))}";
            _lblDiferencia.ForeColor = UITheme.Danger;
            _lblDiferencia.BackColor = Color.FromArgb(254, 242, 242);
        }
        else
        {
            _lblDiferencia.Text = $"ℹ️ Sobrante en Caja: {AppCulture.FormatearMoneda(diff)}";
            _lblDiferencia.ForeColor = UITheme.Warning;
            _lblDiferencia.BackColor = Color.FromArgb(255, 251, 235);
        }
    }

    private async Task ProcesarCierreAsync()
    {
        _lblError.Text = string.Empty;
        var montoCierre = _numMontoCierre.Value;

        var esperado = _turno.MontoApertura + _turno.TotalVentasEfectivo;
        var diff = montoCierre - esperado;

        if (diff != 0)
        {
            var tipo = diff < 0 ? "FALTANTE" : "SOBRANTE";
            var conf = MessageBox.Show(
                $"Existe una diferencia de {tipo} de {AppCulture.FormatearMoneda(Math.Abs(diff))}.\n\n¿Desea registrar el cierre de caja de todos modos?",
                "Confirmación de Arqueo",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (conf != DialogResult.Yes) return;
        }

        _btnCerrar.Enabled = false;
        try
        {
            var cerrado = await _turnoService.CerrarTurnoAsync(_turno.Id, _sesion.UsuarioId, montoCierre, _txtObservaciones.Text);
            MessageBox.Show($"Turno de caja #{cerrado.Id} cerrado correctamente.", "Cierre Exitoso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _lblError.Text = ex.Message;
        }
        finally
        {
            _btnCerrar.Enabled = true;
        }
    }
}
