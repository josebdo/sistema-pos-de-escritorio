using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class AlertasStockForm : Form
{
    private readonly IAlertaStockService _alertaService;
    private readonly ICompraService _compraService;
    private readonly IProveedorService _proveedorService;
    private readonly IProductoService _productoService;
    private readonly SesionUsuario _sesionActual;

    private Panel _cardAgotados = null!;
    private Panel _cardCriticos = null!;
    private Panel _cardInversion = null!;
    private DataGridView _gridAlertas = null!;
    private Button _btnComprarSeleccionado = null!;
    private Button _btnRefrescar = null!;
    private List<AlertaStockDto> _listaAlertas = new();

    public AlertasStockForm(
        IAlertaStockService alertaService,
        ICompraService compraService,
        IProveedorService proveedorService,
        IProductoService productoService,
        SesionUsuario sesionActual)
    {
        _alertaService = alertaService;
        _compraService = compraService;
        _proveedorService = proveedorService;
        _productoService = productoService;
        _sesionActual = sesionActual;

        InitializeCustomComponents();
        Load += async (s, e) => await RecargarAlertasAsync();
    }

    private void InitializeCustomComponents()
    {
        Text = "Panel de Control - Alertas de Stock Mínimo y Reabastecimiento";
        Size = new Size(1100, 680);
        BackColor = UITheme.AppBg;
        Font = UITheme.BodyFont;

        var panelPrincipal = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
        Controls.Add(panelPrincipal);

        // Header
        var header = new Panel { Dock = DockStyle.Top, Height = 55 };
        var lblTitulo = new Label
        {
            Text = "Alertas de Inventario y Reabastecimiento Crítico",
            Font = UITheme.TitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(0, 5),
            AutoSize = true
        };
        header.Controls.Add(lblTitulo);

        var lblSub = new Label
        {
            Text = "Productos en o por debajo del umbral mínimo de stock requeridos para operación continua",
            Font = UITheme.SmallFont,
            ForeColor = UITheme.TextMuted,
            Location = new Point(0, 32),
            AutoSize = true
        };
        header.Controls.Add(lblSub);

        // KPI Summary Cards (TableLayoutPanel with 3 responsive columns)
        var panelKpis = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 85,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(0),
            Margin = new Padding(0, 0, 0, 10)
        };
        panelKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        panelKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        panelKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));

        _cardAgotados = CrearCardKpi("🚫 Agotados (Stock 0)", "0 productos", UITheme.Danger);
        _cardAgotados.Margin = new Padding(0, 0, 10, 0);
        panelKpis.Controls.Add(_cardAgotados, 0, 0);

        _cardCriticos = CrearCardKpi("⚠️ Stock Bajo / Crítico", "0 productos", UITheme.Warning);
        _cardCriticos.Margin = new Padding(5, 0, 10, 0);
        panelKpis.Controls.Add(_cardCriticos, 1, 0);

        _cardInversion = CrearCardKpi("💰 Inversión Estimada Reposición", "RD$0.00", UITheme.Primary);
        _cardInversion.Margin = new Padding(5, 0, 0, 0);
        panelKpis.Controls.Add(_cardInversion, 2, 0);

        // Toolbar
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 48 };

        _btnRefrescar = new Button { Text = "🔄 Actualizar Alertas", Location = new Point(0, 5), Size = new Size(150, 36) };
        UITheme.AplicarBotonSecundario(_btnRefrescar);
        _btnRefrescar.Click += async (s, e) => await RecargarAlertasAsync();
        toolbar.Controls.Add(_btnRefrescar);

        var panelBotones = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.RightToLeft, Width = 300, Height = 45 };

        _btnComprarSeleccionado = new Button { Text = "🛒 Comprar Producto", Size = new Size(180, 36) };
        UITheme.AplicarBotonPrimario(_btnComprarSeleccionado);
        _btnComprarSeleccionado.Click += async (s, e) => await AbrirCompraProductoSeleccionadoAsync();
        panelBotones.Controls.Add(_btnComprarSeleccionado);

        toolbar.Controls.Add(panelBotones);

        // DataGridView
        var panelGrid = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(1) };
        _gridAlertas = new DataGridView { Dock = DockStyle.Fill };
        UITheme.EstilizarDataGridView(_gridAlertas);
        ConfigurarColumnas();
        panelGrid.Controls.Add(_gridAlertas);

        // Add to panel in reverse docking order so Header is at the top, then KPIs, then Toolbar, then Grid
        panelPrincipal.Controls.Add(panelGrid);
        panelPrincipal.Controls.Add(toolbar);
        panelPrincipal.Controls.Add(panelKpis);
        panelPrincipal.Controls.Add(header);
    }

    private Panel CrearCardKpi(string titulo, string valorInicial, Color color)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(15)
        };

        var lblTit = new Label { Text = titulo, Font = UITheme.SmallFont, ForeColor = UITheme.TextMuted, Location = new Point(15, 12), AutoSize = true };
        card.Controls.Add(lblTit);

        var lblVal = new Label { Text = valorInicial, Font = UITheme.SubtitleFont, ForeColor = color, Location = new Point(15, 38), AutoSize = true, Name = "Valor" };
        card.Controls.Add(lblVal);

        return card;
    }

    private void ConfigurarColumnas()
    {
        _gridAlertas.Columns.Clear();
        _gridAlertas.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductoId", HeaderText = "ID", Visible = false });
        _gridAlertas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Criticidad", HeaderText = "Nivel Alerta", FillWeight = 95 });
        _gridAlertas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sku", HeaderText = "SKU", FillWeight = 85 });
        _gridAlertas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nombre", HeaderText = "Producto Crítico", FillWeight = 180 });
        _gridAlertas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Categoria", HeaderText = "Categoría", FillWeight = 110 });
        _gridAlertas.Columns.Add(new DataGridViewTextBoxColumn { Name = "StockActual", HeaderText = "Stock Actual", FillWeight = 75 });
        _gridAlertas.Columns.Add(new DataGridViewTextBoxColumn { Name = "CantidadMinima", HeaderText = "Stock Mínimo", FillWeight = 75 });
        _gridAlertas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Faltantes", HeaderText = "Faltante", FillWeight = 70 });
        _gridAlertas.Columns.Add(new DataGridViewTextBoxColumn { Name = "CostoUnitario", HeaderText = "Costo Unit.", FillWeight = 90 });
        _gridAlertas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Inversion", HeaderText = "Inversión Reposición", FillWeight = 110 });
    }

    private async Task RecargarAlertasAsync()
    {
        _listaAlertas = await _alertaService.ObtenerAlertasStockAsync();

        // Actualizar KPIs
        var agotados = _listaAlertas.Count(a => a.Criticidad == NivelCriticidadStock.Agotado);
        var criticos = _listaAlertas.Count(a => a.Criticidad == NivelCriticidadStock.Critico || a.Criticidad == NivelCriticidadStock.Bajo);
        var inversionTotal = _listaAlertas.Sum(a => a.InversionReposicion);

        _cardAgotados.Controls["Valor"]!.Text = $"{agotados} productos";
        _cardCriticos.Controls["Valor"]!.Text = $"{criticos} productos";
        _cardInversion.Controls["Valor"]!.Text = AppCulture.FormatearMoneda(inversionTotal);

        _gridAlertas.Rows.Clear();
        foreach (var a in _listaAlertas)
        {
            var nivelTexto = a.Criticidad switch
            {
                NivelCriticidadStock.Agotado => "🔴 Agotado (0)",
                NivelCriticidadStock.Critico => "🟠 Crítico",
                _ => "🟡 En el Mínimo"
            };

            var rowIndex = _gridAlertas.Rows.Add(
                a.ProductoId,
                nivelTexto,
                a.Sku,
                a.Nombre,
                a.CategoriaNombre,
                a.StockActual,
                a.CantidadMinima,
                $"{a.UnidadesFaltantes} unids",
                AppCulture.FormatearMoneda(a.PrecioCosto),
                AppCulture.FormatearMoneda(a.InversionReposicion)
            );

            var row = _gridAlertas.Rows[rowIndex];
            if (a.Criticidad == NivelCriticidadStock.Agotado)
            {
                row.Cells["Criticidad"].Style.ForeColor = UITheme.Danger;
                row.Cells["StockActual"].Style.ForeColor = UITheme.Danger;
                row.Cells["StockActual"].Style.Font = UITheme.SectionFont;
            }
            else if (a.Criticidad == NivelCriticidadStock.Critico)
            {
                row.Cells["Criticidad"].Style.ForeColor = UITheme.Warning;
                row.Cells["StockActual"].Style.ForeColor = UITheme.Warning;
            }
        }
    }

    private async Task AbrirCompraProductoSeleccionadoAsync()
    {
        if (_gridAlertas.SelectedRows.Count == 0)
        {
            MessageBox.Show("Por favor seleccione un producto crítico para comprar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var id = (int)_gridAlertas.SelectedRows[0].Cells["ProductoId"].Value;
        var alerta = _listaAlertas.FirstOrDefault(a => a.ProductoId == id);

        var modal = new RegistrarCompraForm(_compraService, _proveedorService, _productoService, _sesionActual);
        if (modal.ShowDialog(this) == DialogResult.OK)
        {
            await RecargarAlertasAsync();
        }
    }
}
