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
    private readonly IClienteService _clienteService;
    private readonly SesionUsuario _sesion;
    private readonly IServiceProvider _serviceProvider;

    private Turno? _turnoActivo;
    private readonly List<ItemCarritoVentaDto> _carrito = new();
    private Cliente? _clienteSeleccionado;

    public Action? OnSalirPos { get; set; }

    // Controles
    private Label _lblTurnoInfo = null!;
    private ComboBox _cmbTipoComprobante = null!;
    private ComboBox _cmbClientes = null!;
    private Button _btnNuevoCliente = null!;
    private TextBox _txtClienteNombre = null!;
    private TextBox _txtClienteRnc = null!;
    private TextBox _txtBusquedaProducto = null!;
    private DataGridView _gridCarrito = null!;

    private Panel _pnlSugerencias = null!;
    private ListBox _lstSugerencias = null!;
    private List<Producto> _catalogoProductos = new();

    private Label _lblSubtotal = null!;
    private Label _lblItbis = null!;
    private NumericUpDown _numDescuento = null!;
    private Label _lblTotal = null!;
    private Button _btnCobrar = null!;
    private Button _btnLimpiar = null!;
    private Button _btnHistorial = null!;

    private class SugerenciaProductoItem
    {
        public Producto Producto { get; set; } = null!;
        public override string ToString()
        {
            string stockStr = Producto.StockActual > 0 ? $"Stock: {Producto.StockActual}" : "AGOTADO";
            return $"📦 {Producto.Nombre}  |  RD$ {Producto.PrecioVenta:N2}  |  {stockStr}  ({Producto.Sku})";
        }
    }

    public PuntoVentaForm(
        IVentaService ventaService,
        IProductoService productoService,
        IPagoService pagoService,
        ITurnoService turnoService,
        IClienteService clienteService,
        SesionUsuario sesion,
        IServiceProvider serviceProvider)
    {
        _ventaService = ventaService;
        _productoService = productoService;
        _pagoService = pagoService;
        _turnoService = turnoService;
        _clienteService = clienteService;
        _sesion = sesion;
        _serviceProvider = serviceProvider;

        InitializeComponents();
        CargarTurnoActivo();
        _ = CargarComboClientesAsync();
        _ = CargarCatalogoProductosAsync();
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

        // Contenedor Principal
        var pnlPrincipal = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(15),
            BackColor = UITheme.AppBg
        };

        // Header Superior
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = Color.FromArgb(24, 43, 73),
            Padding = new Padding(15, 8, 15, 8)
        };

        var lblTitulo = new Label
        {
            Text = "🛒 PUNTO DE VENTA (POS) Y FACTURACIÓN COMERCIAL",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Dock = DockStyle.Left,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var btnSalir = new Button
        {
            Text = "🚪 Salir del POS",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(220, 53, 69),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(130, 34),
            Cursor = Cursors.Hand,
            Dock = DockStyle.Right,
            Margin = new Padding(15, 0, 0, 0)
        };
        btnSalir.FlatAppearance.BorderSize = 0;
        btnSalir.Click += (s, e) => OnSalirPos?.Invoke();

        var btnHistorial = new Button
        {
            Text = "📜 Historial / Anular (F6)",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(44, 62, 80),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(185, 34),
            Cursor = Cursors.Hand,
            Dock = DockStyle.Right,
            Margin = new Padding(0, 0, 10, 0)
        };
        btnHistorial.FlatAppearance.BorderSize = 0;
        btnHistorial.Click += (s, e) =>
        {
            using var dlg = new HistorialVentasForm(_ventaService, _sesion);
            dlg.ShowDialog(this);
        };

        _lblTurnoInfo = new Label
        {
            Text = "Cargando turno...",
            ForeColor = Color.FromArgb(46, 204, 113),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Dock = DockStyle.Right,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 7, 15, 0)
        };

        // Dock right controls: added in reverse order in WinForms Dock
        pnlHeader.Controls.Add(lblTitulo);
        pnlHeader.Controls.Add(_lblTurnoInfo);
        pnlHeader.Controls.Add(btnHistorial);
        pnlHeader.Controls.Add(btnSalir);

        // Panel Central
        var pnlCentro = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 10, 0, 0)
        };

        // Panel Lateral Derecho: Totales y Cobro
        var pnlTotales = new Panel
        {
            Dock = DockStyle.Right,
            Width = 320,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(15),
            AutoScroll = true
        };

        var lblResumenTitulo = new Label
        {
            Text = "RESUMEN DE VENTA",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(52, 73, 94),
            Dock = DockStyle.Top,
            Height = 25
        };

        var pnlTotalGrande = new Panel
        {
            Dock = DockStyle.Top,
            Height = 85,
            BackColor = Color.FromArgb(24, 43, 73),
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 0, 10)
        };

        var lblTotalTxt = new Label
        {
            Text = "TOTAL A PAGAR (RD$)",
            ForeColor = Color.FromArgb(173, 181, 189),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Dock = DockStyle.Top
        };

        _lblTotal = new Label
        {
            Text = "RD$ 0.00",
            ForeColor = Color.FromArgb(46, 204, 113),
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };

        pnlTotalGrande.Controls.Add(_lblTotal);
        pnlTotalGrande.Controls.Add(lblTotalTxt);

        var pnlDesglose = new Panel
        {
            Dock = DockStyle.Top,
            Height = 110,
            Padding = new Padding(0, 8, 0, 0)
        };

        var lblSubtxt = new Label { Text = "Subtotal:", Location = new Point(5, 8), AutoSize = true, Font = UITheme.BodyFont };
        _lblSubtotal = new Label { Text = "RD$ 0.00", Location = new Point(140, 8), Size = new Size(140, 20), TextAlign = ContentAlignment.MiddleRight, Font = UITheme.SectionFont };

        var lblItbistxt = new Label { Text = "ITBIS (18%):", Location = new Point(5, 36), AutoSize = true, Font = UITheme.BodyFont };
        _lblItbis = new Label { Text = "RD$ 0.00", Location = new Point(140, 36), Size = new Size(140, 20), TextAlign = ContentAlignment.MiddleRight, Font = UITheme.SectionFont };

        var lblDesctxt = new Label { Text = "Descuento:", Location = new Point(5, 68), AutoSize = true, Font = UITheme.BodyFont };
        _numDescuento = new NumericUpDown { Location = new Point(150, 66), Size = new Size(130, 26), Maximum = 100000, DecimalPlaces = 2 };
        _numDescuento.ValueChanged += (s, e) => RecalcularTotales();

        pnlDesglose.Controls.Add(lblSubtxt);
        pnlDesglose.Controls.Add(_lblSubtotal);
        pnlDesglose.Controls.Add(lblItbistxt);
        pnlDesglose.Controls.Add(_lblItbis);
        pnlDesglose.Controls.Add(lblDesctxt);
        pnlDesglose.Controls.Add(_numDescuento);

        // Botones de Cobro
        var pnlBotonesCobro = new Panel
        {
            Dock = DockStyle.Top,
            Height = 160,
            Padding = new Padding(0, 10, 0, 0)
        };

        _btnCobrar = new Button
        {
            Text = "💳 COBRAR Y FACTURAR (F12)",
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = Color.FromArgb(39, 174, 96),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnCobrar.FlatAppearance.BorderSize = 0;
        _btnCobrar.Click += async (s, e) => await ProcesarCobroVentaAsync();

        var spacerBtn = new Panel { Dock = DockStyle.Top, Height = 8 };

        _btnLimpiar = new Button
        {
            Text = "🗑️ Limpiar Carrito (F4)",
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = Color.FromArgb(231, 76, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = UITheme.SectionFont,
            Cursor = Cursors.Hand
        };
        _btnLimpiar.FlatAppearance.BorderSize = 0;
        _btnLimpiar.Click += (s, e) =>
        {
            _carrito.Clear();
            RefrescarGridCarrito();
        };

        var spacerBtn2 = new Panel { Dock = DockStyle.Top, Height = 8 };

        _btnHistorial = new Button
        {
            Text = "📜 Historial de Ventas (F6)",
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = Color.FromArgb(52, 152, 219),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = UITheme.SectionFont,
            Cursor = Cursors.Hand
        };
        _btnHistorial.FlatAppearance.BorderSize = 0;
        _btnHistorial.Click += (s, e) =>
        {
            var f = new HistorialVentasForm(_ventaService, _sesion);
            f.ShowDialog(this);
        };

        pnlBotonesCobro.Controls.Add(_btnHistorial);
        pnlBotonesCobro.Controls.Add(spacerBtn2);
        pnlBotonesCobro.Controls.Add(_btnLimpiar);
        pnlBotonesCobro.Controls.Add(spacerBtn);
        pnlBotonesCobro.Controls.Add(_btnCobrar);

        pnlTotales.Controls.Add(pnlBotonesCobro);
        pnlTotales.Controls.Add(pnlDesglose);
        pnlTotales.Controls.Add(pnlTotalGrande);
        pnlTotales.Controls.Add(lblResumenTitulo);

        // Panel Izquierdo: Datos fiscales, escáner y Carrito
        var pnlIzquierdo = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 0, 10, 0)
        };

        // Panel de Opciones Fiscales y Cliente (Top)
        var pnlFiscal = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 66,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(8, 6, 8, 6),
            ColumnCount = 4,
            RowCount = 2
        };
        pnlFiscal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlFiscal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
        pnlFiscal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22f));
        pnlFiscal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18f));
        pnlFiscal.RowStyles.Add(new RowStyle(SizeType.Absolute, 18f));
        pnlFiscal.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var lblTipoComp = new Label { Text = "Comprobante Fiscal:", AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = UITheme.TextSecondary };
        _cmbTipoComprobante = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9f),
            Margin = new Padding(0, 2, 6, 0)
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

        var lblCliente = new Label { Text = "Cliente Registrado:", AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = UITheme.TextSecondary };
        var pnlComboCliente = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 2, 6, 0) };
        _btnNuevoCliente = new Button
        {
            Text = "➕",
            Dock = DockStyle.Right,
            Width = 32,
            BackColor = Color.FromArgb(26, 35, 126),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnNuevoCliente.FlatAppearance.BorderSize = 0;
        _btnNuevoCliente.Click += async (s, e) => await RegistrarNuevoClienteRapidoAsync();

        _cmbClientes = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9f)
        };
        _cmbClientes.SelectedIndexChanged += (s, e) => SeleccionarClienteDeCombo();

        pnlComboCliente.Controls.Add(_cmbClientes);
        pnlComboCliente.Controls.Add(_btnNuevoCliente);

        var lblCliLibre = new Label { Text = "Nombre Cliente / Razón:", AutoSize = true, Font = new Font("Segoe UI", 8.5f), ForeColor = UITheme.TextSecondary };
        _txtClienteNombre = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f), PlaceholderText = "Consumidor Final", Margin = new Padding(0, 2, 6, 0) };

        var lblRnc = new Label { Text = "RNC / Cédula (B01):", AutoSize = true, Font = new Font("Segoe UI", 8.5f), ForeColor = UITheme.TextSecondary };
        _txtClienteRnc = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f), PlaceholderText = "131-12345-6", Margin = new Padding(0, 2, 0, 0) };

        pnlFiscal.Controls.Add(lblTipoComp, 0, 0);
        pnlFiscal.Controls.Add(_cmbTipoComprobante, 0, 1);
        pnlFiscal.Controls.Add(lblCliente, 1, 0);
        pnlFiscal.Controls.Add(pnlComboCliente, 1, 1);
        pnlFiscal.Controls.Add(lblCliLibre, 2, 0);
        pnlFiscal.Controls.Add(_txtClienteNombre, 2, 1);
        pnlFiscal.Controls.Add(lblRnc, 3, 0);
        pnlFiscal.Controls.Add(_txtClienteRnc, 3, 1);

        var spacerFiscal = new Panel { Dock = DockStyle.Top, Height = 8 };

        // Barra de Búsqueda y Escáner (Center-Top)
        var pnlBusqueda = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Color.FromArgb(234, 250, 234),
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(8, 6, 8, 6)
        };

        var lblScan = new Label
        {
            Text = "🔍 Escanear / SKU:",
            Dock = DockStyle.Left,
            AutoSize = true,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 132, 73),
            Padding = new Padding(0, 6, 8, 0)
        };

        var btnBuscar = new Button
        {
            Text = "➕ Agregar",
            Dock = DockStyle.Right,
            Width = 95,
            BackColor = Color.FromArgb(39, 174, 96),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnBuscar.FlatAppearance.BorderSize = 0;
        btnBuscar.Click += async (s, e) => await BuscarYAgregarProductoAsync(_txtBusquedaProducto.Text.Trim());

        _txtBusquedaProducto = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f),
            PlaceholderText = "Escanear código de barras o escribir SKU / nombre..."
        };
        _txtBusquedaProducto.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await BuscarYAgregarProductoAsync(_txtBusquedaProducto.Text.Trim());
            }
        };

        pnlBusqueda.Controls.Add(_txtBusquedaProducto);
        pnlBusqueda.Controls.Add(lblScan);
        pnlBusqueda.Controls.Add(btnBuscar);

        var spacerBusqueda = new Panel { Dock = DockStyle.Top, Height = 8 };

        // Panel Flotante de Sugerencias Autocomplete
        _pnlSugerencias = new Panel
        {
            Visible = false,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(2),
            Size = new Size(480, 180)
        };

        _lstSugerencias = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 9.5f),
            ItemHeight = 26,
            IntegralHeight = false,
            Cursor = Cursors.Hand
        };
        _pnlSugerencias.Controls.Add(_lstSugerencias);

        // Eventos de Búsqueda y Sugerencias en Vivo
        _txtBusquedaProducto.TextChanged += (s, e) =>
        {
            string query = _txtBusquedaProducto.Text.Trim();
            if (query.Length < 1)
            {
                _pnlSugerencias.Visible = false;
                return;
            }

            var coincidencias = _catalogoProductos
                .Where(p => p.Nombre.Contains(query, StringComparison.OrdinalIgnoreCase)
                         || p.Sku.Contains(query, StringComparison.OrdinalIgnoreCase)
                         || (p.CodigoBarras != null && p.CodigoBarras.Contains(query, StringComparison.OrdinalIgnoreCase))
                         || (p.Categoria != null && p.Categoria.Nombre.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .Take(10)
                .ToList();

            if (coincidencias.Count == 0)
            {
                _pnlSugerencias.Visible = false;
                return;
            }

            _lstSugerencias.BeginUpdate();
            _lstSugerencias.Items.Clear();
            foreach (var prod in coincidencias)
            {
                _lstSugerencias.Items.Add(new SugerenciaProductoItem { Producto = prod });
            }
            _lstSugerencias.SelectedIndex = 0;
            _lstSugerencias.EndUpdate();

            int altura = Math.Min(220, _lstSugerencias.Items.Count * 28 + 8);
            _pnlSugerencias.Size = new Size(Math.Max(380, _txtBusquedaProducto.Width + 20), altura);
            _pnlSugerencias.Location = new Point(pnlBusqueda.Left + 130, pnlBusqueda.Bottom + 2);
            _pnlSugerencias.Visible = true;
            _pnlSugerencias.BringToFront();
        };

        _txtBusquedaProducto.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.Down)
            {
                if (_pnlSugerencias.Visible && _lstSugerencias.Items.Count > 0)
                {
                    int next = _lstSugerencias.SelectedIndex + 1;
                    if (next < _lstSugerencias.Items.Count) _lstSugerencias.SelectedIndex = next;
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
            else if (e.KeyCode == Keys.Up)
            {
                if (_pnlSugerencias.Visible && _lstSugerencias.Items.Count > 0)
                {
                    int prev = _lstSugerencias.SelectedIndex - 1;
                    if (prev >= 0) _lstSugerencias.SelectedIndex = prev;
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
            else if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (_pnlSugerencias.Visible && _lstSugerencias.SelectedItem is SugerenciaProductoItem selItem)
                {
                    _pnlSugerencias.Visible = false;
                    _txtBusquedaProducto.Clear();
                    await AgregarProductoAlCarritoAsync(selItem.Producto);
                }
                else
                {
                    _pnlSugerencias.Visible = false;
                    await BuscarYAgregarProductoAsync(_txtBusquedaProducto.Text.Trim());
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                _pnlSugerencias.Visible = false;
            }
        };

        _lstSugerencias.Click += async (s, e) =>
        {
            if (_lstSugerencias.SelectedItem is SugerenciaProductoItem selItem)
            {
                _pnlSugerencias.Visible = false;
                _txtBusquedaProducto.Clear();
                await AgregarProductoAlCarritoAsync(selItem.Producto);
                _txtBusquedaProducto.Focus();
            }
        };

        // Grid Carrito de Venta
        var pnlGridCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(1)
        };

        _gridCarrito = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = false
        };
        UITheme.EstilizarDataGridView(_gridCarrito);
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
        pnlGridCard.Controls.Add(_gridCarrito);

        pnlIzquierdo.Controls.Add(_pnlSugerencias);
        pnlIzquierdo.Controls.Add(pnlGridCard);
        pnlIzquierdo.Controls.Add(spacerBusqueda);
        pnlIzquierdo.Controls.Add(pnlBusqueda);
        pnlIzquierdo.Controls.Add(spacerFiscal);
        pnlIzquierdo.Controls.Add(pnlFiscal);

        pnlCentro.Controls.Add(pnlIzquierdo);
        pnlCentro.Controls.Add(pnlTotales);

        pnlPrincipal.Controls.Add(pnlCentro);
        pnlPrincipal.Controls.Add(pnlHeader);
        Controls.Add(pnlPrincipal);
    }

    private void ConfigurarColumnasGrid()
    {
        _gridCarrito.Columns.Clear();
        _gridCarrito.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sku", HeaderText = "SKU", ReadOnly = true, MinimumWidth = 60, FillWeight = 50 });
        _gridCarrito.Columns.Add(new DataGridViewTextBoxColumn { Name = "Producto", HeaderText = "Producto", ReadOnly = true, MinimumWidth = 110, FillWeight = 135 });
        _gridCarrito.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cantidad", HeaderText = "Cant.", ReadOnly = false, MinimumWidth = 40, FillWeight = 38 });
        _gridCarrito.Columns.Add(new DataGridViewTextBoxColumn { Name = "Precio", HeaderText = "Precio (RD$)", ReadOnly = true, MinimumWidth = 65, FillWeight = 60 });
        _gridCarrito.Columns.Add(new DataGridViewTextBoxColumn { Name = "Itbis", HeaderText = "ITBIS 18%", ReadOnly = true, MinimumWidth = 60, FillWeight = 55 });
        _gridCarrito.Columns.Add(new DataGridViewTextBoxColumn { Name = "Total", HeaderText = "Total (RD$)", ReadOnly = true, MinimumWidth = 70, FillWeight = 65 });
        _gridCarrito.Columns.Add(new DataGridViewButtonColumn { Name = "Accion", HeaderText = "Quitar", Text = "❌", UseColumnTextForButtonValue = true, MinimumWidth = 45, FillWeight = 35 });
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

    private async Task CargarComboClientesAsync()
    {
        try
        {
            var clientes = await _clienteService.ObtenerTodosAsync(soloActivos: true);
            var lista = new List<ClienteComboItem>
            {
                new(0, "-- Consumidor Final / Sin Registro --", null)
            };
            lista.AddRange(clientes.Select(c => new ClienteComboItem(c.Id, $"{c.NombreCompleto} ({(c.Telefono ?? "S/T")})", c)));

            _cmbClientes.DisplayMember = nameof(ClienteComboItem.Texto);
            _cmbClientes.ValueMember = nameof(ClienteComboItem.Id);
            _cmbClientes.DataSource = lista;
        }
        catch
        {
            // Ignorar errores de carga inicial
        }
    }

    private void SeleccionarClienteDeCombo()
    {
        if (_cmbClientes.SelectedItem is ClienteComboItem item && item.Cliente != null)
        {
            _clienteSeleccionado = item.Cliente;
            _txtClienteNombre.Text = item.Cliente.NombreCompleto;
            _txtClienteRnc.Text = item.Cliente.RncOCedula ?? string.Empty;

            if (item.Cliente.EsFrecuente && item.Cliente.PorcentajeDescuento > 0)
            {
                // Calcular descuento porcentual
                decimal subtotal = _carrito.Sum(x => x.SubtotalSinItbis);
                decimal itbis = _carrito.Sum(x => x.Itbis);
                decimal totalSinDesc = subtotal + itbis;
                decimal desc = Math.Round(totalSinDesc * (item.Cliente.PorcentajeDescuento / 100m), 2);
                _numDescuento.Value = desc;
            }
        }
        else
        {
            _clienteSeleccionado = null;
        }
    }

    private async Task RegistrarNuevoClienteRapidoAsync()
    {
        using var modal = new ClienteModalForm();
        if (modal.ShowDialog(this) == DialogResult.OK && modal.ClienteGuardado != null)
        {
            try
            {
                var nuevo = await _clienteService.CrearAsync(modal.ClienteGuardado);
                await CargarComboClientesAsync();
                _cmbClientes.SelectedValue = nuevo.Id;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear cliente: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async Task CargarCatalogoProductosAsync()
    {
        try
        {
            _catalogoProductos = await _productoService.ObtenerProductosAsync(soloActivos: true);
        }
        catch
        {
            _catalogoProductos = new List<Producto>();
        }
    }

    private async Task BuscarYAgregarProductoAsync(string criterio)
    {
        if (string.IsNullOrWhiteSpace(criterio)) return;

        // Si es un IMEI escaneado directamente
        var unidadImei = await _productoService.ObtenerUnidadPorImeiAsync(criterio);
        if (unidadImei != null)
        {
            if (unidadImei.Estado != EstadoUnidadProducto.EnStock)
            {
                MessageBox.Show($"La unidad con IMEI '{criterio}' no está disponible (Estado: {unidadImei.Estado}).", "IMEI no disponible", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtBusquedaProducto.Clear();
                return;
            }

            await AgregarProductoAlCarritoAsync(unidadImei.Producto, unidadImei.Imei, unidadImei.Id);
            return;
        }

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

        await AgregarProductoAlCarritoAsync(prod);
    }

    private async Task AgregarProductoAlCarritoAsync(Producto prod, string? imeiSeleccionado = null, int? unidadIdSeleccionada = null)
    {
        if (prod.StockActual <= 0)
        {
            MessageBox.Show($"El producto '{prod.Nombre}' está AGOTADO en inventario.", "Stock Agotado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtBusquedaProducto.Clear();
            return;
        }

        // Si el producto requiere serie (celular) y no se escaneó directamente la unidad
        if (prod.RequiereSerie && string.IsNullOrEmpty(imeiSeleccionado))
        {
            var unidadesDisponibles = await _productoService.ObtenerUnidadesPorProductoAsync(prod.Id, EstadoUnidadProducto.EnStock);
            // Filtrar las que ya están en el carrito actual
            var imeisEnCarrito = _carrito.Where(c => c.UnidadProductoId.HasValue).Select(c => c.UnidadProductoId!.Value).ToHashSet();
            var disponibles = unidadesDisponibles.Where(u => !imeisEnCarrito.Contains(u.Id)).ToList();

            if (disponibles.Count == 0)
            {
                MessageBox.Show($"No hay unidades físicas disponibles con IMEI en stock para '{prod.Nombre}'.", "Sin IMEI en Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtBusquedaProducto.Clear();
                return;
            }

            using var dlgSel = new Form
            {
                Text = $"Seleccionar IMEI - {prod.Nombre}",
                Size = new Size(420, 240),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Font = new Font("Segoe UI", 9.5F)
            };

            var lblPrompt = new Label { Text = "Seleccione o escanee la unidad física (IMEI):", Location = new Point(15, 15), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            dlgSel.Controls.Add(lblPrompt);

            var cbImeis = new ComboBox { Location = new Point(15, 45), Width = 370, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var u in disponibles)
            {
                cbImeis.Items.Add($"{u.Imei} {(string.IsNullOrEmpty(u.Notas) ? "" : $"({u.Notas})")}");
            }
            cbImeis.SelectedIndex = 0;
            dlgSel.Controls.Add(cbImeis);

            var btnOk = new Button { Text = "Aceptar", Location = new Point(200, 140), Size = new Size(95, 32), DialogResult = DialogResult.OK };
            UITheme.AplicarBotonPrimario(btnOk);
            dlgSel.Controls.Add(btnOk);

            var btnCanc = new Button { Text = "Cancelar", Location = new Point(300, 140), Size = new Size(85, 32), DialogResult = DialogResult.Cancel };
            UITheme.AplicarBotonSecundario(btnCanc);
            dlgSel.Controls.Add(btnCanc);

            dlgSel.AcceptButton = btnOk;
            dlgSel.CancelButton = btnCanc;

            if (dlgSel.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var selUnidad = disponibles[cbImeis.SelectedIndex];
            imeiSeleccionado = selUnidad.Imei;
            unidadIdSeleccionada = selUnidad.Id;
        }

        if (prod.RequiereSerie)
        {
            // Celulares se agregan como filas individuales con su propio IMEI
            _carrito.Add(new ItemCarritoVentaDto
            {
                ProductoId = prod.Id,
                Sku = prod.Sku,
                CodigoBarras = prod.CodigoBarras,
                NombreProducto = $"{prod.Nombre} [IMEI: {imeiSeleccionado}]",
                Cantidad = 1,
                PrecioUnitario = prod.PrecioVenta,
                CostoUnitario = prod.PrecioCosto,
                StockDisponible = prod.StockActual,
                AplicaItbis = true,
                RequiereSerie = true,
                UnidadProductoId = unidadIdSeleccionada,
                Imei = imeiSeleccionado
            });
        }
        else
        {
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
                    AplicaItbis = true,
                    RequiereSerie = false
                });
            }
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
            ClienteId = _clienteSeleccionado?.Id,
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
        _clienteSeleccionado = null;
        if (_cmbClientes.Items.Count > 0) _cmbClientes.SelectedIndex = 0;
        _txtClienteNombre.Clear();
        _txtClienteRnc.Clear();
        RefrescarGridCarrito();
        _txtBusquedaProducto.Focus();
    }

    private record ClienteComboItem(int Id, string Texto, Cliente? Cliente);
}
