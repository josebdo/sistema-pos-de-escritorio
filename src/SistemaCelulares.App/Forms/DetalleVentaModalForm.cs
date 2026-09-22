using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Constants;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class DetalleVentaModalForm : Form
{
    private readonly int _ventaId;
    private readonly IVentaService _ventaService;
    private readonly SesionUsuario _sesion;

    private Venta? _venta;
    private Label _lblFactura = null!;
    private Label _lblNcf = null!;
    private Label _lblFecha = null!;
    private Label _lblCliente = null!;
    private Label _lblCajero = null!;
    private Label _lblEstado = null!;
    private Label _lblTurno = null!;
    private DataGridView _gridDetalles = null!;
    private Label _lblSubtotal = null!;
    private Label _lblItbis = null!;
    private Label _lblDescuento = null!;
    private Label _lblTotal = null!;
    private Label _lblMetodoPago = null!;
    private Button _btnReimprimir = null!;
    private Button _btnAnular = null!;
    private Button _btnCerrar = null!;

    public DetalleVentaModalForm(int ventaId, IVentaService ventaService, SesionUsuario sesion)
    {
        _ventaId = ventaId;
        _ventaService = ventaService;
        _sesion = sesion;

        InitializeCustomComponents();
        Load += async (s, e) => await CargarVentaAsync();
    }

    private void InitializeCustomComponents()
    {
        Text = "Detalle Completo de Factura de Venta — Veyra POS";
        Size = new Size(880, 680);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.AppBg;
        Font = UITheme.BodyFont;

        var panelPrincipal = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };
        Controls.Add(panelPrincipal);

        // Header Superior
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 65 };
        var lblTitulo = new Label
        {
            Text = "Detalle de Venta y Comprobante Fiscal",
            Font = UITheme.TitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(0, 4),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblTitulo);

        var lblSub = new Label
        {
            Text = "Consulte el desglose de productos vendidos, IMEIs/series asociadas y detalles del pago",
            Font = UITheme.SmallFont,
            ForeColor = UITheme.TextMuted,
            Location = new Point(0, 34),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblSub);

        // Card Datos de Cabecera
        var cardCabecera = new Panel
        {
            Dock = DockStyle.Top,
            Height = 115,
            BackColor = Color.White,
            Padding = new Padding(15),
            Margin = new Padding(0, 0, 0, 10)
        };
        cardCabecera.Paint += (s, e) => e.Graphics.DrawRectangle(new Pen(UITheme.Border), 0, 0, cardCabecera.Width - 1, cardCabecera.Height - 1);

        _lblFactura = new Label { Text = "Factura: Cargando...", Font = UITheme.SectionFont, ForeColor = UITheme.Primary, Location = new Point(15, 12), AutoSize = true };
        cardCabecera.Controls.Add(_lblFactura);

        _lblNcf = new Label { Text = "NCF: -", Font = UITheme.BodyFont, Location = new Point(15, 38), AutoSize = true };
        cardCabecera.Controls.Add(_lblNcf);

        _lblFecha = new Label { Text = "Fecha: -", Font = UITheme.BodyFont, Location = new Point(15, 62), AutoSize = true };
        cardCabecera.Controls.Add(_lblFecha);

        _lblTurno = new Label { Text = "Turno: -", Font = UITheme.BodyFont, Location = new Point(15, 86), AutoSize = true };
        cardCabecera.Controls.Add(_lblTurno);

        _lblCliente = new Label { Text = "Cliente: -", Font = UITheme.SectionFont, Location = new Point(380, 12), AutoSize = true };
        cardCabecera.Controls.Add(_lblCliente);

        _lblCajero = new Label { Text = "Cajero: -", Font = UITheme.BodyFont, Location = new Point(380, 38), AutoSize = true };
        cardCabecera.Controls.Add(_lblCajero);

        _lblEstado = new Label { Text = "Estado: -", Font = UITheme.SectionFont, Location = new Point(380, 62), AutoSize = true };
        cardCabecera.Controls.Add(_lblEstado);

        _lblMetodoPago = new Label { Text = "Pago: -", Font = UITheme.BodyFont, Location = new Point(380, 86), AutoSize = true };
        cardCabecera.Controls.Add(_lblMetodoPago);

        // DataGridView de Productos
        var panelGrid = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(1) };
        _gridDetalles = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 32 }
        };
        UITheme.EstilizarDataGridView(_gridDetalles);
        ConfigurarColumnas();
        panelGrid.Controls.Add(_gridDetalles);

        // Footer con Totales y Botones
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 115,
            BackColor = Color.White,
            Padding = new Padding(15, 10, 15, 10)
        };
        footer.Paint += (s, e) => e.Graphics.DrawRectangle(new Pen(UITheme.Border), 0, 0, footer.Width - 1, footer.Height - 1);

        var pnlTotales = new Panel { Location = new Point(15, 6), Size = new Size(380, 100) };
        _lblSubtotal = new Label { Text = "Subtotal: RD$0.00", Font = UITheme.BodyFont, Location = new Point(0, 3), AutoSize = true };
        _lblDescuento = new Label { Text = "Descuento: RD$0.00", Font = UITheme.BodyFont, Location = new Point(0, 24), AutoSize = true };
        _lblItbis = new Label { Text = "ITBIS (18%): RD$0.00", Font = UITheme.BodyFont, Location = new Point(0, 45), AutoSize = true };
        _lblTotal = new Label { Text = "Total Facturado: RD$0.00", Font = UITheme.TitleFont, ForeColor = UITheme.Primary, Location = new Point(0, 68), AutoSize = true };
        pnlTotales.Controls.Add(_lblSubtotal);
        pnlTotales.Controls.Add(_lblDescuento);
        pnlTotales.Controls.Add(_lblItbis);
        pnlTotales.Controls.Add(_lblTotal);
        footer.Controls.Add(pnlTotales);

        var flowBotones = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 440,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 25, 0, 0)
        };

        _btnCerrar = new Button { Text = "Cerrar", Size = new Size(95, 40), DialogResult = DialogResult.OK };
        UITheme.AplicarBotonSecundario(_btnCerrar);
        flowBotones.Controls.Add(_btnCerrar);

        _btnReimprimir = new Button { Text = "🖨️ Imprimir Ticket", Size = new Size(150, 40) };
        UITheme.AplicarBotonPrimario(_btnReimprimir);
        _btnReimprimir.Click += async (s, e) => await ReimprimirTicketAsync();
        flowBotones.Controls.Add(_btnReimprimir);

        _btnAnular = new Button { Text = "⛔ Anular Venta", Size = new Size(140, 40) };
        UITheme.AplicarBotonPeligro(_btnAnular);
        _btnAnular.Click += async (s, e) => await AnularVentaAsync();
        _btnAnular.Visible = _sesion.EsSuperAdmin || _sesion.EsAdmin || _sesion.TienePermiso(Permisos.VentasAnular);
        flowBotones.Controls.Add(_btnAnular);

        footer.Controls.Add(flowBotones);

        // Jerarquía de Acoplamiento ordenada
        panelPrincipal.Controls.Add(panelGrid);       // Fill
        panelPrincipal.Controls.Add(footer);          // Bottom
        panelPrincipal.Controls.Add(cardCabecera);    // Top 2
        panelPrincipal.Controls.Add(pnlHeader);       // Top 1
    }

    private void ConfigurarColumnas()
    {
        _gridDetalles.Columns.Clear();
        _gridDetalles.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sku", HeaderText = "SKU", FillWeight = 75 });
        _gridDetalles.Columns.Add(new DataGridViewTextBoxColumn { Name = "Producto", HeaderText = "Producto / Descripción", FillWeight = 180 });
        _gridDetalles.Columns.Add(new DataGridViewTextBoxColumn { Name = "Imei", HeaderText = "IMEI / Serie", FillWeight = 120 });
        _gridDetalles.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cantidad", HeaderText = "Cant.", FillWeight = 45 });
        _gridDetalles.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrecioUnitario", HeaderText = "Precio Unit.", FillWeight = 85 });
        _gridDetalles.Columns.Add(new DataGridViewTextBoxColumn { Name = "Itbis", HeaderText = "ITBIS", FillWeight = 70 });
        _gridDetalles.Columns.Add(new DataGridViewTextBoxColumn { Name = "Subtotal", HeaderText = "Total Línea", FillWeight = 90 });
    }

    private async Task CargarVentaAsync()
    {
        try
        {
            _venta = await _ventaService.ObtenerPorIdAsync(_ventaId);
            if (_venta == null)
            {
                MessageBox.Show("No se encontró la información de la venta especificada.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            _lblFactura.Text = $"Factura: {_venta.NumeroFactura}";
            _lblNcf.Text = $"NCF DGII: {_venta.Ncf ?? "(Comprobante Simple)"}";
            _lblFecha.Text = $"Fecha: {_venta.FechaVenta.ToLocalTime():dd/MM/yyyy hh:mm tt}";
            _lblTurno.Text = $"Turno: #{_venta.TurnoId} | Caja: {(_venta.Turno?.Estado == TurnoEstado.Abierto ? "Abierta" : "Cerrada")}";

            string clienteNombre = _venta.Cliente?.NombreCompleto ?? _venta.NombreClienteAnonimo ?? "Consumidor Final";
            string rnc = _venta.Cliente?.RncOCedula ?? _venta.RncCliente ?? "No especificado";
            _lblCliente.Text = $"Cliente: {clienteNombre} (RNC/Cédula: {rnc})";
            _lblCajero.Text = $"Cajero: {_venta.Usuario?.NombreCompleto ?? "Usuario #" + _venta.UsuarioId}";

            if (_venta.Estado == EstadoVenta.Anulada)
            {
                _lblEstado.Text = "Estado: 🔴 ANULADA";
                _lblEstado.ForeColor = UITheme.Danger;
                _btnAnular.Enabled = false;
            }
            else
            {
                _lblEstado.Text = "Estado: 🟢 COMPLETADA / PAGADA";
                _lblEstado.ForeColor = UITheme.Success;
                _btnAnular.Enabled = true;
            }

            // Métodos de pago
            string pagosStr = "Efectivo";
            if (_venta.Pago?.Detalles != null && _venta.Pago.Detalles.Count > 0)
            {
                pagosStr = string.Join(", ", _venta.Pago.Detalles.Select(d => $"{d.MetodoPago}: {AppCulture.FormatearMoneda(d.Monto)}"));
            }
            _lblMetodoPago.Text = $"Forma de Pago: {pagosStr}";

            // Grilla de detalles
            _gridDetalles.Rows.Clear();
            foreach (var d in _venta.Detalles)
            {
                _gridDetalles.Rows.Add(
                    d.Producto?.Sku ?? "-",
                    d.Producto?.Nombre ?? "Artículo",
                    d.Imei ?? "-",
                    d.Cantidad,
                    AppCulture.FormatearMoneda(d.PrecioUnitario),
                    AppCulture.FormatearMoneda(d.Itbis),
                    AppCulture.FormatearMoneda(d.Subtotal)
                );
            }

            _lblSubtotal.Text = $"Subtotal: {AppCulture.FormatearMoneda(_venta.Subtotal)}";
            _lblDescuento.Text = $"Descuento: {AppCulture.FormatearMoneda(_venta.Descuento)}";
            _lblItbis.Text = $"ITBIS (18%): {AppCulture.FormatearMoneda(_venta.Itbis)}";
            _lblTotal.Text = $"Total Facturado: {AppCulture.FormatearMoneda(_venta.Total)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar datos de la venta: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ReimprimirTicketAsync()
    {
        try
        {
            var ticket = await _ventaService.GenerarTicketVentaAsync(_ventaId);
            TicketRenderer.ImprimirTicket(ticket);
            MessageBox.Show("Ticket de venta enviado a la impresora.", "Impresión", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al generar ticket: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task AnularVentaAsync()
    {
        if (_venta == null || _venta.Estado == EstadoVenta.Anulada) return;

        var confirm = MessageBox.Show(
            $"¿Está seguro de anular la venta {_venta.NumeroFactura}?\nSe revertirá el stock al inventario y se cancelará el pago registrado.",
            "Confirmar Anulación",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm == DialogResult.Yes)
        {
            bool ok = await _ventaService.AnularVentaAsync(_ventaId, "Anulación manual desde detalle de venta", _sesion.UsuarioId);
            if (ok)
            {
                MessageBox.Show("Venta anulada exitosamente y stock restaurado.", "Anulación Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await CargarVentaAsync();
            }
        }
    }
}
