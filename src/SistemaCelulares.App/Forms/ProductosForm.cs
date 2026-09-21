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

    private List<Producto> _listaProductos = new();

    public ProductosForm(
        IProductoService productoService,
        ICategoriaService categoriaService,
        SesionUsuario sesionActual)
    {
        _productoService = productoService;
        _categoriaService = categoriaService;
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
        Text = "Catálogo de Productos e Inventario";
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
            Text = "Catálogo de Celulares y Accesorios",
            Font = UITheme.TitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(0, 5),
            AutoSize = true
        };
        header.Controls.Add(lblTitulo);

        _lblResumen = new Label
        {
            Text = "Cargando inventario...",
            Font = UITheme.SmallFont,
            ForeColor = UITheme.TextMuted,
            Location = new Point(0, 35),
            AutoSize = true
        };
        header.Controls.Add(_lblResumen);
        panelPrincipal.Controls.Add(header);

        // Toolbar
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 55 };

        var lblBuscar = new Label { Text = "🔍 Buscar / Escanear:", Location = new Point(0, 16), AutoSize = true, Font = UITheme.SectionFont };
        toolbar.Controls.Add(lblBuscar);

        _txtBuscar = new TextBox { Location = new Point(135, 13), Size = new Size(180, 28), Font = new Font("Segoe UI", 9.5F), PlaceholderText = "Escanear o buscar..." };
        _txtBuscar.TextChanged += (s, e) => FiltrarGrid();
        BarcodeScannerHelper.ConfigurarParaEscaneo(_txtBuscar, async (codigo) =>
        {
            _txtBuscar.Text = codigo;
            FiltrarGrid();
            await Task.CompletedTask;
        });
        toolbar.Controls.Add(_txtBuscar);

        var lblCat = new Label { Text = "Categoría:", Location = new Point(325, 16), AutoSize = true, Font = UITheme.BodyFont };
        toolbar.Controls.Add(lblCat);

        _cbCategorias = new ComboBox { Location = new Point(390, 13), Size = new Size(140, 28), DropDownStyle = ComboBoxStyle.DropDownList };
        _cbCategorias.SelectedIndexChanged += async (s, e) => await RecargarProductosAsync();
        toolbar.Controls.Add(_cbCategorias);

        _chkBajoStock = new CheckBox { Text = "⚠️ Bajo stock", Location = new Point(535, 15), Checked = false, AutoSize = true, Font = UITheme.SectionFont, ForeColor = UITheme.Danger };
        _chkBajoStock.CheckedChanged += (s, e) => FiltrarGrid();
        toolbar.Controls.Add(_chkBajoStock);

        _chkSoloActivos = new CheckBox { Text = "Activos", Location = new Point(645, 16), Checked = true, AutoSize = true };
        _chkSoloActivos.CheckedChanged += async (s, e) => await RecargarProductosAsync();
        toolbar.Controls.Add(_chkSoloActivos);

        // Panel Botones Derecha
        var panelBotones = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            Width = 530,
            Height = 50
        };

        _btnNuevo = new Button { Text = "➕ Nuevo", Size = new Size(95, 38) };
        UITheme.AplicarBotonPrimario(_btnNuevo);
        _btnNuevo.Click += async (s, e) => await AbrirCrearProductoAsync();
        _btnNuevo.Visible = _sesionActual.TienePermiso(Permisos.ProductosCrear);
        panelBotones.Controls.Add(_btnNuevo);

        var btnVerificador = new Button { Text = "📸 Lector", Size = new Size(95, 38) };
        UITheme.AplicarBotonSecundario(btnVerificador);
        btnVerificador.Click += (s, e) => AbrirVerificadorPrecio();
        panelBotones.Controls.Add(btnVerificador);

        _btnEditar = new Button { Text = "✏️ Editar", Size = new Size(85, 38) };
        UITheme.AplicarBotonSecundario(_btnEditar);
        _btnEditar.Click += async (s, e) => await AbrirEditarProductoAsync();
        _btnEditar.Visible = _sesionActual.TienePermiso(Permisos.ProductosEditar);
        panelBotones.Controls.Add(_btnEditar);

        _btnToggleEstado = new Button { Text = "🔄 Act/Desc", Size = new Size(105, 38) };
        UITheme.AplicarBotonSecundario(_btnToggleEstado);
        _btnToggleEstado.Click += async (s, e) => await ToggleEstadoProductoAsync();
        _btnToggleEstado.Visible = _sesionActual.TienePermiso(Permisos.ProductosDesactivar);
        panelBotones.Controls.Add(_btnToggleEstado);

        _btnCategorias = new Button { Text = "📁 Categorías", Size = new Size(110, 38) };
        UITheme.AplicarBotonSecundario(_btnCategorias);
        _btnCategorias.Click += async (s, e) => await AbrirCategoriasFormAsync();
        _btnCategorias.Visible = _sesionActual.TienePermiso(Permisos.CategoriasGestionar);
        panelBotones.Controls.Add(_btnCategorias);

        toolbar.Controls.Add(panelBotones);
        panelPrincipal.Controls.Add(toolbar);

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

        panelPrincipal.Controls.Add(panelGrid);
    }

    private void ConfigurarColumnasGrid()
    {
        _gridProductos.Columns.Clear();
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Visible = false });
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sku", HeaderText = "SKU", FillWeight = 85 });
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nombre", HeaderText = "Producto", FillWeight = 160 });
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Categoria", HeaderText = "Categoría", FillWeight = 100 });
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrecioCosto", HeaderText = "Costo", FillWeight = 80 });
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrecioVenta", HeaderText = "Venta", FillWeight = 80 });
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { Name = "StockActual", HeaderText = "Stock", FillWeight = 60 });
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { Name = "CantidadMinima", HeaderText = "Mín.", FillWeight = 50 });
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { Name = "CodigoBarras", HeaderText = "Cód. Barras", FillWeight = 95 });
        _gridProductos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", HeaderText = "Estado", FillWeight = 65 });
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
            soloActivos: !_chkSoloActivos.Checked,
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
             p.Categoria.Nombre.ToLower().Contains(q)) &&
            (!soloBajo || (p.Activo && p.StockActual <= p.CantidadMinima))
        ).ToList();

        _gridProductos.Rows.Clear();
        foreach (var p in filtrados)
        {
            var rowIndex = _gridProductos.Rows.Add(
                p.Id,
                p.Sku,
                p.Nombre,
                p.Categoria.Nombre,
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

            // Resaltar alerta de stock bajo
            if (p.Activo && p.StockActual <= p.CantidadMinima)
            {
                row.Cells["StockActual"].Style.ForeColor = UITheme.Danger;
                row.Cells["StockActual"].Style.Font = UITheme.SectionFont;
            }
        }
    }

    private Producto? ObtenerProductoSeleccionado()
    {
        if (_gridProductos.SelectedRows.Count == 0) return null;
        var id = (int)_gridProductos.SelectedRows[0].Cells["Id"].Value;
        return _listaProductos.FirstOrDefault(p => p.Id == id);
    }

    private async Task AbrirCrearProductoAsync()
    {
        var modal = new ProductoModalForm(_productoService, _categoriaService);
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

        var modal = new ProductoModalForm(_productoService, _categoriaService, prod.Id);
        if (modal.ShowDialog(this) == DialogResult.OK)
        {
            await RecargarProductosAsync();
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
