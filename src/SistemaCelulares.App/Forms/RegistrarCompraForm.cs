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
        StartPosition = FormStartPosition.CenterParent;
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

        _cbProductos = new ComboBox { Location = new Point(15, 34), Size = new Size(340, 28), DropDownStyle = ComboBoxStyle.DropDownList };
        _cbProductos.SelectedIndexChanged += (s, e) => ActualizarCostoSugerido();
        cardAgregar.Controls.Add(_cbProductos);

        var lblCant = new Label { Text = "Cantidad:", Font = UITheme.SectionFont, Location = new Point(375, 12), AutoSize = true };
        cardAgregar.Controls.Add(lblCant);

        _numCantidad = new NumericUpDown { Location = new Point(375, 34), Size = new Size(100, 28), Minimum = 1, Maximum = 10000, Value = 1 };
        cardAgregar.Controls.Add(_numCantidad);

        var lblCos = new Label { Text = "Costo Unitario (RD$):", Font = UITheme.SectionFont, Location = new Point(495, 12), AutoSize = true };
        cardAgregar.Controls.Add(lblCos);

        _numCostoUnitario = new NumericUpDown { Location = new Point(495, 34), Size = new Size(160, 28), DecimalPlaces = 2, ThousandsSeparator = true, Maximum = 10000000m, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
        cardAgregar.Controls.Add(_numCostoUnitario);

        _btnAgregarItem = new Button { Text = "➕ Agregar", Location = new Point(675, 30), Size = new Size(120, 36) };
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
        panelPrincipal.Controls.Add(panelGrid);

        // Panel Footer / Total y Confirmación
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 90, BackColor = Color.White, Padding = new Padding(20, 15, 20, 15) };

        _lblError = new Label { Text = string.Empty, Font = UITheme.SmallFont, ForeColor = UITheme.Danger, Location = new Point(20, 10), Size = new Size(500, 25) };
        footer.Controls.Add(_lblError);

        _lblTotal = new Label { Text = "Total Compra: RD$0.00", Font = UITheme.TitleFont, ForeColor = UITheme.Primary, Location = new Point(20, 35), AutoSize = true };
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
        panelPrincipal.Controls.Add(footer);
    }

    private void ConfigurarColumnasGrid()
    {
        _gridItems.Columns.Clear();
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductoId", HeaderText = "ID", Visible = false });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sku", HeaderText = "SKU", FillWeight = 90 });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nombre", HeaderText = "Producto", FillWeight = 180 });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cantidad", HeaderText = "Cantidad", FillWeight = 70 });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "CostoUnitario", HeaderText = "Costo Unit.", FillWeight = 100 });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Subtotal", HeaderText = "Subtotal", FillWeight = 110 });

        var btnQuitar = new DataGridViewButtonColumn
        {
            Name = "Quitar",
            HeaderText = "Acción",
            Text = "🗑️ Quitar",
            UseColumnTextForButtonValue = true,
            FillWeight = 70
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

        // Si ya está en la lista, sumar cantidad
        var existente = _itemsCompra.FirstOrDefault(i => i.ProductoId == prod.Id);
        if (existente != null)
        {
            existente.Cantidad += cant;
            existente.CostoUnitario = costo; // Tomar el último costo ingresado
            existente.Subtotal = existente.Cantidad * existente.CostoUnitario;
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
                Subtotal = cant * costo
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

        var dtoList = _itemsCompra.Select(i => new ItemCompraDto(i.ProductoId, i.Cantidad, i.CostoUnitario)).ToList();

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
                $"Compra #{compra.Id} registrada con éxito por {AppCulture.FormatearMoneda(compra.Total)}.\nEl inventario fue incrementado y los precios de costo actualizados.",
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
        public decimal Subtotal { get; set; }
    }
}
