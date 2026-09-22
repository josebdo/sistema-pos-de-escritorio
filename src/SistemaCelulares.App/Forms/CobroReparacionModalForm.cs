using System.Globalization;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class CobroReparacionModalForm : Form
{
    private readonly OrdenReparacion _orden;
    private readonly ITurnoService _turnoService;
    private readonly IPagoService _pagoService;
    private readonly IReparacionService _reparacionService;
    private readonly SesionUsuario _sesion;

    public bool CobradoExitosamente { get; private set; }

    private NumericUpDown _numMontoFinal = null!;
    private ComboBox _cbMetodoPago = null!;
    private NumericUpDown _numEfectivoEntregado = null!;
    private Label _lblDevuelta = null!;
    private Panel _pnlTransferencia = null!;
    private TextBox _txtBanco = null!;
    private TextBox _txtReferencia = null!;
    private CheckBox _chkTransferenciaVerificada = null!;
    private Panel _pnlTarjeta = null!;
    private ComboBox _cbSubtipoTarjeta = null!;
    private TextBox _txtAutorizacionPos = null!;
    private CheckBox _chkVoucherFirmado = null!;
    private Label _lblError = null!;
    private Button _btnConfirmarCobro = null!;

    public CobroReparacionModalForm(
        OrdenReparacion orden,
        ITurnoService turnoService,
        IPagoService pagoService,
        IReparacionService reparacionService,
        SesionUsuario sesion)
    {
        _orden = orden;
        _turnoService = turnoService;
        _pagoService = pagoService;
        _reparacionService = reparacionService;
        _sesion = sesion;

        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = $"Cobro y Entrega de Equipo - {_orden.NumeroOrden}";
        Size = new Size(540, 620);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        BackColor = Color.FromArgb(248, 249, 250);

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Color.FromArgb(0, 105, 92),
            Padding = new Padding(20, 12, 20, 10)
        };

        var lblTitulo = new Label
        {
            Text = $"COBRAR Y ENTREGAR: {_orden.NumeroOrden}",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = $"Cliente: {_orden.Cliente?.NombreCompleto} | Equipo: {_orden.Marca} {_orden.Modelo}",
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(178, 223, 219),
            Location = new Point(20, 38),
            AutoSize = true
        };

        pnlHeader.Controls.Add(lblTitulo);
        pnlHeader.Controls.Add(lblSub);
        Controls.Add(pnlHeader);

        int top = 85;
        int left = 30;
        int width = 465;

        // Monto a Cobrar
        var lblMonto = new Label { Text = "Monto Total de Reparación (RD$) *", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
        _numMontoFinal = new NumericUpDown
        {
            Location = new Point(left, top + 25),
            Width = width,
            Height = 32,
            DecimalPlaces = 2,
            ThousandsSeparator = true,
            Maximum = 1000000m,
            Value = _orden.PrecioFinal > 0 ? _orden.PrecioFinal : _orden.PrecioEstimado,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 105, 92)
        };
        _numMontoFinal.ValueChanged += (s, e) => CalcularDevuelta();

        Controls.Add(lblMonto);
        Controls.Add(_numMontoFinal);
        top += 68;

        // Método de Pago
        var lblMetodo = new Label { Text = "Método de Pago *", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
        _cbMetodoPago = new ComboBox
        {
            Location = new Point(left, top + 22),
            Width = width,
            Height = 28,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cbMetodoPago.Items.AddRange(new object[] { "Efectivo (RD$)", "Transferencia Bancaria", "Tarjeta (Datáfono / POS)" });
        _cbMetodoPago.SelectedIndex = 0;
        _cbMetodoPago.SelectedIndexChanged += (s, e) => CambiarMetodoPago();

        Controls.Add(lblMetodo);
        Controls.Add(_cbMetodoPago);
        top += 60;

        // Panel Efectivo
        var pnlEfectivo = new Panel { Location = new Point(left, top), Size = new Size(width, 70), BackColor = Color.FromArgb(232, 245, 233), Padding = new Padding(10) };
        var lblEf = new Label { Text = "Efectivo Entregado (RD$):", Location = new Point(10, 10), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        _numEfectivoEntregado = new NumericUpDown { Location = new Point(10, 32), Width = 180, Height = 28, DecimalPlaces = 2, ThousandsSeparator = true, Maximum = 1000000m, Value = _numMontoFinal.Value, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
        _numEfectivoEntregado.ValueChanged += (s, e) => CalcularDevuelta();

        _lblDevuelta = new Label { Text = "Devuelta / Cambio: RD$0.00", Location = new Point(210, 34), AutoSize = true, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), ForeColor = Color.FromArgb(46, 125, 50) };

        pnlEfectivo.Controls.Add(lblEf);
        pnlEfectivo.Controls.Add(_numEfectivoEntregado);
        pnlEfectivo.Controls.Add(_lblDevuelta);
        Controls.Add(pnlEfectivo);

        // Panel Transferencia
        _pnlTransferencia = new Panel { Location = new Point(left, top), Size = new Size(width, 100), BackColor = Color.FromArgb(227, 242, 253), Padding = new Padding(10), Visible = false };
        var lblBco = new Label { Text = "Banco:", Location = new Point(10, 10), AutoSize = true };
        _txtBanco = new TextBox { Location = new Point(10, 30), Width = 210, Height = 26, PlaceholderText = "Banreservas, BHD, Popular..." };
        var lblRef = new Label { Text = "N° Referencia:", Location = new Point(230, 10), AutoSize = true };
        _txtReferencia = new TextBox { Location = new Point(230, 30), Width = 210, Height = 26, PlaceholderText = "Últimos dígitos o confirmación" };
        _chkTransferenciaVerificada = new CheckBox { Text = "✅ Transferencia verificada en cuenta bancaria", Location = new Point(10, 68), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(13, 71, 161) };

        _pnlTransferencia.Controls.Add(lblBco);
        _pnlTransferencia.Controls.Add(_txtBanco);
        _pnlTransferencia.Controls.Add(lblRef);
        _pnlTransferencia.Controls.Add(_txtReferencia);
        _pnlTransferencia.Controls.Add(_chkTransferenciaVerificada);
        Controls.Add(_pnlTransferencia);

        // Panel Tarjeta
        _pnlTarjeta = new Panel { Location = new Point(left, top), Size = new Size(width, 100), BackColor = Color.FromArgb(254, 237, 222), Padding = new Padding(10), Visible = false };
        var lblSubT = new Label { Text = "Tipo de Tarjeta:", Location = new Point(10, 10), AutoSize = true };
        _cbSubtipoTarjeta = new ComboBox { Location = new Point(10, 30), Width = 190, Height = 26, DropDownStyle = ComboBoxStyle.DropDownList };
        _cbSubtipoTarjeta.Items.AddRange(new object[] { "Tarjeta de Débito", "Tarjeta de Crédito" });
        _cbSubtipoTarjeta.SelectedIndex = 0;

        var lblAut = new Label { Text = "N° Autorización / Aprobación:", Location = new Point(210, 10), AutoSize = true };
        _txtAutorizacionPos = new TextBox { Location = new Point(210, 30), Width = 230, Height = 26, PlaceholderText = "Aprobación del datáfono" };
        _chkVoucherFirmado = new CheckBox { Text = "✅ Voucher firmado / aprobado por datáfono", Location = new Point(10, 68), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(230, 81, 0) };

        _pnlTarjeta.Controls.Add(lblSubT);
        _pnlTarjeta.Controls.Add(_cbSubtipoTarjeta);
        _pnlTarjeta.Controls.Add(lblAut);
        _pnlTarjeta.Controls.Add(_txtAutorizacionPos);
        _pnlTarjeta.Controls.Add(_chkVoucherFirmado);
        Controls.Add(_pnlTarjeta);

        top += 115;

        // Label de error
        _lblError = new Label { Location = new Point(left, top), Size = new Size(width, 22), ForeColor = Color.Red, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Visible = false };
        Controls.Add(_lblError);
        top += 28;

        // Botones
        _btnConfirmarCobro = new Button
        {
            Text = "💳 Cobrar y Entregar Equipo",
            Location = new Point(left + 150, top),
            Size = new Size(210, 42),
            BackColor = Color.FromArgb(0, 105, 92),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnConfirmarCobro.FlatAppearance.BorderSize = 0;
        _btnConfirmarCobro.Click += BtnConfirmarCobro_Click;

        var btnCancelar = new Button
        {
            Text = "Cancelar",
            Location = new Point(left + 370, top),
            Size = new Size(95, 42),
            BackColor = Color.FromArgb(224, 224, 224),
            FlatStyle = FlatStyle.Flat,
            DialogResult = DialogResult.Cancel,
            Cursor = Cursors.Hand
        };
        btnCancelar.FlatAppearance.BorderSize = 0;

        Controls.Add(_btnConfirmarCobro);
        Controls.Add(btnCancelar);

        AcceptButton = _btnConfirmarCobro;
        CancelButton = btnCancelar;
    }

    private void CambiarMetodoPago()
    {
        _pnlTransferencia.Visible = _cbMetodoPago.SelectedIndex == 1;
        _pnlTarjeta.Visible = _cbMetodoPago.SelectedIndex == 2;
    }

    private void CalcularDevuelta()
    {
        var total = _numMontoFinal.Value;
        var entregado = _numEfectivoEntregado.Value;
        var devuelta = entregado - total;

        if (devuelta >= 0)
        {
            _lblDevuelta.Text = $"Devuelta / Cambio: RD${devuelta:N2}";
            _lblDevuelta.ForeColor = Color.FromArgb(46, 125, 50);
        }
        else
        {
            _lblDevuelta.Text = $"Falta: RD${Math.Abs(devuelta):N2}";
            _lblDevuelta.ForeColor = Color.Red;
        }
    }

    private async void BtnConfirmarCobro_Click(object? sender, EventArgs e)
    {
        _lblError.Visible = false;

        var totalCobro = _numMontoFinal.Value;
        if (totalCobro <= 0)
        {
            _lblError.Text = "El monto a cobrar debe ser mayor a cero.";
            _lblError.Visible = true;
            return;
        }

        // Obtener turno abierto
        var turno = await _turnoService.ObtenerTurnoAbiertoAsync(_sesion.UsuarioId);
        if (turno == null)
        {
            _lblError.Text = "No tiene un turno de caja abierto para registrar el cobro.";
            _lblError.Visible = true;
            return;
        }

        var pagoRequest = new RegistrarPagoDto
        {
            UsuarioId = _sesion.UsuarioId,
            TurnoId = turno.Id,
            MontoTotal = totalCobro
        };

        if (_cbMetodoPago.SelectedIndex == 0) // Efectivo
        {
            if (_numEfectivoEntregado.Value < totalCobro)
            {
                _lblError.Text = "El efectivo entregado es insuficiente.";
                _lblError.Visible = true;
                return;
            }

            pagoRequest.Metodos.Add(new DetallePagoRequestDto
            {
                Metodo = MetodoPago.Efectivo,
                Monto = totalCobro,
                MontoEntregado = _numEfectivoEntregado.Value
            });
        }
        else if (_cbMetodoPago.SelectedIndex == 1) // Transferencia
        {
            if (!_chkTransferenciaVerificada.Checked)
            {
                _lblError.Text = "Debe marcar que la transferencia fue verificada en banco.";
                _lblError.Visible = true;
                return;
            }

            pagoRequest.Metodos.Add(new DetallePagoRequestDto
            {
                Metodo = MetodoPago.Transferencia,
                Monto = totalCobro,
                BancoDestino = _txtBanco.Text.Trim(),
                NumeroReferencia = _txtReferencia.Text.Trim(),
                EsVerificado = true
            });
        }
        else if (_cbMetodoPago.SelectedIndex == 2) // Tarjeta
        {
            if (!_chkVoucherFirmado.Checked)
            {
                _lblError.Text = "Debe confirmar la aprobación del datáfono / voucher.";
                _lblError.Visible = true;
                return;
            }

            pagoRequest.Metodos.Add(new DetallePagoRequestDto
            {
                Metodo = MetodoPago.Tarjeta,
                Monto = totalCobro,
                TipoTarjeta = _cbSubtipoTarjeta.SelectedIndex == 0 ? SubtipoTarjeta.Debito : SubtipoTarjeta.Credito,
                NumeroAutorizacionPos = _txtAutorizacionPos.Text.Trim()
            });
        }

        _btnConfirmarCobro.Enabled = false;
        try
        {
            var resPago = await _pagoService.ProcesarPagoAsync(pagoRequest);
            if (!resPago.Exitoso || !resPago.PagoId.HasValue)
            {
                _lblError.Text = string.Join("; ", resPago.Errores);
                _lblError.Visible = true;
                return;
            }

            var pagoEntidad = await _pagoService.ObtenerPorIdAsync(resPago.PagoId.Value);
            if (pagoEntidad == null)
            {
                throw new InvalidOperationException("No se encontró el registro de pago generado.");
            }

            await _reparacionService.CobrarYEntregarAsync(_orden.Id, _sesion.UsuarioId, pagoEntidad);
            CobradoExitosamente = true;

            var pregImprimir = MessageBox.Show(
                $"Orden {_orden.NumeroOrden} cobrada y entregada exitosamente.\nMonto: RD${totalCobro:N2} | Vuelto: RD${resPago.MontoVuelto:N2}\n\n¿Desea imprimir la factura / ticket de entrega de la reparación?",
                "Reparación Entregada — Imprimir Comprobante",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (pregImprimir == DialogResult.Yes)
            {
                var ticketRep = new TicketReparacionDto
                {
                    NombreEmpresa = "Veyra POS — Tienda y Taller de Celulares",
                    RncEmpresa = "131-12345-6",
                    DireccionEmpresa = "Av. Principal #123, Santo Domingo",
                    TelefonoEmpresa = "(809) 555-0199",
                    NumeroOrden = _orden.NumeroOrden,
                    Fecha = DateTime.Now,
                    TecnicoEntrega = _sesion.NombreCompleto,
                    ClienteNombre = _orden.Cliente?.NombreCompleto ?? "Cliente General",
                    ClienteTelefono = _orden.Cliente?.Telefono ?? "—",
                    ClienteRnc = _orden.Cliente?.RncOCedula,
                    EquipoMarcaModelo = $"{_orden.Marca} {_orden.Modelo}",
                    ImeiOSerie = _orden.ImeiOSerie ?? "—",
                    DescripcionProblema = _orden.DescripcionProblema,
                    DiagnosticoSolucion = _orden.NotasDiagnostico ?? "Reparación y servicio técnico completado satisfactoriamente",
                    MontoTotal = totalCobro,
                    MetodosPagoTexto = _cbMetodoPago.SelectedItem?.ToString() ?? "Efectivo",
                    MontoEntregado = _cbMetodoPago.SelectedIndex == 0 ? _numEfectivoEntregado.Value : totalCobro,
                    MontoVuelto = resPago.MontoVuelto,
                    EstadoOrden = "Entregada y Cobrada"
                };

                Common.TicketRenderer.ImprimirTicketReparacion(ticketRep);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _lblError.Text = ex.Message;
            _lblError.Visible = true;
        }
        finally
        {
            _btnConfirmarCobro.Enabled = true;
        }
    }
}
