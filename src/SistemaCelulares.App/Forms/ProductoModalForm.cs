using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;

namespace SistemaCelulares.App.Forms;

public class ProductoModalForm : Form
{
    private readonly IProductoService _productoService;
    private readonly ICategoriaService _categoriaService;
    private readonly IEan13GeneratorService? _ean13Service;
    private readonly int? _productoIdParaEditar;

    private TextBox _txtNombre = null!;
    private ComboBox _cbCategorias = null!;
    private TextBox _txtSku = null!;
    private Button _btnGenerarSku = null!;
    private TextBox _txtCodigoBarras = null!;
    private Button _btnGenerarEan13 = null!;
    private NumericUpDown _numPrecioCosto = null!;
    private NumericUpDown _numPrecioVenta = null!;
    private Label _lblMargen = null!;
    private NumericUpDown _numStock = null!;
    private NumericUpDown _numCantidadMinima = null!;
    private TextBox _txtDescripcion = null!;
    private Label _lblError = null!;
    private Button _btnGuardar = null!;

    public ProductoModalForm(
        IProductoService productoService,
        ICategoriaService categoriaService,
        IEan13GeneratorService? ean13Service = null,
        int? productoId = null,
        string? codigoBarrasInicial = null)
    {
        _productoService = productoService;
        _categoriaService = categoriaService;
        _ean13Service = ean13Service;
        _productoIdParaEditar = productoId;

        InitializeCustomComponents();

        if (!string.IsNullOrWhiteSpace(codigoBarrasInicial))
        {
            _txtCodigoBarras.Text = codigoBarrasInicial.Trim();
        }

        Load += async (s, e) =>
        {
            await CargarCategoriasAsync();
            if (_productoIdParaEditar.HasValue)
            {
                await CargarProductoParaEditarAsync();
            }
            else
            {
                await GenerarSkuAutomaticoAsync();
            }
            CalcularMargen();
        };
    }

