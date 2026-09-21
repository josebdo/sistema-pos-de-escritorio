using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class PuntoVentaForm : Form
{
    private readonly IVentaService _ventaService;
    private readonly IProductoService _productoService;
    private readonly IPagoService _pagoService;
    private readonly ITurnoService _turnoService;
    private readonly SesionUsuario _sesion;
    private readonly IServiceProvider _serviceProvider;

    private Turno? _turnoActivo;
    private readonly List<ItemCarritoVentaDto> _carrito = new();

    // Controles
    private Label _lblTurnoInfo = null!;
    private ComboBox _cmbTipoComprobante = null!;
    private TextBox _txtClienteNombre = null!;
    private TextBox _txtClienteRnc = null!;
    private TextBox _txtBusquedaProducto = null!;
    private DataGridView _gridCarrito = null!;

    private Label _lblSubtotal = null!;
    private Label _lblItbis = null!;
    private NumericUpDown _numDescuento = null!;
    private Label _lblTotal = null!;
    private Button _btnCobrar = null!;
    private Button _btnLimpiar = null!;
    private Button _btnHistorial = null!;

    public PuntoVentaForm(
        IVentaService ventaService,
        IProductoService productoService,
        IPagoService pagoService,
        ITurnoService turnoService,
        SesionUsuario sesion,
        IServiceProvider serviceProvider)
    {
        _ventaService = ventaService;
        _productoService = productoService;
        _pagoService = pagoService;
        _turnoService = turnoService;
        _sesion = sesion;
        _serviceProvider = serviceProvider;

        InitializeComponents();
        CargarTurnoActivo();
    }

    private void InitializeComponents()
    {
        Text = "Punto de Venta (POS) y Facturación con NCF DGII";
        Size = new Size(1150, 720);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(248, 249, 250);
        KeyPreview = true;

        KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.F12) _btnCobrar.PerformClick();
            if (e.KeyCode == Keys.F4) _btnLimpiar.PerformClick();
            if (e.KeyCode == Keys.F6) _btnHistorial.PerformClick();
        };

        // Header Superior
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Color.FromArgb(24, 43, 73),
            Padding = new Padding(20, 10, 20, 10)
        };

        var lblTitulo = new Label
        {
            Text = "🛒 PUNTO DE VENTA Y FACTURACIÓN COMERCIAL",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Dock = DockStyle.Left,
            AutoSize = true
        };

        _lblTurnoInfo = new Label
        {
            Text = "Cargando turno...",
            ForeColor = Color.FromArgb(46, 204, 113),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Dock = DockStyle.Right,
            AutoSize = true
        };

        pnlHeader.Controls.Add(lblTitulo);
        pnlHeader.Controls.Add(_lblTurnoInfo);

        // Panel de Opciones Fiscales y Cliente (Top)
        var pnlFiscal = new Panel
        {
            Location = new Point(20, 75),
            Size = new Size(740, 75),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(10)
        };

        var lblTipoComp = new Label { Text = "Comprobante Fiscal:", Location = new Point(10, 10), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        _cmbTipoComprobante = new ComboBox
        {
            Location = new Point(10, 32),
            Size = new Size(230, 25),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 8.5f)
        };
        _cmbTipoComprobante.Items.AddRange(new object[]
        {
            "Factura de Consumo (B02)",
            "Factura de Crédito Fiscal (B01)",
            "Regímenes Especiales (B14)",
            "Gubernamental (B15)",
            "Ticket Sin NCF (Venta Informal)"
        });
        _cmbTipoComprobante.SelectedIndex = 0;
        _cmbTipoComprobante.SelectedIndexChanged += (s, e) =>
        {
            bool esB01 = _cmbTipoComprobante.SelectedIndex == 1;
            _txtClienteRnc.BackColor = esB01 ? Color.FromArgb(254, 249, 231) : Color.White;
        };

        var lblCliente = new Label { Text = "Cliente / Razón Social:", Location = new Point(255, 10), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtClienteNombre = new TextBox { Location = new Point(255, 32), Size = new Size(240, 25), Font = new Font("Segoe UI", 8.5f), PlaceholderText = "Consumidor Final" };

        var lblRnc = new Label { Text = "RNC / Cédula (obligatorio para B01):", Location = new Point(510, 10), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        _txtClienteRnc = new TextBox { Location = new Point(510, 32), Size = new Size(210, 25), Font = new Font("Segoe UI", 8.5f), PlaceholderText = "Ej: 131-12345-6" };

        pnlFiscal.Controls.Add(lblTipoComp);
        pnlFiscal.Controls.Add(_cmbTipoComprobante);
        pnlFiscal.Controls.Add(lblCliente);
        pnlFiscal.Controls.Add(_txtClienteNombre);
        pnlFiscal.Controls.Add(lblRnc);
        pnlFiscal.Controls.Add(_txtClienteRnc);

        // Barra de Búsqueda y Escáner (Center-Top)
        var pnlBusqueda = new Panel
        {
            Location = new Point(20, 160),
            Size = new Size(740, 50),
            BackColor = Color.FromArgb(234, 250, 234),
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(10)
        };

        var lblScan = new Label
        {
            Text = "🔍 Escanear Código de Barras / SKU / Nombre:",
            Location = new Point(10, 14),
            AutoSize = true,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 132, 73)
        };

        _txtBusquedaProducto = new TextBox
        {
            Location = new Point(310, 11),
            Size = new Size(310, 26),
            Font = new Font("Segoe UI", 10)
        };
        _txtBusquedaProducto.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await BuscarYAgregarProductoAsync(_txtBusquedaProducto.Text.Trim());
            }
        };

        var btnBuscar = new Button
        {
            Text = "Agregar",
            Location = new Point(630, 9),
            Size = new Size(90, 29),
            BackColor = Color.FromArgb(39, 174, 96),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnBuscar.FlatAppearance.BorderSize = 0;
        btnBuscar.Click += async (s, e) => await BuscarYAgregarProductoAsync(_txtBusquedaProducto.Text.Trim());

        pnlBusqueda.Controls.Add(lblScan);
        pnlBusqueda.Controls.Add(_txtBusquedaProducto);
        pnlBusqueda.Controls.Add(btnBuscar);

        // Grid Carrito de Venta
        _gridCarrito = new DataGridView
        {
            Location = new Point(20, 220),
            Size = new Size(740, 440),
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            Font = new Font("Segoe UI", 9)
        };

        ConfigurarColumnasGrid();
        _gridCarrito.CellValueChanged += (s, e) => RecalcularTotales();
        _gridCarrito.CellContentClick += (s, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == _gridCarrito.Columns["Accion"].Index)
            {
                _carrito.RemoveAt(e.RowIndex);
                RefrescarGridCarrito();
            }
        };

        // Panel Lateral Derecho de Totales y Liquidación
        var pnlTotales = new Panel
        {
            Location = new Point(775, 75),
            Size = new Size(340, 585),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(20)
        };

        var lblResumenTitulo = new Label
        {
            Text = "RESUMEN DE VENTA",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(52, 73, 94),
            Location = new Point(20, 15),
            AutoSize = true
        };

        var pnlTotalGrande = new Panel
        {
            Location = new Point(15, 45),
            Size = new Size(305, 110),
            BackColor = Color.FromArgb(24, 43, 73),
            Padding = new Padding(10)
        };

        var lblTotalTxt = new Label
        {
            Text = "TOTAL A PAGAR (RD$)",
            ForeColor = Color.FromArgb(173, 181, 189),
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Dock = DockStyle.Top
        };

        _lblTotal = new Label
        {
            Text = "RD$ 0.00",
            ForeColor = Color.FromArgb(46, 204, 113),
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            Dock = DockStyle.Fill
        };

        pnlTotalGrande.Controls.Add(_lblTotal);
        pnlTotalGrande.Controls.Add(lblTotalTxt);

        // Desglose
        int yTotales = 170;
        var lblSubtxt = new Label { Text = "Subtotal Neto:", Location = new Point(20, yTotales), AutoSize = true, Font = new Font("Segoe UI", 9.5f) };
        _lblSubtotal = new Label { Text = "RD$ 0.00", Location = new Point(180, yTotales), Size = new Size(140, 20), TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        yTotales += 30;

        var lblItbistxt = new Label { Text = "ITBIS (18%):", Location = new Point(20, yTotales), AutoSize = true, Font = new Font("Segoe UI", 9.5f) };
        _lblItbis = new Label { Text = "RD$ 0.00", Location = new Point(180, yTotales), Size = new Size(140, 20), TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        yTotales += 30;

        var lblDesctxt = new Label { Text = "Descuento (RD$):", Location = new Point(20, yTotales + 3), AutoSize = true, Font = new Font("Segoe UI", 9.5f) };
        _numDescuento = new NumericUpDown { Location = new Point(180, yTotales), Size = new Size(140, 25), Maximum = 100000, DecimalPlaces = 2 };
        _numDescuento.ValueChanged += (s, e) => RecalcularTotales();
        yTotales += 45;

        // Botón Cobrar (F12)
        _btnCobrar = new Button
        {
            Text = "💳 COBRAR Y FACTURAR\n(F12)",
            Location = new Point(15, yTotales),
            Size = new Size(305, 65),
            BackColor = Color.FromArgb(39, 174, 96),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnCobrar.FlatAppearance.BorderSize = 0;
        _btnCobrar.Click += async (s, e) => await ProcesarCobroVentaAsync();
        yTotales += 75;

        _btnLimpiar = new Button
        {
            Text = "🗑️ Limpiar Carrito (F4)",
            Location = new Point(15, yTotales),
            Size = new Size(305, 38),
            BackColor = Color.FromArgb(231, 76, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnLimpiar.FlatAppearance.BorderSize = 0;
        _btnLimpiar.Click += (s, e) =>
        {
            _carrito.Clear();
            RefrescarGridCarrito();
        };
        yTotales += 46;

        _btnHistorial = new Button
        {
            Text = "📜 Historial de Ventas (F6)",
            Location = new Point(15, yTotales),
            Size = new Size(305, 38),
            BackColor = Color.FromArgb(52, 152, 219),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnHistorial.FlatAppearance.BorderSize = 0;
        _btnHistorial.Click += (s, e) =>
        {
            var f = new HistorialVentasForm(_ventaService, _sesion);
            f.ShowDialog(this);
        };

        pnlTotales.Controls.Add(lblResumenTitulo);
        pnlTotales.Controls.Add(pnlTotalGrande);
        pnlTotales.Controls.Add(lblSubtxt);
        pnlTotales.Controls.Add(_lblSubtotal);
        pnlTotales.Controls.Add(lblItbistxt);
        pnlTotales.Controls.Add(_lblItbis);
        pnlTotales.Controls.Add(lblDesctxt);
        pnlTotales.Controls.Add(_numDescuento);
        pnlTotales.Controls.Add(_btnCobrar);
        pnlTotales.Controls.Add(_btnLimpiar);
        pnlTotales.Controls.Add(_btnHistorial);

        Controls.Add(pnlHeader);
        Controls.Add(pnlFiscal);
        Controls.Add(pnlBusqueda);
        Controls.Add(_gridCarrito);
        Controls.Add(pnlTotales);
    }

    private void ConfigurarColumnasGrid()
    {
        _gridCarrito.Columns.Clear();
        _gridCarrito.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sku", HeaderText = "SKU", ReadOnly = true, FillWeight = 60 });
        _gridCarrito.Columns.Add(new DataGridViewTextBoxColumn { Name = "Producto", HeaderText = "Producto", ReadOnly = true, FillWeight = 160 });
        _gridCarrito.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cantidad", HeaderText = "Cant.", ReadOnly = false, FillWeight = 45 });
        _gridCarrito.Columns.Add(new DataGridViewTextBoxColumn { Name = "Precio", HeaderText = "Precio (RD$)", ReadOnly = true, FillWeight = 70 });
        _gridCarrito.Columns.Add(new DataGridViewTextBoxColumn { Name = "Itbis", HeaderText = "ITBIS 18%", ReadOnly = true, FillWeight = 65 });
        _gridCarrito.Columns.Add(new DataGridViewTextBoxColumn { Name = "Total", HeaderText = "Total (RD$)", ReadOnly = true, FillWeight = 75 });
        _gridCarrito.Columns.Add(new DataGridViewButtonColumn { Name = "Accion", HeaderText = "Quitar", Text = "❌", UseColumnTextForButtonValue = true, FillWeight = 40 });
    }

    private async void CargarTurnoActivo()
    {
        _turnoActivo = await _turnoService.ObtenerTurnoAbiertoAsync(_sesion.UsuarioId);
        if (_turnoActivo == null)
        {
            _lblTurnoInfo.Text = "⚠ CAJA CERRADA (Abra un turno antes de vender)";
            _lblTurnoInfo.ForeColor = Color.FromArgb(231, 76, 60);
            _btnCobrar.Enabled = false;
        }
        else
        {
            _lblTurnoInfo.Text = $"✔ TURNO ACTIVO #{_turnoActivo.Id} | Cajero: {_sesion.NombreCompleto} (Caja {_turnoActivo.CajaId})";
            _lblTurnoInfo.ForeColor = Color.FromArgb(46, 204, 113);
            _btnCobrar.Enabled = true;
        }
    }

    private async Task BuscarYAgregarProductoAsync(string criterio)
    {
        if (string.IsNullOrWhiteSpace(criterio)) return;

        var prod = await _productoService.BuscarPorCodigoBarrasOSkuAsync(criterio);
        if (prod == null)
        {
            var lista = await _productoService.ObtenerProductosAsync(soloActivos: true, busqueda: criterio);
            prod = lista.FirstOrDefault();
        }

        if (prod == null)
        {
            MessageBox.Show($"No se encontró ningún producto con el código o nombre '{criterio}'.", "Producto no encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtBusquedaProducto.SelectAll();
            return;
        }

        if (prod.StockActual <= 0)
        {
            MessageBox.Show($"El producto '{prod.Nombre}' está AGOTADO en inventario.", "Stock Agotado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtBusquedaProducto.Clear();
            return;
        }

        var itemExistente = _carrito.FirstOrDefault(x => x.ProductoId == prod.Id);
        if (itemExistente != null)
        {
            if (itemExistente.Cantidad + 1 > prod.StockActual)
            {
                MessageBox.Show($"No hay suficiente stock. Stock disponible: {prod.StockActual}.", "Límite de Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            itemExistente.Cantidad++;
        }
        else
        {
            _carrito.Add(new ItemCarritoVentaDto
            {
                ProductoId = prod.Id,
                Sku = prod.Sku,
                CodigoBarras = prod.CodigoBarras,
                NombreProducto = prod.Nombre,
                Cantidad = 1,
                PrecioUnitario = prod.PrecioVenta,
                CostoUnitario = prod.PrecioCosto,
                StockDisponible = prod.StockActual,
                AplicaItbis = true
            });
        }

        _txtBusquedaProducto.Clear();
        RefrescarGridCarrito();
    }

    private void RefrescarGridCarrito()
    {
        _gridCarrito.Rows.Clear();
        foreach (var it in _carrito)
        {
            _gridCarrito.Rows.Add(
                it.Sku,
                it.NombreProducto,
                it.Cantidad,
                $"RD$ {it.PrecioUnitario:N2}",
                $"RD$ {it.Itbis:N2}",
                $"RD$ {it.TotalBruto:N2}"
            );
        }

        RecalcularTotales();
    }

    private void RecalcularTotales()
    {
        for (int i = 0; i < _gridCarrito.Rows.Count; i++)
        {
            if (int.TryParse(_gridCarrito.Rows[i].Cells["Cantidad"].Value?.ToString(), out int cant))
            {
                if (cant <= 0) cant = 1;
                if (cant > _carrito[i].StockDisponible)
                {
                    cant = _carrito[i].StockDisponible;
                    _gridCarrito.Rows[i].Cells["Cantidad"].Value = cant;
                }
                _carrito[i].Cantidad = cant;
                _gridCarrito.Rows[i].Cells["Itbis"].Value = $"RD$ {_carrito[i].Itbis:N2}";
                _gridCarrito.Rows[i].Cells["Total"].Value = $"RD$ {_carrito[i].TotalBruto:N2}";
            }
        }

        decimal subtotal = _carrito.Sum(x => x.SubtotalSinItbis);
        decimal itbis = _carrito.Sum(x => x.Itbis);
        decimal descuento = _numDescuento.Value;
        decimal total = subtotal + itbis - descuento;
        if (total < 0) total = 0;

        _lblSubtotal.Text = $"RD$ {subtotal:N2}";
        _lblItbis.Text = $"RD$ {itbis:N2}";
        _lblTotal.Text = $"RD$ {total:N2}";
    }

    private async Task ProcesarCobroVentaAsync()
    {
        if (_carrito.Count == 0)
        {
            MessageBox.Show("El carrito de compras está vacío.", "Carrito Vacío", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_turnoActivo == null)
        {
            MessageBox.Show("Debe abrir un turno de caja antes de registrar ventas.", "Turno Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        TipoComprobanteFiscal tipoComp = _cmbTipoComprobante.SelectedIndex switch
        {
            0 => TipoComprobanteFiscal.Consumo_B02,
            1 => TipoComprobanteFiscal.CreditoFiscal_B01,
            2 => TipoComprobanteFiscal.RegimenEspecial_B14,
            3 => TipoComprobanteFiscal.Gubernamental_B15,
            _ => TipoComprobanteFiscal.SinComprobante
        };

        if (tipoComp == TipoComprobanteFiscal.CreditoFiscal_B01 && string.IsNullOrWhiteSpace(_txtClienteRnc.Text))
        {
            MessageBox.Show("Para emitir Factura de Crédito Fiscal (B01) es obligatorio registrar el RNC del cliente.", "RNC Obligatorio", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtClienteRnc.Focus();
            return;
        }

        decimal subtotal = _carrito.Sum(x => x.SubtotalSinItbis);
        decimal itbis = _carrito.Sum(x => x.Itbis);
        decimal total = subtotal + itbis - _numDescuento.Value;

        // Abrir pasarela de cobro
        using var cobroModal = new CobroModalForm(_pagoService, total, _sesion.UsuarioId, _turnoActivo.Id);
        if (cobroModal.ShowDialog(this) != DialogResult.OK || cobroModal.ResultadoPago == null)
        {
            return; // Cancelado
        }

        var pagoRequest = new RegistrarPagoDto
        {
            MontoTotal = total,
            UsuarioId = _sesion.UsuarioId,
            TurnoId = _turnoActivo.Id
        };

        var requestVenta = new RegistrarVentaRequestDto
        {
            UsuarioId = _sesion.UsuarioId,
            TurnoId = _turnoActivo.Id,
            TipoComprobante = tipoComp,
            NombreCliente = string.IsNullOrWhiteSpace(_txtClienteNombre.Text) ? "Consumidor Final" : _txtClienteNombre.Text.Trim(),
            RncCliente = _txtClienteRnc.Text.Trim(),
            Descuento = _numDescuento.Value,
            Items = _carrito.ToList()
        };

        var resVenta = await _ventaService.RegistrarVentaAsync(requestVenta);
        if (!resVenta.Exitoso)
        {
            MessageBox.Show(string.Join("\n", resVenta.Errores), "Error al Facturar", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Notificación de éxito
        string infoNcf = !string.IsNullOrEmpty(resVenta.Ncf) ? $"\nNCF: {resVenta.Ncf}" : "";
        string infoVuelto = resVenta.Vuelto > 0 ? $"\nDevuelta a entregar: RD$ {resVenta.Vuelto:N2}" : "";

        var r = MessageBox.Show(
            $"¡Venta registrada exitosamente!\n\nFactura: {resVenta.NumeroFactura}{infoNcf}\nTotal: RD$ {resVenta.Total:N2}{infoVuelto}\n\n¿Desea imprimir el ticket de venta térmico?",
            "Venta Completada",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);

        if (r == DialogResult.Yes && resVenta.VentaId.HasValue)
        {
            try
            {
                var ticket = await _ventaService.GenerarTicketVentaAsync(resVenta.VentaId.Value);
                TicketRenderer.ImprimirTicket(ticket);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo enviar el ticket a la impresora: {ex.Message}", "Aviso de Impresión", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Limpiar para la siguiente venta
        _carrito.Clear();
        _numDescuento.Value = 0;
        _txtClienteNombre.Clear();
        _txtClienteRnc.Clear();
        RefrescarGridCarrito();
        _txtBusquedaProducto.Focus();
    }
}
