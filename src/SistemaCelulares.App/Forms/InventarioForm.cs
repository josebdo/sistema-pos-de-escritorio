using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Constants;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class InventarioForm : Form
{
    private readonly IProductoService _productoService;
    private readonly ICategoriaService _categoriaService;
    private readonly ICompraService _compraService;
    private readonly IProveedorService _proveedorService;
    private readonly IAlertaStockService _alertaService;
    private readonly SesionUsuario _sesionActual;
    private readonly bool _soloLectura;

    private DataGridView _gridInventario = null!;
    private TextBox _txtBuscar = null!;
    private ComboBox _cbCategorias = null!;
    private ComboBox _cbTipoFiltro = null!;
    private Label _lblResumen = null!;

    private Label _lblTotalStock = null!;
    private Label _lblTotalCelulares = null!;
    private Label _lblTotalArticulos = null!;
    private Label _lblAlertasBajoStock = null!;
    private Label _lblValorInventario = null!;

    private Button _btnEntradaStock = null!;
    private Button _btnHistCompras = null!;
    private Button _btnAlertas = null!;
    private Button _btnGestionarImeis = null!;
    private Button _btnRefrescar = null!;

    private List<Producto> _listaProductos = new();

    public InventarioForm(
        IProductoService productoService,
        ICategoriaService categoriaService,
        ICompraService compraService,
        IProveedorService proveedorService,
        IAlertaStockService alertaService,
        SesionUsuario sesionActual,
        bool soloLectura = false)
    {
        _productoService = productoService;
        _categoriaService = categoriaService;
        _compraService = compraService;
        _proveedorService = proveedorService;
        _alertaService = alertaService;
        _sesionActual = sesionActual;
        _soloLectura = soloLectura;

        InitializeCustomComponents();
        Load += async (s, e) =>
        {
            await CargarComboCategoriasAsync();
            await RecargarInventarioAsync();
        };
    }

    private void InitializeCustomComponents()
    {
        Text = "Gestión de Inventario y Stock";
        Size = new Size(1180, 720);
        BackColor = UITheme.AppBg;
        Font = UITheme.BodyFont;

        var panelPrincipal = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };
        Controls.Add(panelPrincipal);

        // Header Superior
        var header = new Panel { Dock = DockStyle.Top, Height = 56 };
        var lblTitulo = new Label
        {
            Text = "Control de Inventario y Existencias",
            Font = UITheme.TitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(0, 4),
            AutoSize = true
        };
        header.Controls.Add(lblTitulo);

        var lblSub = new Label
        {
            Text = "Entradas por compras a suplidor, existencias por IMEI / series y alertas de stock",
            Font = UITheme.SmallFont,
            ForeColor = UITheme.TextMuted,
            Location = new Point(0, 32),
            AutoSize = true
        };
        header.Controls.Add(lblSub);

        // Barra de Herramientas / Botones de Acción de Inventario
        var flowBotones = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 0, 0, 6)
        };

        bool puedeGestionar = !_soloLectura && (_sesionActual.EsSuperAdmin || _sesionActual.RolNombre == Rol.Admin || _sesionActual.TienePermiso(Permisos.ComprasRegistrar));

        if (puedeGestionar)
        {
            _btnEntradaStock = new Button { Text = "📥 Entrada por Compra", Size = new Size(185, 34), Margin = new Padding(0, 0, 8, 6) };
            UITheme.AplicarBotonPrimario(_btnEntradaStock);
            _btnEntradaStock.Click += async (s, e) =>
            {
                using var dlg = new RegistrarCompraForm(_compraService, _proveedorService, _productoService, _sesionActual);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    await RecargarInventarioAsync();
                }
            };
            flowBotones.Controls.Add(_btnEntradaStock);

            _btnHistCompras = new Button { Text = "📜 Historial Compras", Size = new Size(155, 34), Margin = new Padding(0, 0, 8, 6) };
            UITheme.AplicarBotonSecundario(_btnHistCompras);
            _btnHistCompras.Click += (s, e) =>
            {
                using var dlg = new HistorialComprasForm(_compraService, _proveedorService, _productoService, _sesionActual);
                dlg.ShowDialog(this);
            };
            flowBotones.Controls.Add(_btnHistCompras);

            var btnEditar = new Button { Text = "✏️ Editar Producto / Precios", Size = new Size(185, 34), Margin = new Padding(0, 0, 8, 6) };
            UITheme.AplicarBotonSecundario(btnEditar);
            btnEditar.Click += async (s, e) => await AbrirEditarProductoAsync();
            flowBotones.Controls.Add(btnEditar);
        }

        _btnAlertas = new Button { Text = "⚠️ Alertas de Stock", Size = new Size(155, 34), Margin = new Padding(0, 0, 8, 6) };
        UITheme.AplicarBotonSecundario(_btnAlertas);
        _btnAlertas.Click += (s, e) =>
        {
            using var dlg = new AlertasStockForm(_alertaService, _compraService, _proveedorService, _productoService, _sesionActual);
            dlg.ShowDialog(this);
        };
        flowBotones.Controls.Add(_btnAlertas);

        _btnGestionarImeis = new Button { Text = "📱 Ver IMEIs / Series", Size = new Size(150, 34), Margin = new Padding(0, 0, 8, 6) };
        UITheme.AplicarBotonSecundario(_btnGestionarImeis);
        _btnGestionarImeis.Click += async (s, e) => await AbrirGestionImeisAsync();
        flowBotones.Controls.Add(_btnGestionarImeis);

        _btnRefrescar = new Button { Text = "🔄 Actualizar", Size = new Size(110, 34), Margin = new Padding(0, 0, 8, 6) };
        UITheme.AplicarBotonSecundario(_btnRefrescar);
        _btnRefrescar.Click += async (s, e) => await RecargarInventarioAsync();
        flowBotones.Controls.Add(_btnRefrescar);

        // Tarjetas de Métricas de Inventario
        var pnlCards = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 78,
            ColumnCount = 5,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 8)
        };
        pnlCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
        pnlCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
        pnlCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
        pnlCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
        pnlCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));

        var cardStock = CrearCardMetrica("Total Unidades Stock", out _lblTotalStock, UITheme.Primary);
        var cardCels = CrearCardMetrica("Celulares (IMEI)", out _lblTotalCelulares, UITheme.Pro);
        var cardArts = CrearCardMetrica("Accesorios / Cantidad", out _lblTotalArticulos, UITheme.TextPrimary);
        var cardBajo = CrearCardMetrica("Stock Bajo / Agotado", out _lblAlertasBajoStock, UITheme.Danger);
        var cardValor = CrearCardMetrica("Valor Total (Costo RD$)", out _lblValorInventario, UITheme.Success);

        pnlCards.Controls.Add(cardStock, 0, 0);
        pnlCards.Controls.Add(cardCels, 1, 0);
        pnlCards.Controls.Add(cardArts, 2, 0);
        pnlCards.Controls.Add(cardBajo, 3, 0);
        pnlCards.Controls.Add(cardValor, 4, 0);

        // Barra de Búsqueda y Filtros
        var pnlFiltros = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 4, 0, 6) };
        var flowFiltros = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };

        var lblBuscar = new Label { Text = "🔍 Buscar:", AutoSize = true, Font = UITheme.SectionFont, Margin = new Padding(0, 6, 6, 4) };
        _txtBuscar = new TextBox { Width = 180, Font = UITheme.BodyFont, PlaceholderText = "Nombre, SKU, Código...", Margin = new Padding(0, 2, 8, 4) };
        _txtBuscar.TextChanged += (s, e) => FiltrarGrilla();

        var lblCat = new Label { Text = "Categoría:", AutoSize = true, Font = UITheme.SectionFont, Margin = new Padding(2, 6, 6, 4) };
        _cbCategorias = new ComboBox { Width = 160, DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.BodyFont, Margin = new Padding(0, 2, 8, 4) };
        _cbCategorias.SelectedIndexChanged += (s, e) => FiltrarGrilla();

        var lblTipo = new Label { Text = "Filtro:", AutoSize = true, Font = UITheme.SectionFont, Margin = new Padding(2, 6, 6, 4) };
        _cbTipoFiltro = new ComboBox { Width = 175, DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.BodyFont, Margin = new Padding(0, 2, 8, 4) };
        _cbTipoFiltro.Items.AddRange(new object[] { "Todos los productos", "Solo Celulares (IMEI)", "Solo Artículos por Cantidad", "⚠️ Solo Bajo Stock / Agotados" });
        _cbTipoFiltro.SelectedIndex = 0;
        _cbTipoFiltro.SelectedIndexChanged += (s, e) => FiltrarGrilla();

        flowFiltros.Controls.Add(lblBuscar);
        flowFiltros.Controls.Add(_txtBuscar);
        flowFiltros.Controls.Add(lblCat);
        flowFiltros.Controls.Add(_cbCategorias);
        flowFiltros.Controls.Add(lblTipo);
        flowFiltros.Controls.Add(_cbTipoFiltro);

        pnlFiltros.Controls.Add(flowFiltros);

        // DataGridView de Inventario
        var pnlGridContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(1) };
        _gridInventario = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        UITheme.EstilizarDataGridView(_gridInventario);
        ConfigurarColumnasGrid();
        _gridInventario.DoubleClick += async (s, e) => await AbrirEditarProductoAsync();

        pnlGridContainer.Controls.Add(_gridInventario);

        // Footer Resumen
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 32 };
        _lblResumen = new Label { Text = "Cargando inventario...", Font = UITheme.SmallFont, ForeColor = UITheme.TextMuted, Location = new Point(0, 8), AutoSize = true };
        footer.Controls.Add(_lblResumen);

        // Agregar al panelPrincipal en orden de acoplamiento estricto
        panelPrincipal.Controls.Add(pnlGridContainer); // Fill
        panelPrincipal.Controls.Add(footer);           // Bottom
        panelPrincipal.Controls.Add(pnlFiltros);        // Top 4
        panelPrincipal.Controls.Add(pnlCards);          // Top 3
        panelPrincipal.Controls.Add(flowBotones);       // Top 2
        panelPrincipal.Controls.Add(header);            // Top 1
    }

    private Panel CrearCardMetrica(string titulo, out Label lblValor, Color colorValor)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(10, 8, 10, 8),
            Margin = new Padding(0, 0, 8, 0)
        };
        card.Paint += (s, e) => e.Graphics.DrawRectangle(new Pen(UITheme.Border), 0, 0, card.Width - 1, card.Height - 1);

        var lblTit = new Label
        {
            Text = titulo,
            Font = new Font("Segoe UI", 8F, FontStyle.Regular),
            ForeColor = UITheme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 16
        };
        lblValor = new Label
        {
            Text = "0",
            Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
            ForeColor = colorValor,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        card.Controls.Add(lblValor);
        card.Controls.Add(lblTit);
        return card;
    }

    private void ConfigurarColumnasGrid()
    {
        _gridInventario.Columns.Clear();
        _gridInventario.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Visible = false });
        _gridInventario.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sku", HeaderText = "SKU", FillWeight = 65 });
        _gridInventario.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nombre", HeaderText = "Producto / Modelo", FillWeight = 160 });
        _gridInventario.Columns.Add(new DataGridViewTextBoxColumn { Name = "Categoria", HeaderText = "Categoría", FillWeight = 85 });
        _gridInventario.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tipo", HeaderText = "Tipo Control", FillWeight = 75 });
        _gridInventario.Columns.Add(new DataGridViewTextBoxColumn { Name = "StockActual", HeaderText = "Stock Actual", FillWeight = 65 });
        _gridInventario.Columns.Add(new DataGridViewTextBoxColumn { Name = "StockMinimo", HeaderText = "Mínimo", FillWeight = 55 });
        _gridInventario.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrecioCosto", HeaderText = "Costo (RD$)", FillWeight = 75 });
        _gridInventario.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrecioVenta", HeaderText = "Venta (RD$)", FillWeight = 75 });
        _gridInventario.Columns.Add(new DataGridViewTextBoxColumn { Name = "ValorTotal", HeaderText = "Valor Stock (RD$)", FillWeight = 85 });
        _gridInventario.Columns.Add(new DataGridViewTextBoxColumn { Name = "EstadoStock", HeaderText = "Estado", FillWeight = 75 });
    }

    private async Task CargarComboCategoriasAsync()
    {
        try
        {
            var categorias = await _categoriaService.ObtenerCategoriasAsync(soloActivas: true);
            _cbCategorias.Items.Clear();
            _cbCategorias.Items.Add("Todas las categorías");
            foreach (var cat in categorias)
            {
                _cbCategorias.Items.Add(cat.Nombre);
            }
            _cbCategorias.SelectedIndex = 0;
        }
        catch
        {
            // Ignorar errores de combo
        }
    }

    private async Task RecargarInventarioAsync()
    {
        try
        {
            _listaProductos = await _productoService.ObtenerProductosAsync(soloActivos: true);
            FiltrarGrilla();
            ActualizarMetricas();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar inventario: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ActualizarMetricas()
    {
        int totalUnits = _listaProductos.Sum(p => p.StockActual);
        int totalCels = _listaProductos.Where(p => p.RequiereSerie).Sum(p => p.StockActual);
        int totalArts = _listaProductos.Where(p => !p.RequiereSerie).Sum(p => p.StockActual);
        int bajoStock = _listaProductos.Count(p => p.StockActual <= p.CantidadMinima);
        decimal valorTotalCosto = _listaProductos.Sum(p => p.StockActual * p.PrecioCosto);

        _lblTotalStock.Text = $"{totalUnits:N0}";
        _lblTotalCelulares.Text = $"{totalCels:N0}";
        _lblTotalArticulos.Text = $"{totalArts:N0}";
        _lblAlertasBajoStock.Text = $"{bajoStock:N0}";
        _lblValorInventario.Text = $"RD${valorTotalCosto:N2}";
    }

    private void FiltrarGrilla()
    {
        var query = _listaProductos.AsEnumerable();

        string buscar = _txtBuscar.Text.Trim().ToLower();
        if (!string.IsNullOrEmpty(buscar))
        {
            query = query.Where(p =>
                p.Nombre.ToLower().Contains(buscar) ||
                p.Sku.ToLower().Contains(buscar) ||
                (p.CodigoBarras != null && p.CodigoBarras.ToLower().Contains(buscar)));
        }

        if (_cbCategorias.SelectedIndex > 0 && _cbCategorias.SelectedItem != null)
        {
            string catSel = _cbCategorias.SelectedItem.ToString()!;
            query = query.Where(p => p.Categoria != null && p.Categoria.Nombre == catSel);
        }

        if (_cbTipoFiltro.SelectedIndex == 1) // Solo Celulares (IMEI)
        {
            query = query.Where(p => p.RequiereSerie);
        }
        else if (_cbTipoFiltro.SelectedIndex == 2) // Solo Artículos por Cantidad
        {
            query = query.Where(p => !p.RequiereSerie);
        }
        else if (_cbTipoFiltro.SelectedIndex == 3) // Solo Bajo Stock / Agotados
        {
            query = query.Where(p => p.StockActual <= p.CantidadMinima);
        }

        var listaFiltrada = query.OrderBy(p => p.Nombre).ToList();

        _gridInventario.Rows.Clear();
        foreach (var p in listaFiltrada)
        {
            string tipoStr = p.RequiereSerie ? "📱 Celular (IMEI)" : "📦 Cantidad";
            string estadoStr = p.StockActual <= 0 ? "❌ Agotado" : (p.StockActual <= p.CantidadMinima ? "⚠️ Stock Bajo" : "✔ Normal");
            decimal valorStock = p.StockActual * p.PrecioCosto;

            int rowIndex = _gridInventario.Rows.Add(
                p.Id,
                p.Sku,
                p.Nombre,
                p.Categoria?.Nombre ?? "Sin categoría",
                tipoStr,
                p.StockActual,
                p.CantidadMinima,
                p.PrecioCosto.ToString("N2"),
                p.PrecioVenta.ToString("N2"),
                valorStock.ToString("N2"),
                estadoStr
            );

            // Resaltar filas con stock crítico
            var row = _gridInventario.Rows[rowIndex];
            if (p.StockActual <= 0)
            {
                row.Cells["StockActual"].Style.ForeColor = UITheme.Danger;
                row.Cells["EstadoStock"].Style.ForeColor = UITheme.Danger;
                row.Cells["EstadoStock"].Style.Font = UITheme.BodyBoldFont;
            }
            else if (p.StockActual <= p.CantidadMinima)
            {
                row.Cells["StockActual"].Style.ForeColor = UITheme.Warning;
                row.Cells["EstadoStock"].Style.ForeColor = UITheme.Warning;
                row.Cells["EstadoStock"].Style.Font = UITheme.BodyBoldFont;
            }
            else
            {
                row.Cells["StockActual"].Style.ForeColor = UITheme.Success;
                row.Cells["EstadoStock"].Style.ForeColor = UITheme.Success;
            }
        }

        _lblResumen.Text = $"Mostrando {listaFiltrada.Count} de {_listaProductos.Count} productos registrados en inventario.";
    }

    private async Task AbrirGestionImeisAsync()
    {
        if (_gridInventario.SelectedRows.Count == 0)
        {
            MessageBox.Show("Seleccione un producto en la lista para ver o gestionar sus IMEIs.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        int id = (int)_gridInventario.SelectedRows[0].Cells["Id"].Value;
        var prod = _listaProductos.FirstOrDefault(p => p.Id == id);
        if (prod == null) return;

        if (!prod.RequiereSerie)
        {
            MessageBox.Show($"El producto '{prod.Nombre}' es un artículo controlado por cantidad, no requiere trazabilidad por IMEI.", "Control por Cantidad", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var modal = new GestionarImeisModalForm(_productoService, prod);
        modal.ShowDialog(this);
        await RecargarInventarioAsync();
    }

    private async Task AbrirEditarProductoAsync()
    {
        if (_gridInventario.SelectedRows.Count == 0)
        {
            MessageBox.Show("Seleccione un producto en la lista para editar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        int id = (int)_gridInventario.SelectedRows[0].Cells["Id"].Value;
        var modal = new ProductoModalForm(_productoService, _categoriaService, productoId: id);
        if (modal.ShowDialog(this) == DialogResult.OK)
        {
            await RecargarInventarioAsync();
        }
    }
}
