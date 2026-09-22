using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class RegistrarCompraForm : Form
{
    private readonly ICompraService _compraService;
    private readonly IProveedorService _proveedorService;
    private readonly IProductoService _productoService;
    private readonly SesionUsuario _sesionActual;

    private ComboBox _cbProveedores = null!;
    private TextBox _txtNumeroFactura = null!;
    private ComboBox _cbProductos = null!;
    private NumericUpDown _numCantidad = null!;
    private NumericUpDown _numCostoUnitario = null!;
    private NumericUpDown _numPrecioVenta = null!;
    private Button _btnAgregarItem = null!;
    private DataGridView _gridItems = null!;
    private Label _lblTotal = null!;
    private TextBox _txtObservaciones = null!;
    private Label _lblError = null!;
    private Button _btnConfirmarCompra = null!;

    private List<Producto> _todosProductos = new();
    private readonly List<ItemFilaCompra> _itemsCompra = new();

    public RegistrarCompraForm(
        ICompraService compraService,
        IProveedorService proveedorService,
        IProductoService productoService,
        SesionUsuario sesionActual)
    {
        _compraService = compraService;
        _proveedorService = proveedorService;
        _productoService = productoService;
        _sesionActual = sesionActual;

        InitializeCustomComponents();
        Load += async (s, e) =>
        {
            await CargarProveedoresAsync();
            await CargarProductosAsync();
        };
    }

    private void InitializeCustomComponents()
    {
        Text = "Registrar Compra a Proveedor - Entrada de Inventario";
        Size = new Size(1020, 720);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = UITheme.AppBg;
        Font = UITheme.BodyFont;

        var panelPrincipal = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
        Controls.Add(panelPrincipal);

        // Header
        var header = new Panel { Dock = DockStyle.Top, Height = 55 };
        var lblTitulo = new Label { Text = "Entrada de Mercancía / Compra a Proveedor", Font = UITheme.TitleFont, ForeColor = UITheme.DarkBg, Location = new Point(0, 5), AutoSize = true };
        header.Controls.Add(lblTitulo);
        var lblSub = new Label { Text = "Incremente stock y actualice el precio de costo de los productos según la factura", Font = UITheme.SmallFont, ForeColor = UITheme.TextMuted, Location = new Point(0, 32), AutoSize = true };
        header.Controls.Add(lblSub);
        panelPrincipal.Controls.Add(header);

        // Panel Datos Cabecera (Proveedor, Factura, etc.)
        var cardCabecera = new Panel { Dock = DockStyle.Top, Height = 85, BackColor = Color.White, Padding = new Padding(15), Margin = new Padding(0, 0, 0, 10) };

        var lblProv = new Label { Text = "Proveedor *", Font = UITheme.SectionFont, Location = new Point(15, 12), AutoSize = true };
        cardCabecera.Controls.Add(lblProv);

        _cbProveedores = new ComboBox { Location = new Point(15, 34), Size = new Size(340, 28), DropDownStyle = ComboBoxStyle.DropDownList };
        cardCabecera.Controls.Add(_cbProveedores);

        var lblFac = new Label { Text = "N° Factura / Comprobante", Font = UITheme.SectionFont, Location = new Point(375, 12), AutoSize = true };
        cardCabecera.Controls.Add(lblFac);

        _txtNumeroFactura = new TextBox { Location = new Point(375, 34), Size = new Size(200, 28) };
        cardCabecera.Controls.Add(_txtNumeroFactura);

        var lblObs = new Label { Text = "Observaciones", Font = UITheme.BodyFont, Location = new Point(595, 12), AutoSize = true };
        cardCabecera.Controls.Add(lblObs);

        _txtObservaciones = new TextBox { Location = new Point(595, 34), Size = new Size(360, 28) };
        cardCabecera.Controls.Add(_txtObservaciones);

        panelPrincipal.Controls.Add(cardCabecera);

        // Panel Agregar Ítem
        var cardAgregar = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = UITheme.PrimaryLight, Padding = new Padding(15), Margin = new Padding(0, 10, 0, 10) };

        var lblSelProd = new Label { Text = "Seleccionar Producto:", Font = UITheme.SectionFont, Location = new Point(15, 12), AutoSize = true };
        cardAgregar.Controls.Add(lblSelProd);

        _cbProductos = new ComboBox { Location = new Point(15, 34), Size = new Size(300, 28), DropDownStyle = ComboBoxStyle.DropDownList };
        _cbProductos.SelectedIndexChanged += (s, e) => ActualizarCostoSugerido();
        cardAgregar.Controls.Add(_cbProductos);

        var lblCant = new Label { Text = "Cantidad:", Font = UITheme.SectionFont, Location = new Point(330, 12), AutoSize = true };
        cardAgregar.Controls.Add(lblCant);

        _numCantidad = new NumericUpDown { Location = new Point(330, 34), Size = new Size(90, 28), Minimum = 1, Maximum = 10000, Value = 1 };
        cardAgregar.Controls.Add(_numCantidad);

        var lblCos = new Label { Text = "Costo Unit. (RD$):", Font = UITheme.SectionFont, Location = new Point(435, 12), AutoSize = true };
        cardAgregar.Controls.Add(lblCos);

        _numCostoUnitario = new NumericUpDown { Location = new Point(435, 34), Size = new Size(130, 28), DecimalPlaces = 2, ThousandsSeparator = true, Maximum = 10000000m, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
        cardAgregar.Controls.Add(_numCostoUnitario);

        var lblPrcVenta = new Label { Text = "Precio Venta (RD$):", Font = UITheme.SectionFont, Location = new Point(580, 12), AutoSize = true };
        cardAgregar.Controls.Add(lblPrcVenta);

        _numPrecioVenta = new NumericUpDown { Location = new Point(580, 34), Size = new Size(130, 28), DecimalPlaces = 2, ThousandsSeparator = true, Maximum = 10000000m, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
        cardAgregar.Controls.Add(_numPrecioVenta);

        _btnAgregarItem = new Button { Text = "➕ Agregar", Location = new Point(725, 30), Size = new Size(110, 36) };
        UITheme.AplicarBotonPrimario(_btnAgregarItem);
        _btnAgregarItem.Click += (s, e) => AgregarItemACompra();
        cardAgregar.Controls.Add(_btnAgregarItem);

        panelPrincipal.Controls.Add(cardAgregar);

        // DataGridView de Ítems
        var panelGrid = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(1) };
        _gridItems = new DataGridView { Dock = DockStyle.Fill };
        UITheme.EstilizarDataGridView(_gridItems);
        ConfigurarColumnasGrid();
        panelGrid.Controls.Add(_gridItems);

        // Panel Footer / Total y Confirmación
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 90, BackColor = Color.White, Padding = new Padding(20, 15, 20, 15) };

        _lblError = new Label { Text = string.Empty, Font = UITheme.SmallFont, ForeColor = UITheme.Danger, Location = new Point(20, 10), Size = new Size(500, 25) };
        footer.Controls.Add(_lblError);

        _lblTotal = new Label { Text = "Total Compra: RD$0.00 (0 unidades)", Font = UITheme.TitleFont, ForeColor = UITheme.Primary, Location = new Point(20, 35), AutoSize = true };
        footer.Controls.Add(_lblTotal);

        var panelBotones = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.RightToLeft, Width = 400, Height = 60 };

        var btnCancelar = new Button { Text = "Cancelar", Size = new Size(100, 42), DialogResult = DialogResult.Cancel };
        UITheme.AplicarBotonSecundario(btnCancelar);
        panelBotones.Controls.Add(btnCancelar);

        _btnConfirmarCompra = new Button { Text = "✅ Confirmar Compra", Size = new Size(190, 42) };
        UITheme.AplicarBotonPrimario(_btnConfirmarCompra);
        _btnConfirmarCompra.Click += async (s, e) => await ProcesarRegistroCompraAsync();
        panelBotones.Controls.Add(_btnConfirmarCompra);

        footer.Controls.Add(panelBotones);

        // Agregar al panel principal en orden de acoplamiento estricto para que no se invierta
        panelPrincipal.Controls.Add(panelGrid);      // Fill va primero
        panelPrincipal.Controls.Add(footer);         // Bottom
        panelPrincipal.Controls.Add(cardAgregar);    // Top 3 (abajo)
        panelPrincipal.Controls.Add(cardCabecera);   // Top 2 (medio)
        panelPrincipal.Controls.Add(header);         // Top 1 (arriba del todo)
    }

    private void ConfigurarColumnasGrid()
    {
        _gridItems.Columns.Clear();
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductoId", HeaderText = "ID", Visible = false });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sku", HeaderText = "SKU", FillWeight = 80 });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nombre", HeaderText = "Producto", FillWeight = 160 });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cantidad", HeaderText = "Cant.", FillWeight = 60 });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "CostoUnitario", HeaderText = "Costo Unit.", FillWeight = 90 });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrecioVenta", HeaderText = "Precio Venta", FillWeight = 90 });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Subtotal", HeaderText = "Subtotal", FillWeight = 100 });

        var btnQuitar = new DataGridViewButtonColumn
        {
            Name = "Quitar",
            HeaderText = "Acción",
            Text = "🗑️ Quitar",
            UseColumnTextForButtonValue = true,
            FillWeight = 65
        };
        _gridItems.Columns.Add(btnQuitar);
        _gridItems.CellContentClick += (s, e) =>
        {
            if (e.RowIndex >= 0 && _gridItems.Columns[e.ColumnIndex].Name == "Quitar")
            {
                _itemsCompra.RemoveAt(e.RowIndex);
                RefrescarGridItems();
            }
        };
    }

    private async Task CargarProveedoresAsync()
    {
        var provs = await _proveedorService.ObtenerProveedoresAsync(soloActivos: true);
        _cbProveedores.DisplayMember = nameof(Proveedor.Nombre);
        _cbProveedores.ValueMember = nameof(Proveedor.Id);
        _cbProveedores.DataSource = provs;
    }

    private async Task CargarProductosAsync()
    {
        _todosProductos = await _productoService.ObtenerProductosAsync(soloActivos: true);
        _cbProductos.DisplayMember = "Nombre";
        _cbProductos.ValueMember = "Id";
        _cbProductos.DataSource = _todosProductos;
        ActualizarCostoSugerido();
    }

    private void ActualizarCostoSugerido()
    {
        if (_cbProductos.SelectedItem is Producto prod)
        {
            _numCostoUnitario.Value = prod.PrecioCosto;
            _numPrecioVenta.Value = prod.PrecioVenta;
        }
    }

    private void AgregarItemACompra()
    {
        _lblError.Text = string.Empty;
        if (_cbProductos.SelectedItem is not Producto prod) return;

        var cant = (int)_numCantidad.Value;
        var costo = _numCostoUnitario.Value;

        if (cant <= 0)
        {
            _lblError.Text = "La cantidad debe ser mayor a cero.";
            return;
        }

        var imeis = new List<string>();
        if (prod.RequiereSerie)
        {
            // Solicitar los IMEIs de las unidades compradas
            using var dlgImeis = new Form
            {
                Text = $"Ingresar IMEIs para {cant} unidades de {prod.Nombre}",
                Size = new Size(460, 380),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Font = new Font("Segoe UI", 9.5F)
            };

            var lblPrompt = new Label
            {
                Text = $"Ingrese los {cant} IMEIs (uno por línea o separados por comas):",
                Location = new Point(15, 12),
                Size = new Size(420, 35),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            dlgImeis.Controls.Add(lblPrompt);

            var txtImeis = new TextBox
            {
                Location = new Point(15, 50),
                Size = new Size(415, 220),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            dlgImeis.Controls.Add(txtImeis);

            var btnOk = new Button { Text = "Aceptar", Location = new Point(230, 285), Size = new Size(100, 32), DialogResult = DialogResult.OK };
            UITheme.AplicarBotonPrimario(btnOk);
            dlgImeis.Controls.Add(btnOk);

            var btnCanc = new Button { Text = "Cancelar", Location = new Point(340, 285), Size = new Size(90, 32), DialogResult = DialogResult.Cancel };
            UITheme.AplicarBotonSecundario(btnCanc);
            dlgImeis.Controls.Add(btnCanc);

            dlgImeis.AcceptButton = btnOk;
            dlgImeis.CancelButton = btnCanc;

            if (dlgImeis.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var raw = txtImeis.Text.Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .ToList();

            if (raw.Count != cant)
            {
                MessageBox.Show($"Debe ingresar exactamente {cant} IMEIs únicos. Se ingresaron {raw.Count}.", "Error de Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            imeis = raw;
        }

        var precioVenta = _numPrecioVenta.Value;

        // Si ya está en la lista y no requiere serie, sumar cantidad; si requiere serie, agregar como línea separada o concatenar
        var existente = _itemsCompra.FirstOrDefault(i => i.ProductoId == prod.Id);
        if (existente != null && !prod.RequiereSerie)
        {
            existente.Cantidad += cant;
            existente.CostoUnitario = costo; // Tomar el último costo ingresado
            existente.PrecioVenta = precioVenta;
            existente.Subtotal = existente.Cantidad * existente.CostoUnitario;
        }
        else if (existente != null && prod.RequiereSerie)
        {
            existente.Cantidad += cant;
            existente.CostoUnitario = costo;
            existente.PrecioVenta = precioVenta;
            existente.Subtotal = existente.Cantidad * existente.CostoUnitario;
            existente.Imeis.AddRange(imeis);
        }
        else
        {
            _itemsCompra.Add(new ItemFilaCompra
            {
                ProductoId = prod.Id,
                Sku = prod.Sku,
                Nombre = prod.Nombre,
                Cantidad = cant,
                CostoUnitario = costo,
                PrecioVenta = precioVenta,
                Subtotal = cant * costo,
                RequiereSerie = prod.RequiereSerie,
                Imeis = imeis
            });
        }

        RefrescarGridItems();
        _numCantidad.Value = 1;
    }

    private void RefrescarGridItems()
    {
        _gridItems.Rows.Clear();
        decimal total = 0;

        foreach (var i in _itemsCompra)
        {
            _gridItems.Rows.Add(
                i.ProductoId,
                i.Sku,
                i.Nombre,
                i.Cantidad,
                AppCulture.FormatearMoneda(i.CostoUnitario),
                AppCulture.FormatearMoneda(i.PrecioVenta),
                AppCulture.FormatearMoneda(i.Subtotal)
            );
            total += i.Subtotal;
        }

        _lblTotal.Text = $"Total Compra: {AppCulture.FormatearMoneda(total)} ({_itemsCompra.Sum(x => x.Cantidad)} unidades)";
    }

    private async Task ProcesarRegistroCompraAsync()
    {
        _lblError.Text = string.Empty;

        if (_cbProveedores.SelectedValue == null)
        {
            _lblError.Text = "Debe seleccionar un proveedor.";
            return;
        }

        if (_itemsCompra.Count == 0)
        {
            _lblError.Text = "Debe agregar al menos un producto a la compra.";
            return;
        }

        var provId = (int)_cbProveedores.SelectedValue;
        var factura = _txtNumeroFactura.Text.Trim();
        var obs = _txtObservaciones.Text.Trim();

        var dtoList = _itemsCompra.Select(i => new ItemCompraDto(i.ProductoId, i.Cantidad, i.CostoUnitario, i.Imeis.Count > 0 ? i.Imeis : null, i.PrecioVenta > 0 ? i.PrecioVenta : null)).ToList();

        _btnConfirmarCompra.Enabled = false;
        try
        {
            var compra = await _compraService.RegistrarCompraAsync(
                provId,
                _sesionActual.UsuarioId,
                factura,
                obs,
                dtoList
            );

            MessageBox.Show(
                $"Compra #{compra.Id} registrada con éxito por {AppCulture.FormatearMoneda(compra.Total)}.\nEl inventario fue incrementado y los precios fueron actualizados.",
                "Compra Registrada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _lblError.Text = ex.Message;
        }
        finally
        {
            _btnConfirmarCompra.Enabled = true;
        }
    }

    private class ItemFilaCompra
    {
        public int ProductoId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal PrecioVenta { get; set; }
        public decimal Subtotal { get; set; }
        public bool RequiereSerie { get; set; }
        public List<string> Imeis { get; set; } = new();
    }
}
