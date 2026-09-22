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
    private CheckBox _chkRequiereSerie = null!;
    private Panel _pnlImeis = null!;
    private TextBox _txtImeis = null!;
    private Label _lblImeisConteo = null!;
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
        Size = new Size(610, 750);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.AppBg;
        Font = UITheme.BodyFont;

        var panelPrincipal = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(25, 15, 25, 20)
        };
        Controls.Add(panelPrincipal);

        var lblTitulo = new Label
        {
            Text = _productoIdParaEditar.HasValue ? "Modificar Datos del Producto" : "Nuevo Producto en Catálogo",
            Font = UITheme.SubtitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(25, 14),
            AutoSize = true
        };
        panelPrincipal.Controls.Add(lblTitulo);

        // Nombre
        var lblNom = new Label { Text = "Nombre del Producto *", Font = UITheme.SectionFont, Location = new Point(25, 46), AutoSize = true };
        panelPrincipal.Controls.Add(lblNom);

        _txtNombre = new TextBox { Location = new Point(25, 68), Size = new Size(530, 28) };
        panelPrincipal.Controls.Add(_txtNombre);

        // Categoría y SKU
        var lblCat = new Label { Text = "Categoría *", Font = UITheme.SectionFont, Location = new Point(25, 102), AutoSize = true };
        panelPrincipal.Controls.Add(lblCat);

        _cbCategorias = new ComboBox { Location = new Point(25, 124), Size = new Size(240, 28), DropDownStyle = ComboBoxStyle.DropDownList };
        _cbCategorias.SelectedIndexChanged += async (s, e) =>
        {
            _contadorClicksSku = 0;
            if (_cbCategorias.SelectedItem is Categoria cat)
            {
                var nom = cat.Nombre.ToLowerInvariant();
                bool esCelular = nom.Contains("celular") || nom.Contains("smartphone") || nom.Contains("telefono") || nom.Contains("teléfono") || nom.Contains("móvil") || nom.Contains("movil") || nom.Contains("equipo") || nom.Contains("smart");

                if (!_productoIdParaEditar.HasValue)
                {
                    _chkRequiereSerie.Visible = esCelular;
                    _chkRequiereSerie.Checked = esCelular;
                    _pnlImeis.Visible = esCelular;
                    ActualizarPosicionesFormulario();
                    await GenerarSkuAutomaticoAsync();
                }
            }
        };
        panelPrincipal.Controls.Add(_cbCategorias);

        var lblSku = new Label { Text = "Código SKU (Único) *", Font = UITheme.SectionFont, Location = new Point(280, 102), AutoSize = true };
        panelPrincipal.Controls.Add(lblSku);

        _txtSku = new TextBox { Location = new Point(280, 124), Size = new Size(175, 28), CharacterCasing = CharacterCasing.Upper };
        panelPrincipal.Controls.Add(_txtSku);

        _btnGenerarSku = new Button { Text = "⚡ Auto", Location = new Point(465, 122), Size = new Size(90, 30) };
        UITheme.AplicarBotonSecundario(_btnGenerarSku);
        _btnGenerarSku.Click += async (s, e) => await GenerarSkuAutomaticoAsync();
        panelPrincipal.Controls.Add(_btnGenerarSku);

        // Precios (Costo y Venta)
        var lblCosto = new Label { Text = "Precio de Costo (RD$) *", Font = UITheme.SectionFont, Location = new Point(25, 158), AutoSize = true };
        panelPrincipal.Controls.Add(lblCosto);

        _numPrecioCosto = new NumericUpDown
        {
            Location = new Point(25, 180),
            Size = new Size(240, 28),
            DecimalPlaces = 2,
            ThousandsSeparator = true,
            Maximum = 10000000m,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };
        _numPrecioCosto.ValueChanged += (s, e) => CalcularMargen();
        panelPrincipal.Controls.Add(_numPrecioCosto);

        var lblVenta = new Label { Text = "Precio de Venta (RD$) *", Font = UITheme.SectionFont, Location = new Point(280, 158), AutoSize = true };
        panelPrincipal.Controls.Add(lblVenta);

        _numPrecioVenta = new NumericUpDown
        {
            Location = new Point(280, 180),
            Size = new Size(275, 28),
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
            Location = new Point(25, 212),
            Size = new Size(530, 20)
        };
        panelPrincipal.Controls.Add(_lblMargen);

        // Stock y Cantidad Mínima
        var lblStock = new Label { Text = "Stock Inicial / Actual *", Font = UITheme.SectionFont, Location = new Point(25, 238), AutoSize = true };
        panelPrincipal.Controls.Add(lblStock);

        _numStock = new NumericUpDown { Location = new Point(25, 260), Size = new Size(240, 28), Maximum = 100000, Value = 1 };
        panelPrincipal.Controls.Add(_numStock);

        var lblMin = new Label { Text = "Alerta Stock Mínimo *", Font = UITheme.SectionFont, Location = new Point(280, 238), AutoSize = true };
        panelPrincipal.Controls.Add(lblMin);

        _numCantidadMinima = new NumericUpDown { Location = new Point(280, 260), Size = new Size(275, 28), Maximum = 10000, Value = 3 };
        panelPrincipal.Controls.Add(_numCantidadMinima);

        // CheckBox RequiereSerie (IMEI)
        _chkRequiereSerie = new CheckBox
        {
            Text = "📱 Es un celular / equipo con serie física (Rastreo por IMEI)",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(26, 35, 126),
            Location = new Point(25, 296),
            AutoSize = true
        };
        _chkRequiereSerie.CheckedChanged += (s, e) =>
        {
            _pnlImeis.Visible = _chkRequiereSerie.Checked;
            if (_chkRequiereSerie.Checked && !_productoIdParaEditar.HasValue)
            {
                var imeis = ObtenerListaImeisIngresados();
                _numStock.Value = imeis.Count;
                _numStock.Enabled = false;
            }
            else if (!_productoIdParaEditar.HasValue)
            {
                _numStock.Enabled = true;
                _numStock.Value = 1;
            }
            ActualizarPosicionesFormulario();
        };
        panelPrincipal.Controls.Add(_chkRequiereSerie);

        // Panel de IMEI dinámico
        _pnlImeis = new Panel
        {
            Location = new Point(25, 325),
            Size = new Size(530, 95),
            BackColor = Color.FromArgb(238, 242, 255),
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(10),
            Visible = false
        };

        var lblImeisTit = new Label
        {
            Text = "📋 Ingrese los números de IMEI / Serie (uno por línea o separados por coma):",
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(26, 35, 126),
            Location = new Point(10, 8),
            AutoSize = true
        };
        _pnlImeis.Controls.Add(lblImeisTit);

        _txtImeis = new TextBox
        {
            Location = new Point(10, 28),
            Size = new Size(508, 40),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            PlaceholderText = "358764091234567\r\n358764091234568"
        };
        _txtImeis.TextChanged += (s, e) =>
        {
            var imeis = ObtenerListaImeisIngresados();
            _lblImeisConteo.Text = $"✅ {imeis.Count} IMEI(s) identificados para registrar en stock.";
            if (!_productoIdParaEditar.HasValue && _chkRequiereSerie.Checked)
            {
                _numStock.Value = imeis.Count;
            }
        };
        _pnlImeis.Controls.Add(_txtImeis);

        _lblImeisConteo = new Label
        {
            Text = "0 IMEI(s) ingresados",
            Font = UITheme.SmallFont,
            ForeColor = UITheme.Primary,
            Location = new Point(10, 72),
            AutoSize = true
        };
        _pnlImeis.Controls.Add(_lblImeisConteo);
        panelPrincipal.Controls.Add(_pnlImeis);

        // Código de Barras
        var lblBarras = new Label { Name = "lblBarras", Text = "Código de Barras EAN-13 (opcional / escaneo / generación)", Font = UITheme.BodyFont, Location = new Point(25, 428), AutoSize = true };
        panelPrincipal.Controls.Add(lblBarras);

        _txtCodigoBarras = new TextBox { Name = "txtBarras", Location = new Point(25, 450), Size = new Size(400, 28) };
        panelPrincipal.Controls.Add(_txtCodigoBarras);

        _btnGenerarEan13 = new Button { Name = "btnEan", Text = "⚡ EAN-13", Location = new Point(435, 448), Size = new Size(120, 30) };
        UITheme.AplicarBotonSecundario(_btnGenerarEan13);
        _btnGenerarEan13.Click += async (s, e) => await GenerarEan13AutomaticoAsync();
        panelPrincipal.Controls.Add(_btnGenerarEan13);

        // Descripción
        var lblDesc = new Label { Name = "lblDesc", Text = "Descripción / Especificaciones (opcional)", Font = UITheme.BodyFont, Location = new Point(25, 485), AutoSize = true };
        panelPrincipal.Controls.Add(lblDesc);

        _txtDescripcion = new TextBox { Name = "txtDesc", Location = new Point(25, 507), Size = new Size(530, 42), Multiline = true };
        panelPrincipal.Controls.Add(_txtDescripcion);

        // Error
        _lblError = new Label { Name = "lblError", Text = string.Empty, Font = UITheme.SmallFont, ForeColor = UITheme.Danger, Location = new Point(25, 555), Size = new Size(530, 22) };
        panelPrincipal.Controls.Add(_lblError);

        // Botones
        var panelBotones = new FlowLayoutPanel
        {
            Name = "pnlBotones",
            Location = new Point(25, 582),
            Size = new Size(530, 45),
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
        ActualizarPosicionesFormulario();
    }

    private void ActualizarPosicionesFormulario()
    {
        int offset = (_chkRequiereSerie.Visible ? 36 : 0) + (_pnlImeis.Visible ? 100 : 0);
        int baseY = 296 + offset;

        var lblBarras = Controls.Find("lblBarras", true).FirstOrDefault();
        var txtBarras = Controls.Find("txtBarras", true).FirstOrDefault();
        var btnEan = Controls.Find("btnEan", true).FirstOrDefault();
        var lblDesc = Controls.Find("lblDesc", true).FirstOrDefault();
        var txtDesc = Controls.Find("txtDesc", true).FirstOrDefault();
        var lblError = Controls.Find("lblError", true).FirstOrDefault();
        var pnlBotones = Controls.Find("pnlBotones", true).FirstOrDefault();

        if (lblBarras != null) lblBarras.Location = new Point(25, baseY);
        if (txtBarras != null) txtBarras.Location = new Point(25, baseY + 22);
        if (btnEan != null) btnEan.Location = new Point(435, baseY + 20);

        if (lblDesc != null) lblDesc.Location = new Point(25, baseY + 58);
        if (txtDesc != null) txtDesc.Location = new Point(25, baseY + 80);

        if (lblError != null) lblError.Location = new Point(25, baseY + 128);
        if (pnlBotones != null) pnlBotones.Location = new Point(25, baseY + 152);
    }

    private List<string> ObtenerListaImeisIngresados()
    {
        if (string.IsNullOrWhiteSpace(_txtImeis?.Text)) return new List<string>();
        var items = _txtImeis.Text
            .Split(new[] { '\r', '\n', ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return items;
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

    private int _contadorClicksSku = 0;
    private async Task GenerarSkuAutomaticoAsync()
    {
        if (_cbCategorias.SelectedValue is int catId && catId > 0)
        {
            try
            {
                _contadorClicksSku++;
                var baseSku = await _productoService.GenerarSkuSiguienteAsync(catId);
                if (_contadorClicksSku <= 1)
                {
                    _txtSku.Text = baseSku;
                }
                else
                {
                    var partes = baseSku.Split('-');
                    if (partes.Length >= 2 && int.TryParse(partes[1], out int num))
                    {
                        var prefijo = partes[0];
                        var nuevoNum = num + (_contadorClicksSku - 1);
                        _txtSku.Text = $"{prefijo}-{nuevoNum:D4}";
                    }
                    else
                    {
                        _txtSku.Text = $"{baseSku}-{_contadorClicksSku:D2}";
                    }
                }
            }
            catch
            {
                var rnd = Random.Shared.Next(1000, 9999);
                _txtSku.Text = $"PROD-{rnd}";
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
            _chkRequiereSerie.Checked = prod.RequiereSerie;
            _chkRequiereSerie.Enabled = false; // No editable tras registrar para evitar inconsistencias
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
        var requiereSerie = _chkRequiereSerie.Checked;

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
                var imeis = requiereSerie ? ObtenerListaImeisIngresados() : new List<string>();
                int stockFinal = requiereSerie ? imeis.Count : stock;

                var nuevoProd = await _productoService.CrearProductoAsync(
                    nombre,
                    catId,
                    costo,
                    venta,
                    stockFinal,
                    min,
                    sku,
                    string.IsNullOrEmpty(codigoBarras) ? null : codigoBarras,
                    string.IsNullOrEmpty(desc) ? null : desc,
                    requiereSerie
                );

                if (requiereSerie && imeis.Count > 0)
                {
                    foreach (var imei in imeis)
                    {
                        try
                        {
                            await _productoService.RegistrarUnidadImeiAsync(nuevoProd.Id, imei);
                        }
                        catch
                        {
                            // Si alguno ya existe o falla, continuar
                        }
                    }
                }

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