    private void InitializeCustomComponents()
    {
        Text = _productoIdParaEditar.HasValue ? "Editar Producto" : "Registrar Nuevo Producto";
        Size = new Size(580, 680);
        StartPosition = FormStartPosition.CenterParent;
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

        var lblTitulo = new Label
        {
            Text = _productoIdParaEditar.HasValue ? "Modificar Datos del Producto" : "Nuevo Producto en Catálogo",
            Font = UITheme.SubtitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(0, 0),
            AutoSize = true
        };
        panelPrincipal.Controls.Add(lblTitulo);

        // Nombre
        var lblNom = new Label { Text = "Nombre del Producto *", Font = UITheme.SectionFont, Location = new Point(0, 32), AutoSize = true };
        panelPrincipal.Controls.Add(lblNom);

        _txtNombre = new TextBox { Location = new Point(0, 54), Size = new Size(520, 28) };
        panelPrincipal.Controls.Add(_txtNombre);

        // Categoría y SKU
        var lblCat = new Label { Text = "Categoría *", Font = UITheme.SectionFont, Location = new Point(0, 90), AutoSize = true };
        panelPrincipal.Controls.Add(lblCat);

        _cbCategorias = new ComboBox { Location = new Point(0, 112), Size = new Size(240, 28), DropDownStyle = ComboBoxStyle.DropDownList };
        _cbCategorias.SelectedIndexChanged += async (s, e) =>
        {
            if (!_productoIdParaEditar.HasValue && _cbCategorias.SelectedValue is int catId && catId > 0)
            {
                await GenerarSkuAutomaticoAsync();
            }
        };
        panelPrincipal.Controls.Add(_cbCategorias);

        var lblSku = new Label { Text = "Código SKU (Único) *", Font = UITheme.SectionFont, Location = new Point(260, 90), AutoSize = true };
        panelPrincipal.Controls.Add(lblSku);

        _txtSku = new TextBox { Location = new Point(260, 112), Size = new Size(160, 28), CharacterCasing = CharacterCasing.Upper };
        panelPrincipal.Controls.Add(_txtSku);

        _btnGenerarSku = new Button { Text = "⚡ Auto", Location = new Point(430, 110), Size = new Size(90, 30) };
        UITheme.AplicarBotonSecundario(_btnGenerarSku);
        _btnGenerarSku.Click += async (s, e) => await GenerarSkuAutomaticoAsync();
        panelPrincipal.Controls.Add(_btnGenerarSku);

        // Precios (Costo y Venta)
        var lblCosto = new Label { Text = "Precio de Costo (RD$) *", Font = UITheme.SectionFont, Location = new Point(0, 150), AutoSize = true };
        panelPrincipal.Controls.Add(lblCosto);

        _numPrecioCosto = new NumericUpDown
        {
            Location = new Point(0, 172),
            Size = new Size(240, 28),
            DecimalPlaces = 2,
            ThousandsSeparator = true,
            Maximum = 10000000m,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };
        _numPrecioCosto.ValueChanged += (s, e) => CalcularMargen();
        panelPrincipal.Controls.Add(_numPrecioCosto);

        var lblVenta = new Label { Text = "Precio de Venta (RD$) *", Font = UITheme.SectionFont, Location = new Point(260, 150), AutoSize = true };
        panelPrincipal.Controls.Add(lblVenta);

        _numPrecioVenta = new NumericUpDown
        {
            Location = new Point(260, 172),
            Size = new Size(260, 28),
            DecimalPlaces = 2,
            ThousandsSeparator = true,
            Maximum = 10000000m,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };
        _numPrecioVenta.ValueChanged += (s, e) => CalcularMargen();
        panelPrincipal.Controls.Add(_numPrecioVenta);

        // Margen
        _lblMargen = new Label
        {
            Text = "Margen de Ganancia: RD$0.00 (0.0%)",
            Font = UITheme.SmallFont,
            ForeColor = UITheme.Success,
            Location = new Point(0, 205),
            Size = new Size(520, 20)
        };
        panelPrincipal.Controls.Add(_lblMargen);

        // Stock y Cantidad Mínima
        var lblStock = new Label { Text = "Stock Inicial / Actual *", Font = UITheme.SectionFont, Location = new Point(0, 235), AutoSize = true };
        panelPrincipal.Controls.Add(lblStock);

        _numStock = new NumericUpDown { Location = new Point(0, 257), Size = new Size(240, 28), Maximum = 100000, Value = 1 };
        panelPrincipal.Controls.Add(_numStock);

        var lblMin = new Label { Text = "Alerta Stock Mínimo *", Font = UITheme.SectionFont, Location = new Point(260, 235), AutoSize = true };
        panelPrincipal.Controls.Add(lblMin);

        _numCantidadMinima = new NumericUpDown { Location = new Point(260, 257), Size = new Size(260, 28), Maximum = 10000, Value = 3 };
        panelPrincipal.Controls.Add(_numCantidadMinima);

        // Código de Barras
        var lblBarras = new Label { Text = "Código de Barras EAN-13 (opcional / escaneo / generación)", Font = UITheme.BodyFont, Location = new Point(0, 295), AutoSize = true };
        panelPrincipal.Controls.Add(lblBarras);

        _txtCodigoBarras = new TextBox { Location = new Point(0, 317), Size = new Size(390, 28) };
        panelPrincipal.Controls.Add(_txtCodigoBarras);

        _btnGenerarEan13 = new Button { Text = "⚡ EAN-13", Location = new Point(400, 315), Size = new Size(120, 30) };
        UITheme.AplicarBotonSecundario(_btnGenerarEan13);
        _btnGenerarEan13.Click += async (s, e) => await GenerarEan13AutomaticoAsync();
        panelPrincipal.Controls.Add(_btnGenerarEan13);

        // Descripción
        var lblDesc = new Label { Text = "Descripción / Especificaciones (opcional)", Font = UITheme.BodyFont, Location = new Point(0, 355), AutoSize = true };
        panelPrincipal.Controls.Add(lblDesc);

        _txtDescripcion = new TextBox { Location = new Point(0, 377), Size = new Size(520, 50), Multiline = true };
        panelPrincipal.Controls.Add(_txtDescripcion);

        // Error
        _lblError = new Label { Text = string.Empty, Font = UITheme.SmallFont, ForeColor = UITheme.Danger, Location = new Point(0, 440), Size = new Size(520, 30) };
        panelPrincipal.Controls.Add(_lblError);

        // Botones
        var panelBotones = new FlowLayoutPanel
        {
            Location = new Point(0, 480),
            Size = new Size(520, 45),
            FlowDirection = FlowDirection.RightToLeft
        };

        var btnCancelar = new Button { Text = "Cancelar", Size = new Size(95, 38), DialogResult = DialogResult.Cancel };
        UITheme.AplicarBotonSecundario(btnCancelar);
        panelBotones.Controls.Add(btnCancelar);

        _btnGuardar = new Button { Text = "Guardar Producto", Size = new Size(140, 38) };
        UITheme.AplicarBotonPrimario(_btnGuardar);
        _btnGuardar.Click += async (s, e) => await GuardarProductoAsync();
        panelBotones.Controls.Add(_btnGuardar);

        panelPrincipal.Controls.Add(panelBotones);
    }

