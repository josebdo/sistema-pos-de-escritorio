using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class CobroModalForm : Form
{
    private readonly IPagoService _pagoService;
    private readonly decimal _montoTotalOriginal;
    private readonly int _usuarioId;
    private readonly int? _turnoId;

    public ResultadoPagoDto? ResultadoPago { get; private set; }

    // Controles Principales
    private Label _lblMontoTotal = null!;
    private TabControl _tabControl = null!;
    private Button _btnConfirmar = null!;
    private Button _btnCancelar = null!;
    private TextBox _txtNotas = null!;

    // Controles Tab Efectivo
    private NumericUpDown _numEfectivoEntregado = null!;
    private Label _lblEfectivoVuelto = null!;
    private Label _lblEfectivoEstado = null!;

    // Controles Tab Transferencia
    private ComboBox _cmbBanco = null!;
    private TextBox _txtReferenciaTransferencia = null!;
    private CheckBox _chkTransferenciaVerificada = null!;
    private Label _lblTransferenciaAviso = null!;

    // Controles Tab Tarjeta
    private RadioButton _rbDebito = null!;
    private RadioButton _rbCredito = null!;
    private ComboBox _cmbPosTerminal = null!;
    private TextBox _txtAutorizacionPos = null!;

    // Controles Tab Mixto
    private NumericUpDown _numMixtoEfectivo = null!;
    private NumericUpDown _numMixtoEfectivoEntregado = null!;
    private Label _lblMixtoVuelto = null!;
    private NumericUpDown _numMixtoTransferencia = null!;
    private ComboBox _cmbMixtoBanco = null!;
    private TextBox _txtMixtoReferencia = null!;
    private CheckBox _chkMixtoTransferenciaVerificada = null!;
    private NumericUpDown _numMixtoTarjeta = null!;
    private RadioButton _rbMixtoDebito = null!;
    private RadioButton _rbMixtoCredito = null!;
    private TextBox _txtMixtoAutorizacion = null!;
    private Label _lblMixtoBalanceRestante = null!;

    public CobroModalForm(IPagoService pagoService, decimal montoTotal, int usuarioId, int? turnoId = null)
    {
        _pagoService = pagoService;
        _montoTotalOriginal = montoTotal;
        _usuarioId = usuarioId;
        _turnoId = turnoId;

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        Text = "Cobro y Selección de Método de Pago";
        Size = new Size(680, 680);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(248, 249, 250);
        KeyPreview = true;

        KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Escape) Close();
            if (e.KeyCode == Keys.F10) _btnConfirmar.PerformClick();
        };

        // Header
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 100,
            BackColor = Color.FromArgb(24, 43, 73),
            Padding = new Padding(20, 15, 20, 15)
        };

        var lblTitulo = new Label
        {
            Text = "TOTAL A COBRAR",
            ForeColor = Color.FromArgb(173, 181, 189),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 22
        };

        _lblMontoTotal = new Label
        {
            Text = $"RD$ {_montoTotalOriginal:N2}",
            ForeColor = Color.FromArgb(46, 204, 113),
            Font = new Font("Segoe UI", 26, FontStyle.Bold),
            Dock = DockStyle.Fill
        };

        pnlHeader.Controls.Add(_lblMontoTotal);
        pnlHeader.Controls.Add(lblTitulo);

        // TabControl para Métodos de Pago
        _tabControl = new TabControl
        {
            Location = new Point(20, 115),
            Size = new Size(625, 420),
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };

        _tabControl.TabPages.Add(CrearTabEfectivo());
        _tabControl.TabPages.Add(CrearTabTransferencia());
        _tabControl.TabPages.Add(CrearTabTarjeta());
        _tabControl.TabPages.Add(CrearTabMixto());

        _tabControl.SelectedIndexChanged += (s, e) => ActualizarValidaciones();

        // Panel Inferior (Notas, Botones)
        var lblNotas = new Label
        {
            Text = "Notas / Comentarios (opcional):",
            Location = new Point(20, 545),
            AutoSize = true,
            Font = new Font("Segoe UI", 9)
        };

        _txtNotas = new TextBox
        {
            Location = new Point(20, 568),
            Size = new Size(340, 25),
            Font = new Font("Segoe UI", 9)
        };

        _btnConfirmar = new Button
        {
            Text = "✔ Confirmar Cobro (F10)",
            Location = new Point(375, 555),
            Size = new Size(160, 42),
            BackColor = Color.FromArgb(39, 174, 96),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnConfirmar.FlatAppearance.BorderSize = 0;
        _btnConfirmar.Click += async (s, e) => await ProcesarCobroAsync();

        _btnCancelar = new Button
        {
            Text = "Cancelar (Esc)",
            Location = new Point(545, 555),
            Size = new Size(100, 42),
            BackColor = Color.FromArgb(149, 165, 166),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnCancelar.FlatAppearance.BorderSize = 0;
        _btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        Controls.Add(pnlHeader);
        Controls.Add(_tabControl);
        Controls.Add(lblNotas);
        Controls.Add(_txtNotas);
        Controls.Add(_btnConfirmar);
        Controls.Add(_btnCancelar);
    }

    private TabPage CrearTabEfectivo()
    {
        var tab = new TabPage("💵 Efectivo") { BackColor = Color.White, Padding = new Padding(15) };

        var lblMonto = new Label
        {
            Text = $"Monto a cubrir: RD$ {_montoTotalOriginal:N2}",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Location = new Point(20, 15),
            AutoSize = true
        };

        var lblEntregado = new Label
        {
            Text = "Dinero Entregado por el Cliente (RD$):",
            Location = new Point(20, 50),
            AutoSize = true,
            Font = new Font("Segoe UI", 10)
        };

        _numEfectivoEntregado = new NumericUpDown
        {
            Location = new Point(20, 75),
            Size = new Size(200, 32),
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            Maximum = 1000000,
            DecimalPlaces = 2,
            ThousandsSeparator = true,
            Value = _montoTotalOriginal
        };
        _numEfectivoEntregado.ValueChanged += (s, e) => RecalcularVueltoEfectivo();

        // Billetes rápidos RD$
        var pnlBilletes = new FlowLayoutPanel
        {
            Location = new Point(240, 50),
            Size = new Size(350, 85),
            AutoScroll = false
        };

        int[] denominaciones = { 50, 100, 200, 500, 1000, 2000 };
        foreach (var den in denominaciones)
        {
            var btnBillete = new Button
            {
                Text = $"+RD${den}",
                Size = new Size(80, 32),
                BackColor = Color.FromArgb(236, 240, 241),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBillete.Click += (s, e) =>
            {
                _numEfectivoEntregado.Value += den;
            };
            pnlBilletes.Controls.Add(btnBillete);
        }

        var btnExacto = new Button
        {
            Text = "Exacto",
            Size = new Size(80, 32),
            BackColor = Color.FromArgb(52, 152, 219),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnExacto.Click += (s, e) => _numEfectivoEntregado.Value = _montoTotalOriginal;
        pnlBilletes.Controls.Add(btnExacto);

        // Panel de Vuelto
        var pnlVuelto = new Panel
        {
            Location = new Point(20, 160),
            Size = new Size(560, 150),
            BackColor = Color.FromArgb(240, 248, 255),
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(15)
        };

        var lblVueltoTitulo = new Label
        {
            Text = "DEVUELTA / CAMBIO A RETORNAR:",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(52, 73, 94),
            Location = new Point(15, 15),
            AutoSize = true
        };

        _lblEfectivoVuelto = new Label
        {
            Text = "RD$ 0.00",
            Font = new Font("Segoe UI", 28, FontStyle.Bold),
            ForeColor = Color.FromArgb(39, 174, 96),
            Location = new Point(15, 45),
            Size = new Size(520, 50)
        };

        _lblEfectivoEstado = new Label
        {
            Text = "✔ Pago completo y exacto.",
            Font = new Font("Segoe UI", 10, FontStyle.Italic),
            ForeColor = Color.FromArgb(41, 128, 185),
            Location = new Point(15, 105),
            AutoSize = true
        };

        pnlVuelto.Controls.Add(lblVueltoTitulo);
        pnlVuelto.Controls.Add(_lblEfectivoVuelto);
        pnlVuelto.Controls.Add(_lblEfectivoEstado);

        tab.Controls.Add(lblMonto);
        tab.Controls.Add(lblEntregado);
        tab.Controls.Add(_numEfectivoEntregado);
        tab.Controls.Add(pnlBilletes);
        tab.Controls.Add(pnlVuelto);

        RecalcularVueltoEfectivo();
        return tab;
    }

    private void RecalcularVueltoEfectivo()
    {
        var calc = _pagoService.CalcularVueltoEfectivo(_montoTotalOriginal, _numEfectivoEntregado.Value);
        if (calc.EsSuficiente)
        {
            _lblEfectivoVuelto.Text = $"RD$ {calc.Vuelto:N2}";
            _lblEfectivoVuelto.ForeColor = Color.FromArgb(39, 174, 96);
            _lblEfectivoEstado.Text = calc.Vuelto > 0
                ? $"✔ Entregar al cliente RD$ {calc.Vuelto:N2} de cambio."
                : "✔ Pago exacto sin cambio.";
            _lblEfectivoEstado.ForeColor = Color.FromArgb(39, 174, 96);
        }
        else
        {
            _lblEfectivoVuelto.Text = $"FALTA RD$ {calc.MontoFaltante:N2}";
            _lblEfectivoVuelto.ForeColor = Color.FromArgb(231, 76, 60);
            _lblEfectivoEstado.Text = "⚠ Monto insuficiente para cubrir el total.";
            _lblEfectivoEstado.ForeColor = Color.FromArgb(231, 76, 60);
        }
    }

    private TabPage CrearTabTransferencia()
    {
        var tab = new TabPage("🏦 Transferencia") { BackColor = Color.White, Padding = new Padding(20) };

        var lblBanco = new Label
        {
            Text = "Banco Destino (República Dominicana):",
            Location = new Point(20, 20),
            AutoSize = true,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        _cmbBanco = new ComboBox
        {
            Location = new Point(20, 48),
            Size = new Size(350, 28),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10)
        };
        _cmbBanco.Items.AddRange(new object[]
        {
            "Banreservas (Banco de Reservas)",
            "Banco Popular Dominicano",
            "Banco BHD",
            "Scotiabank República Dominicana",
            "Banco Santa Cruz",
            "Banco Promerica",
            "Banco BDI",
            "Asociación Popular de Ahorros y Préstamos (APAP)",
            "Asociación La Nacional",
            "Otro Banco / Fintech (Qik, Mio, etc.)"
        });
        _cmbBanco.SelectedIndex = 0;

        var lblRef = new Label
        {
            Text = "Número de Referencia / Comprobante (opcional):",
            Location = new Point(20, 95),
            AutoSize = true,
            Font = new Font("Segoe UI", 10)
        };

        _txtReferenciaTransferencia = new TextBox
        {
            Location = new Point(20, 120),
            Size = new Size(350, 27),
            Font = new Font("Segoe UI", 10),
            PlaceholderText = "Ej: REF-987456 o últimos 4 dígitos"
        };

        var pnlVerif = new Panel
        {
            Location = new Point(20, 175),
            Size = new Size(560, 150),
            BackColor = Color.FromArgb(254, 249, 231),
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(15)
        };

        _chkTransferenciaVerificada = new CheckBox
        {
            Text = "He verificado manualmente que los fondos ingresaron a la cuenta",
            Location = new Point(15, 15),
            Size = new Size(520, 40),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(125, 102, 8),
            Cursor = Cursors.Hand
        };
        _chkTransferenciaVerificada.CheckedChanged += (s, e) =>
        {
            if (_chkTransferenciaVerificada.Checked)
            {
                pnlVerif.BackColor = Color.FromArgb(234, 250, 234);
                _lblTransferenciaAviso.Text = "✔ Transferencia confirmada por el vendedor. La venta se registrará como Cobrada.";
                _lblTransferenciaAviso.ForeColor = Color.FromArgb(39, 174, 96);
            }
            else
            {
                pnlVerif.BackColor = Color.FromArgb(254, 249, 231);
                _lblTransferenciaAviso.Text = "⚠ Advertencia: Al no estar verificada, la venta quedará en estado 'Pendiente de Verificación' y no se contará como dinero cobrado en caja hasta su confirmación.";
                _lblTransferenciaAviso.ForeColor = Color.FromArgb(183, 149, 11);
            }
        };

        _lblTransferenciaAviso = new Label
        {
            Text = "⚠ Advertencia: Al no estar verificada, la venta quedará en estado 'Pendiente de Verificación' y no se contará como dinero cobrado en caja hasta su confirmación.",
            Location = new Point(15, 65),
            Size = new Size(520, 70),
            Font = new Font("Segoe UI", 9, FontStyle.Regular),
            ForeColor = Color.FromArgb(183, 149, 11)
        };

        pnlVerif.Controls.Add(_chkTransferenciaVerificada);
        pnlVerif.Controls.Add(_lblTransferenciaAviso);

        tab.Controls.Add(lblBanco);
        tab.Controls.Add(_cmbBanco);
        tab.Controls.Add(lblRef);
        tab.Controls.Add(_txtReferenciaTransferencia);
        tab.Controls.Add(pnlVerif);

        return tab;
    }

    private TabPage CrearTabTarjeta()
    {
        var tab = new TabPage("💳 Tarjeta (POS)") { BackColor = Color.White, Padding = new Padding(20) };

        var lblTipo = new Label
        {
            Text = "Modalidad de Tarjeta:",
            Location = new Point(20, 20),
            AutoSize = true,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        _rbDebito = new RadioButton
        {
            Text = "Tarjeta de Débito",
            Location = new Point(20, 48),
            AutoSize = true,
            Checked = true,
            Font = new Font("Segoe UI", 10)
        };

        _rbCredito = new RadioButton
        {
            Text = "Tarjeta de Crédito",
            Location = new Point(180, 48),
            AutoSize = true,
            Font = new Font("Segoe UI", 10)
        };

        var lblTerminal = new Label
        {
            Text = "Procesador / Datáfono POS:",
            Location = new Point(20, 95),
            AutoSize = true,
            Font = new Font("Segoe UI", 10)
        };

        _cmbPosTerminal = new ComboBox
        {
            Location = new Point(20, 120),
            Size = new Size(300, 28),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10)
        };
        _cmbPosTerminal.Items.AddRange(new object[]
        {
            "Cardnet",
            "Azul (Banco Popular)",
            "Visanet Dominicana",
            "Carnet POS",
            "Otro Datáfono Externo"
        });
        _cmbPosTerminal.SelectedIndex = 0;

        var lblAut = new Label
        {
            Text = "Código de Autorización / Aprobación POS (opcional):",
            Location = new Point(20, 165),
            AutoSize = true,
            Font = new Font("Segoe UI", 10)
        };

        _txtAutorizacionPos = new TextBox
        {
            Location = new Point(20, 190),
            Size = new Size(300, 27),
            Font = new Font("Segoe UI", 10),
            PlaceholderText = "Ej: 045892"
        };

        var lblInfo = new Label
        {
            Text = "ℹ Nota: El cobro se efectúa físicamente en el terminal externo. Al presionar Confirmar, se registrará la transacción aprobada.",
            Location = new Point(20, 240),
            Size = new Size(540, 50),
            Font = new Font("Segoe UI", 9, FontStyle.Italic),
            ForeColor = Color.FromArgb(127, 140, 141)
        };

        tab.Controls.Add(lblTipo);
        tab.Controls.Add(_rbDebito);
        tab.Controls.Add(_rbCredito);
        tab.Controls.Add(lblTerminal);
        tab.Controls.Add(_cmbPosTerminal);
        tab.Controls.Add(lblAut);
        tab.Controls.Add(_txtAutorizacionPos);
        tab.Controls.Add(lblInfo);

        return tab;
    }

    private TabPage CrearTabMixto()
    {
        var tab = new TabPage("🔀 Pago Mixto") { BackColor = Color.White, Padding = new Padding(15), AutoScroll = true };

        int y = 10;

        // Efectivo
        var lblSecEfectivo = new Label
        {
            Text = "1. Parte en Efectivo:",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Location = new Point(15, y),
            AutoSize = true
        };
        y += 22;

        var lblMontoE = new Label { Text = "Monto:", Location = new Point(15, y + 3), AutoSize = true };
        _numMixtoEfectivo = new NumericUpDown
        {
            Location = new Point(65, y),
            Size = new Size(110, 24),
            Maximum = 1000000,
            DecimalPlaces = 2
        };
        _numMixtoEfectivo.ValueChanged += (s, e) => RecalcularMixto();

        var lblEntregadoE = new Label { Text = "Entregado:", Location = new Point(190, y + 3), AutoSize = true };
        _numMixtoEfectivoEntregado = new NumericUpDown
        {
            Location = new Point(260, y),
            Size = new Size(110, 24),
            Maximum = 1000000,
            DecimalPlaces = 2
        };
        _numMixtoEfectivoEntregado.ValueChanged += (s, e) => RecalcularMixto();

        _lblMixtoVuelto = new Label
        {
            Text = "Vuelto: RD$0.00",
            Location = new Point(385, y + 3),
            AutoSize = true,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(39, 174, 96)
        };

        tab.Controls.Add(lblSecEfectivo);
        tab.Controls.Add(lblMontoE);
        tab.Controls.Add(_numMixtoEfectivo);
        tab.Controls.Add(lblEntregadoE);
        tab.Controls.Add(_numMixtoEfectivoEntregado);
        tab.Controls.Add(_lblMixtoVuelto);
        y += 35;

        // Transferencia
        var lblSecTrans = new Label
        {
            Text = "2. Parte en Transferencia:",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Location = new Point(15, y),
            AutoSize = true
        };
        y += 22;

        var lblMontoT = new Label { Text = "Monto:", Location = new Point(15, y + 3), AutoSize = true };
        _numMixtoTransferencia = new NumericUpDown
        {
            Location = new Point(65, y),
            Size = new Size(110, 24),
            Maximum = 1000000,
            DecimalPlaces = 2
        };
        _numMixtoTransferencia.ValueChanged += (s, e) => RecalcularMixto();

        _cmbMixtoBanco = new ComboBox
        {
            Location = new Point(190, y),
            Size = new Size(180, 24),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbMixtoBanco.Items.AddRange(new object[] { "Banreservas", "Banco Popular", "Banco BHD", "Otro" });
        _cmbMixtoBanco.SelectedIndex = 0;

        _txtMixtoReferencia = new TextBox
        {
            Location = new Point(385, y),
            Size = new Size(180, 24),
            PlaceholderText = "Referencia"
        };
        y += 28;

        _chkMixtoTransferenciaVerificada = new CheckBox
        {
            Text = "Transferencia verificada",
            Location = new Point(65, y),
            AutoSize = true,
            Font = new Font("Segoe UI", 8)
        };

        tab.Controls.Add(lblSecTrans);
        tab.Controls.Add(lblMontoT);
        tab.Controls.Add(_numMixtoTransferencia);
        tab.Controls.Add(_cmbMixtoBanco);
        tab.Controls.Add(_txtMixtoReferencia);
        tab.Controls.Add(_chkMixtoTransferenciaVerificada);
        y += 32;

        // Tarjeta
        var lblSecTarj = new Label
        {
            Text = "3. Parte en Tarjeta:",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Location = new Point(15, y),
            AutoSize = true
        };
        y += 22;

        var lblMontoTarj = new Label { Text = "Monto:", Location = new Point(15, y + 3), AutoSize = true };
        _numMixtoTarjeta = new NumericUpDown
        {
            Location = new Point(65, y),
            Size = new Size(110, 24),
            Maximum = 1000000,
            DecimalPlaces = 2
        };
        _numMixtoTarjeta.ValueChanged += (s, e) => RecalcularMixto();

        _rbMixtoDebito = new RadioButton { Text = "Débito", Location = new Point(190, y + 2), AutoSize = true, Checked = true };
        _rbMixtoCredito = new RadioButton { Text = "Crédito", Location = new Point(265, y + 2), AutoSize = true };

        _txtMixtoAutorizacion = new TextBox
        {
            Location = new Point(345, y),
            Size = new Size(150, 24),
            PlaceholderText = "Autorización POS"
        };

        tab.Controls.Add(lblSecTarj);
        tab.Controls.Add(lblMontoTarj);
        tab.Controls.Add(_numMixtoTarjeta);
        tab.Controls.Add(_rbMixtoDebito);
        tab.Controls.Add(_rbMixtoCredito);
        tab.Controls.Add(_txtMixtoAutorizacion);
        y += 35;

        // Resumen Mixto
        _lblMixtoBalanceRestante = new Label
        {
            Location = new Point(15, y),
            Size = new Size(550, 30),
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        tab.Controls.Add(_lblMixtoBalanceRestante);

        RecalcularMixto();
        return tab;
    }

    private void RecalcularMixto()
    {
        decimal suma = _numMixtoEfectivo.Value + _numMixtoTransferencia.Value + _numMixtoTarjeta.Value;
        decimal faltante = _montoTotalOriginal - suma;

        if (Math.Abs(faltante) < 0.01m)
        {
            _lblMixtoBalanceRestante.Text = $"✔ Total cubierto exactamente (RD$ {suma:N2})";
            _lblMixtoBalanceRestante.ForeColor = Color.FromArgb(39, 174, 96);
        }
        else if (faltante > 0)
        {
            _lblMixtoBalanceRestante.Text = $"⚠ Falta cubrir: RD$ {faltante:N2} (Total acumulado: RD$ {suma:N2})";
            _lblMixtoBalanceRestante.ForeColor = Color.FromArgb(231, 76, 60);
        }
        else
        {
            _lblMixtoBalanceRestante.Text = $"⚠ Sobran: RD$ {Math.Abs(faltante):N2} (Total acumulado: RD$ {suma:N2})";
            _lblMixtoBalanceRestante.ForeColor = Color.FromArgb(230, 126, 34);
        }

        if (_numMixtoEfectivo.Value > 0)
        {
            decimal vuelto = _numMixtoEfectivoEntregado.Value - _numMixtoEfectivo.Value;
            _lblMixtoVuelto.Text = vuelto >= 0 ? $"Vuelto: RD${vuelto:N2}" : $"Falta: RD${Math.Abs(vuelto):N2}";
            _lblMixtoVuelto.ForeColor = vuelto >= 0 ? Color.FromArgb(39, 174, 96) : Color.FromArgb(231, 76, 60);
        }
    }

    private void ActualizarValidaciones()
    {
        if (_tabControl.SelectedIndex == 0) RecalcularVueltoEfectivo();
        if (_tabControl.SelectedIndex == 3) RecalcularMixto();
    }

    private async Task ProcesarCobroAsync()
    {
        var request = new RegistrarPagoDto
        {
            MontoTotal = _montoTotalOriginal,
            UsuarioId = _usuarioId,
            TurnoId = _turnoId,
            Notas = _txtNotas.Text.Trim()
        };

        switch (_tabControl.SelectedIndex)
        {
            case 0: // Efectivo
                if (_numEfectivoEntregado.Value < _montoTotalOriginal)
                {
                    MessageBox.Show("El dinero entregado es menor al total a pagar.", "Pago Insuficiente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                request.Metodos.Add(new DetallePagoRequestDto
                {
                    Metodo = MetodoPago.Efectivo,
                    Monto = _montoTotalOriginal,
                    MontoEntregado = _numEfectivoEntregado.Value
                });
                break;

            case 1: // Transferencia
                request.Metodos.Add(new DetallePagoRequestDto
                {
                    Metodo = MetodoPago.Transferencia,
                    Monto = _montoTotalOriginal,
                    BancoDestino = _cmbBanco.SelectedItem?.ToString(),
                    NumeroReferencia = _txtReferenciaTransferencia.Text.Trim(),
                    EsVerificado = _chkTransferenciaVerificada.Checked
                });
                break;

            case 2: // Tarjeta
                request.Metodos.Add(new DetallePagoRequestDto
                {
                    Metodo = MetodoPago.Tarjeta,
                    Monto = _montoTotalOriginal,
                    TipoTarjeta = _rbDebito.Checked ? SubtipoTarjeta.Debito : SubtipoTarjeta.Credito,
                    NumeroAutorizacionPos = $"{_cmbPosTerminal.SelectedItem} - {_txtAutorizacionPos.Text.Trim()}"
                });
                break;

            case 3: // Mixto
                decimal suma = _numMixtoEfectivo.Value + _numMixtoTransferencia.Value + _numMixtoTarjeta.Value;
                if (Math.Abs(suma - _montoTotalOriginal) > 0.01m)
                {
                    MessageBox.Show($"La suma de los métodos mixtos (RD$ {suma:N2}) no coincide con el total (RD$ {_montoTotalOriginal:N2}).", "Descuadre en Pago Mixto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_numMixtoEfectivo.Value > 0)
                {
                    if (_numMixtoEfectivoEntregado.Value < _numMixtoEfectivo.Value)
                    {
                        MessageBox.Show("El efectivo entregado es menor al monto asignado a efectivo.", "Efectivo Insuficiente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    request.Metodos.Add(new DetallePagoRequestDto
                    {
                        Metodo = MetodoPago.Efectivo,
                        Monto = _numMixtoEfectivo.Value,
                        MontoEntregado = _numMixtoEfectivoEntregado.Value
                    });
                }

                if (_numMixtoTransferencia.Value > 0)
                {
                    request.Metodos.Add(new DetallePagoRequestDto
                    {
                        Metodo = MetodoPago.Transferencia,
                        Monto = _numMixtoTransferencia.Value,
                        BancoDestino = _cmbMixtoBanco.SelectedItem?.ToString(),
                        NumeroReferencia = _txtMixtoReferencia.Text.Trim(),
                        EsVerificado = _chkMixtoTransferenciaVerificada.Checked
                    });
                }

                if (_numMixtoTarjeta.Value > 0)
                {
                    request.Metodos.Add(new DetallePagoRequestDto
                    {
                        Metodo = MetodoPago.Tarjeta,
                        Monto = _numMixtoTarjeta.Value,
                        TipoTarjeta = _rbMixtoDebito.Checked ? SubtipoTarjeta.Debito : SubtipoTarjeta.Credito,
                        NumeroAutorizacionPos = _txtMixtoAutorizacion.Text.Trim()
                    });
                }
                break;
        }

        var resultado = await _pagoService.ProcesarPagoAsync(request);
        if (!resultado.Exitoso)
        {
            MessageBox.Show(string.Join("\n", resultado.Errores), "Error al Procesar Pago", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        ResultadoPago = resultado;
        DialogResult = DialogResult.OK;
        Close();
    }
}
