using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Constants;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class ProductosForm : Form
{
    private readonly IProductoService _productoService;
    private readonly ICategoriaService _categoriaService;
    private readonly IEan13GeneratorService _ean13Service;
    private readonly SesionUsuario _sesionActual;

    private DataGridView _gridProductos = null!;
    private TextBox _txtBuscar = null!;
    private ComboBox _cbCategorias = null!;
    private CheckBox _chkSoloActivos = null!;
    private CheckBox _chkBajoStock = null!;
    private Label _lblResumen = null!;

    private Button _btnNuevo = null!;
    private Button _btnEditar = null!;
    private Button _btnToggleEstado = null!;
    private Button _btnCategorias = null!;
    private Button _btnImprimirEtiqueta = null!;

    private List<Producto> _listaProductos = new();

    public ProductosForm(
        IProductoService productoService,
        ICategoriaService categoriaService,
        IEan13GeneratorService ean13Service,
        SesionUsuario sesionActual)
    {
        _productoService = productoService;
        _categoriaService = categoriaService;
        _ean13Service = ean13Service;
        _sesionActual = sesionActual;

        InitializeCustomComponents();
        Load += async (s, e) =>
        {
            await CargarComboCategoriasAsync();
            await RecargarProductosAsync();
        };
    }

    private void InitializeCustomComponents()
    {
        Text = "Catálogo de Productos y Precios";
        Size = new Size(1100, 680);
        BackColor = UITheme.AppBg;
        Font = UITheme.BodyFont;

        var panelPrincipal = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };
        Controls.Add(panelPrincipal);

        // Header
        var header = new Panel { Dock = DockStyle.Top, Height = 60 };
        var lblTitulo = new Label
        {
            Text = "Catálogo de Productos y Precios",
            Font = UITheme.TitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(0, 5),
            AutoSize = true
        };
        header.Controls.Add(lblTitulo);

        _lblResumen = new Label
        {
            Text = "Cargando catálogo...",
            Font = UITheme.SmallFont,
            ForeColor = UITheme.TextMuted,
            Location = new Point(0, 35),
            AutoSize = true
        };
        header.Controls.Add(_lblResumen);

        // Toolbar Contenedor
        var toolbarContenedor = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0, 0, 0, 8)
        };

        // Fila 1: Botones de Acción
        var flowBotones = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 2, 0, 4)
        };

        _btnNuevo = new Button { Text = "➕ Nuevo Producto", Size = new Size(140, 34), Margin = new Padding(0, 0, 6, 6) };
        UITheme.AplicarBotonPrimario(_btnNuevo);
        _btnNuevo.Click += async (s, e) => await AbrirCrearProductoAsync();
        _btnNuevo.Visible = _sesionActual.EsSuperAdmin || _sesionActual.EsAdmin || _sesionActual.TienePermiso(Permisos.ProductosCrear);
        flowBotones.Controls.Add(_btnNuevo);

        _btnEditar = new Button { Text = "✏️ Editar", Size = new Size(85, 34), Margin = new Padding(0, 0, 6, 6) };
        UITheme.AplicarBotonSecundario(_btnEditar);
        _btnEditar.Click += async (s, e) => await AbrirEditarProductoAsync();
        _btnEditar.Visible = _sesionActual.EsSuperAdmin || _sesionActual.EsAdmin || _sesionActual.TienePermiso(Permisos.ProductosEditar);
        flowBotones.Controls.Add(_btnEditar);

        var btnImeis = new Button { Text = "📱 IMEIs / Series", Size = new Size(125, 34), Margin = new Padding(0, 0, 6, 6) };
        UITheme.AplicarBotonSecundario(btnImeis);
        btnImeis.Click += (s, e) => AbrirGestionImeis();
        flowBotones.Controls.Add(btnImeis);

        _btnImprimirEtiqueta = new Button { Text = "🏷️ Imprimir Etiqueta", Size = new Size(145, 34), Margin = new Padding(0, 0, 6, 6) };
        UITheme.AplicarBotonSecundario(_btnImprimirEtiqueta);
        _btnImprimirEtiqueta.Click += (s, e) => AbrirImprimirEtiqueta();
        flowBotones.Controls.Add(_btnImprimirEtiqueta);

        var btnVerificador = new Button { Text = "📸 Lector", Size = new Size(85, 34), Margin = new Padding(0, 0, 6, 6) };
        UITheme.AplicarBotonSecundario(btnVerificador);
        btnVerificador.Click += (s, e) => AbrirVerificadorPrecio();
        flowBotones.Controls.Add(btnVerificador);

        _btnToggleEstado = new Button { Text = "🔄 Act/Desc", Size = new Size(95, 34), Margin = new Padding(0, 0, 6, 6) };
        UITheme.AplicarBotonSecundario(_btnToggleEstado);
        _btnToggleEstado.Click += async (s, e) => await ToggleEstadoProductoAsync();
        _btnToggleEstado.Visible = _sesionActual.EsSuperAdmin || _sesionActual.EsAdmin || _sesionActual.TienePermiso(Permisos.ProductosDesactivar);
        flowBotones.Controls.Add(_btnToggleEstado);

        _btnCategorias = new Button { Text = "📁 Categorías", Size = new Size(115, 34), Margin = new Padding(0, 0, 6, 6) };
        UITheme.AplicarBotonSecundario(_btnCategorias);
        _btnCategorias.Click += async (s, e) => await AbrirCategoriasFormAsync();
        _btnCategorias.Visible = _sesionActual.EsSuperAdmin || _sesionActual.EsAdmin || _sesionActual.TienePermiso(Permisos.CategoriasGestionar);
        flowBotones.Controls.Add(_btnCategorias);

        // Fila 2: Filtros de Búsqueda
        var flowFiltros = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 4, 0, 4)
        };

        var lblBuscar = new Label { Text = "🔍 Buscar:", AutoSize = true, Font = UITheme.SectionFont, Margin = new Padding(0, 5, 4, 0) };
        flowFiltros.Controls.Add(lblBuscar);

        _txtBuscar = new TextBox { Size = new Size(160, 26), Font = new Font("Segoe UI", 9F), PlaceholderText = "Buscar o escanear...", Margin = new Padding(0, 2, 10, 0) };
        _txtBuscar.TextChanged += (s, e) => FiltrarGrid();
        BarcodeScannerHelper.ConfigurarParaEscaneo(_txtBuscar, async (codigo) =>
        {
            _txtBuscar.Text = codigo;
            FiltrarGrid();
            await Task.CompletedTask;
        });
        flowFiltros.Controls.Add(_txtBuscar);

        var lblCat = new Label { Text = "Categoría:", AutoSize = true, Font = UITheme.BodyFont, Margin = new Padding(0, 5, 4, 0) };
        flowFiltros.Controls.Add(lblCat);

        _cbCategorias = new ComboBox { Size = new Size(170, 26), DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 2, 10, 0) };
        _cbCategorias.SelectedIndexChanged += async (s, e) => await RecargarProductosAsync();
        flowFiltros.Controls.Add(_cbCategorias);

        _chkBajoStock = new CheckBox { Text = "⚠️ Bajo stock", Checked = false, AutoSize = true, Font = UITheme.SectionFont, ForeColor = UITheme.Danger, Margin = new Padding(0, 4, 10, 0) };
        _chkBajoStock.CheckedChanged += (s, e) => FiltrarGrid();
        flowFiltros.Controls.Add(_chkBajoStock);

        _chkSoloActivos = new CheckBox { Text = "Solo activos", Checked = true, AutoSize = true, Margin = new Padding(0, 5, 0, 0) };
        _chkSoloActivos.CheckedChanged += async (s, e) => await RecargarProductosAsync();
        flowFiltros.Controls.Add(_chkSoloActivos);

        // Agregar a toolbar en orden correcto: filtros abajo, botones arriba
        toolbarContenedor.Controls.Add(flowFiltros);
        toolbarContenedor.Controls.Add(flowBotones);

        // DataGridView
        var panelGrid = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(1)
        };

        _gridProductos = new DataGridView { Dock = DockStyle.Fill };
        UITheme.EstilizarDataGridView(_gridProductos);
        ConfigurarColumnasGrid();
        panelGrid.Controls.Add(_gridProductos);

        // Agregar al panelPrincipal en orden de acoplamiento correcto
        panelPrincipal.Controls.Add(panelGrid);
        panelPrincipal.Controls.Add(toolbarContenedor);
        panelPrincipal.Controls.Add(header);
    }

    private void ConfigurarColumnasGrid()
    {
        _gridProductos.Columns.Clear();
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Visible = false });
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sku", HeaderText = "SKU", FillWeight = 85 });
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nombre", HeaderText = "Producto", FillWeight = 150 });
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Categoria", HeaderText = "Categoría", FillWeight = 95 });
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tipo", HeaderText = "Rastreo", FillWeight = 65 });
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrecioCosto", HeaderText = "Costo", FillWeight = 75 });
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrecioVenta", HeaderText = "Venta", FillWeight = 75 });
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { Name = "StockActual", HeaderText = "Stock", FillWeight = 55 });
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { Name = "CantidadMinima", HeaderText = "Mín.", FillWeight = 45 });
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { Name = "CodigoBarras", HeaderText = "Cód. Barras", FillWeight = 90 });
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", HeaderText = "Estado", FillWeight = 60 });
    }

    private async Task CargarComboCategoriasAsync()
    {
        var cats = await _categoriaService.ObtenerCategoriasAsync(soloActivas: true);
        var items = new List<CategoriaComboItem>
        {
            new(0, "-- Todas las categorías --")
        };
        items.AddRange(cats.Select(c => new CategoriaComboItem(c.Id, c.Nombre)));

        _cbCategorias.DisplayMember = nameof(CategoriaComboItem.Nombre);
        _cbCategorias.ValueMember = nameof(CategoriaComboItem.Id);
        _cbCategorias.DataSource = items;
    }

    private async Task RecargarProductosAsync()
    {
        int? catId = null;
        if (_cbCategorias.SelectedValue is int cid && cid > 0)
        {
            catId = cid;
        }

        _listaProductos = await _productoService.ObtenerProductosAsync(
            soloActivos: _chkSoloActivos.Checked,
            categoriaId: catId
        );

        ActualizarResumen();
        FiltrarGrid();
    }

    private void ActualizarResumen()
    {
        var total = _listaProductos.Count;
        var activos = _listaProductos.Count(p => p.Activo);
        var bajoStock = _listaProductos.Count(p => p.Activo && p.StockActual <= p.CantidadMinima);

        _lblResumen.Text = $"Total en catálogo: {total} | Activos: {activos} | ⚠️ Productos con alerta de stock bajo: {bajoStock}";
        if (bajoStock > 0)
        {
            _lblResumen.ForeColor = UITheme.Danger;
        }
        else
        {
            _lblResumen.ForeColor = UITheme.TextMuted;
        }
    }

    private void FiltrarGrid()
    {
        var q = _txtBuscar.Text.Trim().ToLower();
        var soloBajo = _chkBajoStock.Checked;

        var filtrados = _listaProductos.Where(p =>
            (string.IsNullOrEmpty(q) ||
             p.Nombre.ToLower().Contains(q) ||
             p.Sku.ToLower().Contains(q) ||
             (p.CodigoBarras != null && p.CodigoBarras.ToLower().Contains(q)) ||
             (p.Categoria != null && p.Categoria.Nombre.ToLower().Contains(q))) &&
            (!soloBajo || (p.Activo && p.StockActual <= p.CantidadMinima))
        ).ToList();

        _gridProductos.Rows.Clear();
        foreach (var p in filtrados)
        {
            var rowIndex = _gridProductos.Rows.Add(
                p.Id,
                p.Sku,
                p.Nombre,
                p.Categoria?.Nombre ?? "-",
                p.RequiereSerie ? "📱 Celular/IMEI" : "📦 Cantidad",
                AppCulture.FormatearMoneda(p.PrecioCosto),
                AppCulture.FormatearMoneda(p.PrecioVenta),
                p.StockActual,
                p.CantidadMinima,
                p.CodigoBarras ?? "-",
                p.Activo ? "Activo" : "Inactivo"
            );

            var row = _gridProductos.Rows[rowIndex];
            if (!p.Activo)
            {
                row.DefaultCellStyle.ForeColor = UITheme.TextMuted;
            }
            else if (p.RequiereSerie)
            {
                row.Cells["Tipo"].Style.ForeColor = Color.FromArgb(26, 35, 126);
                row.Cells["Tipo"].Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            }

            // Resaltar alerta de stock bajo
            if (p.Activo && p.StockActual <= p.CantidadMinima)
            {
                row.Cells["StockActual"].Style.ForeColor = UITheme.Danger;
                row.Cells["StockActual"].Style.Font = UITheme.SectionFont;
            }
        }
    }

    private void AbrirGestionImeis()
    {
        var prod = ObtenerProductoSeleccionado();
        if (prod == null)
        {
            MessageBox.Show("Por favor seleccione un producto del catálogo.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!prod.RequiereSerie)
        {
            var r = MessageBox.Show($"El producto '{prod.Nombre}' no está marcado como celular con serie/IMEI (se controla por cantidad normal).\n¿Desea abrir el control de unidades de todos modos?", "Aviso", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r != DialogResult.Yes) return;
        }

        using var modal = new GestionarImeisModalForm(_productoService, prod);
        modal.ShowDialog(this);
        _ = RecargarProductosAsync();
    }

    private Producto? ObtenerProductoSeleccionado()
    {
        if (_gridProductos.SelectedRows.Count == 0) return null;
        var id = (int)_gridProductos.SelectedRows[0].Cells["Id"].Value;
        return _listaProductos.FirstOrDefault(p => p.Id == id);
    }

    private async Task AbrirCrearProductoAsync()
    {
        var modal = new ProductoModalForm(_productoService, _categoriaService, _ean13Service);
        if (modal.ShowDialog(this) == DialogResult.OK)
        {
            await RecargarProductosAsync();
        }
    }

    private async Task AbrirEditarProductoAsync()
    {
        var prod = ObtenerProductoSeleccionado();
        if (prod == null)
        {
            MessageBox.Show("Por favor seleccione un producto para editar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var modal = new ProductoModalForm(_productoService, _categoriaService, _ean13Service, prod.Id);
        if (modal.ShowDialog(this) == DialogResult.OK)
        {
            await RecargarProductosAsync();
        }
    }

    private void AbrirImprimirEtiqueta()
    {
        var prod = ObtenerProductoSeleccionado();
        if (prod == null)
        {
            MessageBox.Show("Por favor seleccione un producto del catálogo para generar su etiqueta.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (string.IsNullOrWhiteSpace(prod.CodigoBarras))
        {
            MessageBox.Show($"El producto '{prod.Nombre}' no tiene un código de barras asignado.\nPuede editarlo y presionar '⚡ EAN-13' para generarle uno.", "Sin Código de Barras", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var modal = new ImprimirEtiquetaModalForm(prod);
            modal.ShowDialog(this);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al generar etiqueta: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ToggleEstadoProductoAsync()
    {
        var prod = ObtenerProductoSeleccionado();
        if (prod == null)
        {
            MessageBox.Show("Por favor seleccione un producto.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var nuevo = !prod.Activo;
        var accion = nuevo ? "activar" : "desactivar";

        var conf = MessageBox.Show(
            $"¿Está seguro de que desea {accion} el producto '{prod.Nombre}' ({prod.Sku})?\n(Se preserva la trazabilidad histórica).",
            "Confirmación",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        );

        if (conf == DialogResult.Yes)
        {
            try
            {
                var ok = await _productoService.CambiarEstadoActivoAsync(prod.Id, nuevo);
                if (ok)
                {
                    await RecargarProductosAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void AbrirVerificadorPrecio()
    {
        using var modal = new VerificadorPrecioModalForm(_productoService, _categoriaService, _sesionActual);
        modal.ShowDialog(this);
        _ = RecargarProductosAsync();
    }

    private async Task AbrirCategoriasFormAsync()
    {
        var formCats = new CategoriasForm(_categoriaService, _sesionActual);
        formCats.ShowDialog(this);
        await CargarComboCategoriasAsync();
        await RecargarProductosAsync();
    }

    private record CategoriaComboItem(int Id, string Nombre);
}