    private void CalcularMargen()
    {
        var costo = _numPrecioCosto.Value;
        var venta = _numPrecioVenta.Value;
        var ganancia = venta - costo;
        var porcentaje = costo > 0 ? (ganancia / costo) * 100 : 0;

        _lblMargen.Text = $"Margen estimado: {AppCulture.FormatearMoneda(ganancia)} ({porcentaje:F1}%)";
        if (ganancia < 0)
        {
            _lblMargen.ForeColor = UITheme.Danger;
        }
        else
        {
            _lblMargen.ForeColor = UITheme.Success;
        }
    }

    private async Task CargarCategoriasAsync()
    {
        var cats = await _categoriaService.ObtenerCategoriasAsync(soloActivas: true);
        _cbCategorias.DisplayMember = nameof(Categoria.Nombre);
        _cbCategorias.ValueMember = nameof(Categoria.Id);
        _cbCategorias.DataSource = cats;
    }

    private async Task GenerarSkuAutomaticoAsync()
    {
        if (_cbCategorias.SelectedValue is int catId && catId > 0)
        {
            try
            {
                var sku = await _productoService.GenerarSkuSiguienteAsync(catId);
                _txtSku.Text = sku;
            }
            catch
            {
                // Ignorar si aún no hay categorías cargadas
            }
        }
    }

    private async Task GenerarEan13AutomaticoAsync()
    {
        if (_ean13Service != null)
        {
            try
            {
                var ean13 = await _ean13Service.GenerarEan13InternoAsync();
                _txtCodigoBarras.Text = ean13;
            }
            catch (Exception ex)
            {
                _lblError.Text = "Error al generar código EAN-13: " + ex.Message;
            }
        }
    }

    private async Task CargarProductoParaEditarAsync()
    {
        var prod = await _productoService.ObtenerPorIdAsync(_productoIdParaEditar!.Value);
        if (prod != null)
        {
            _txtNombre.Text = prod.Nombre;
            _cbCategorias.SelectedValue = prod.CategoriaId;
            _txtSku.Text = prod.Sku;
            _txtCodigoBarras.Text = prod.CodigoBarras ?? string.Empty;
            _numPrecioCosto.Value = prod.PrecioCosto;
            _numPrecioVenta.Value = prod.PrecioVenta;
            _numStock.Value = prod.StockActual;
            _numCantidadMinima.Value = prod.CantidadMinima;
            _txtDescripcion.Text = prod.Descripcion ?? string.Empty;
        }
    }

    private async Task GuardarProductoAsync()
    {
        _lblError.Text = string.Empty;
        var nombre = _txtNombre.Text.Trim();
        var sku = _txtSku.Text.Trim().ToUpper();
        var codigoBarras = _txtCodigoBarras.Text.Trim();
        var desc = _txtDescripcion.Text.Trim();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            _lblError.Text = "El nombre del producto es obligatorio.";
            return;
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            _lblError.Text = "El código SKU es obligatorio.";
            return;
        }

        if (_cbCategorias.SelectedValue == null)
        {
            _lblError.Text = "Debe seleccionar una categoría.";
            return;
        }

        var catId = (int)_cbCategorias.SelectedValue;
        var costo = _numPrecioCosto.Value;
        var venta = _numPrecioVenta.Value;
        var stock = (int)_numStock.Value;
        var min = (int)_numCantidadMinima.Value;

        _btnGuardar.Enabled = false;
        try
        {
            if (_productoIdParaEditar.HasValue)
            {
                var ok = await _productoService.ActualizarProductoAsync(
                    _productoIdParaEditar.Value,
                    nombre,
                    catId,
                    costo,
                    venta,
                    min,
                    sku,
                    string.IsNullOrEmpty(codigoBarras) ? null : codigoBarras,
                    string.IsNullOrEmpty(desc) ? null : desc
                );

                if (ok)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            else
            {
                await _productoService.CrearProductoAsync(
                    nombre,
                    catId,
                    costo,
                    venta,
                    stock,
                    min,
                    sku,
                    string.IsNullOrEmpty(codigoBarras) ? null : codigoBarras,
                    string.IsNullOrEmpty(desc) ? null : desc
                );

                DialogResult = DialogResult.OK;
                Close();
            }
        }
        catch (Exception ex)
        {
            _lblError.Text = ex.Message;
        }
        finally
        {
            _btnGuardar.Enabled = true;
        }
    }
}
