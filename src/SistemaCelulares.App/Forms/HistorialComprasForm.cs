using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Constants;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class HistorialComprasForm : Form
{
    private readonly ICompraService _compraService;
    private readonly IProveedorService _proveedorService;
    private readonly IProductoService _productoService;
    private readonly SesionUsuario _sesionActual;

    private DateTimePicker _dtpDesde = null!;
    private DateTimePicker _dtpHasta = null!;
    private ComboBox _cbProveedores = null!;
    private DataGridView _gridCompras = null!;
    private DataGridView _gridDetalles = null!;
    private Button _btnNuevaCompra = null!;
    private Label _lblTotalPeriodo = null!;

    private List<Compra> _listaCompras = new();

    public HistorialComprasForm(
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
            await CargarComboProveedoresAsync();
            await RecargarComprasAsync();
        };
    }

    private void InitializeCustomComponents()
    {
        Text = "Historial de Compras a Proveedores";
        Size = new Size(1100, 700);
        BackColor = UITheme.AppBg;
        Font = UITheme.BodyFont;

        var panelPrincipal = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
        Controls.Add(panelPrincipal);

        // Header
        var header = new Panel { Dock = DockStyle.Top, Height = 60 };
        var lblTitulo = new Label
        {
            Text = "Historial de Compras a Proveedores",
            Font = UITheme.TitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(0, 5),
            AutoSize = true
        };
        header.Controls.Add(lblTitulo);

        _lblTotalPeriodo = new Label
        {
            Text = "Cargando compras...",
            Font = UITheme.SmallFont,
            ForeColor = UITheme.TextMuted,
            Location = new Point(0, 35),
            AutoSize = true
        };
        header.Controls.Add(_lblTotalPeriodo);
        panelPrincipal.Controls.Add(header);

        // Toolbar Contenedor
        var flowToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 45,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 4, 0, 6)
        };

        var lblDesde = new Label { Text = "Desde:", AutoSize = true, Font = UITheme.BodyFont, Margin = new Padding(0, 5, 4, 0) };
        flowToolbar.Controls.Add(lblDesde);

        _dtpDesde = new DateTimePicker { Size = new Size(110, 26), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1), Margin = new Padding(0, 2, 8, 0) };
        _dtpDesde.ValueChanged += async (s, e) => await RecargarComprasAsync();
        flowToolbar.Controls.Add(_dtpDesde);

        var lblHasta = new Label { Text = "Hasta:", AutoSize = true, Font = UITheme.BodyFont, Margin = new Padding(0, 5, 4, 0) };
        flowToolbar.Controls.Add(lblHasta);

        _dtpHasta = new DateTimePicker { Size = new Size(110, 26), Format = DateTimePickerFormat.Short, Value = DateTime.Today, Margin = new Padding(0, 2, 8, 0) };
        _dtpHasta.ValueChanged += async (s, e) => await RecargarComprasAsync();
        flowToolbar.Controls.Add(_dtpHasta);

        var lblProv = new Label { Text = "Proveedor:", AutoSize = true, Font = UITheme.BodyFont, Margin = new Padding(0, 5, 4, 0) };
        flowToolbar.Controls.Add(lblProv);

        _cbProveedores = new ComboBox { Size = new Size(160, 26), DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 2, 12, 0) };
        _cbProveedores.SelectedIndexChanged += async (s, e) => await RecargarComprasAsync();
        flowToolbar.Controls.Add(_cbProveedores);

        _btnNuevaCompra = new Button { Text = "➕ Registrar Compra", Size = new Size(150, 32), Margin = new Padding(0, 0, 0, 0) };
        UITheme.AplicarBotonPrimario(_btnNuevaCompra);
        _btnNuevaCompra.Click += async (s, e) => await AbrirRegistrarCompraAsync();
        _btnNuevaCompra.Visible = _sesionActual.TienePermiso(Permisos.ComprasRegistrar);
        flowToolbar.Controls.Add(_btnNuevaCompra);

        panelPrincipal.Controls.Add(flowToolbar);

        // Split Container (Compras arriba, Detalles abajo)
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 300,
            BackColor = UITheme.BorderColor
        };

        // Panel Superior: Compras
        var panelGridCompras = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(1) };
        _gridCompras = new DataGridView { Dock = DockStyle.Fill };
        UITheme.EstilizarDataGridView(_gridCompras);
        ConfigurarColumnasCompras();
        _gridCompras.SelectionChanged += (s, e) => MostrarDetallesCompraSeleccionada();
        panelGridCompras.Controls.Add(_gridCompras);
        split.Panel1.Controls.Add(panelGridCompras);

        // Panel Inferior: Detalles de la compra
        var panelInferior = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10) };
        var lblTitDetalle = new Label
        {
            Text = "📦 Productos incluidos en la compra seleccionada:",
            Font = UITheme.SectionFont,
            ForeColor = UITheme.DarkBg,
            Dock = DockStyle.Top,
            Height = 25
        };
        panelInferior.Controls.Add(lblTitDetalle);

        _gridDetalles = new DataGridView { Dock = DockStyle.Fill };
        UITheme.EstilizarDataGridView(_gridDetalles);
        ConfigurarColumnasDetalles();
        panelInferior.Controls.Add(_gridDetalles);
        split.Panel2.Controls.Add(panelInferior);

        panelPrincipal.Controls.Add(split);
    }

    private void ConfigurarColumnasCompras()
    {
        _gridCompras.Columns.Clear();
        _gridCompras.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "# Compra", Width = 80 });
        _gridCompras.Columns.Add(new DataGridViewTextBoxColumn { Name = "Proveedor", HeaderText = "Proveedor", FillWeight = 160 });
        _gridCompras.Columns.Add(new DataGridViewTextBoxColumn { Name = "Factura", HeaderText = "N° Factura", FillWeight = 95 });
        _gridCompras.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fecha", HeaderText = "Fecha Compra", FillWeight = 110 });
        _gridCompras.Columns.Add(new DataGridViewTextBoxColumn { Name = "Total", HeaderText = "Total Factura", FillWeight = 100 });
        _gridCompras.Columns.Add(new DataGridViewTextBoxColumn { Name = "Usuario", HeaderText = "Registrado Por", FillWeight = 110 });
        _gridCompras.Columns.Add(new DataGridViewTextBoxColumn { Name = "Observaciones", HeaderText = "Observaciones", FillWeight = 140 });
    }

    private void ConfigurarColumnasDetalles()
    {
        _gridDetalles.Columns.Clear();
        _gridDetalles.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sku", HeaderText = "SKU", FillWeight = 90 });
        _gridDetalles.Columns.Add(new DataGridViewTextBoxColumn { Name = "Producto", HeaderText = "Producto", FillWeight = 200 });
        _gridDetalles.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cantidad", HeaderText = "Cant. Comprada", FillWeight = 80 });
        _gridDetalles.Columns.Add(new DataGridViewTextBoxColumn { Name = "CostoUnitario", HeaderText = "Costo Unit. (Último)", FillWeight = 100 });
        _gridDetalles.Columns.Add(new DataGridViewTextBoxColumn { Name = "Subtotal", HeaderText = "Subtotal", FillWeight = 110 });
    }

    private async Task CargarComboProveedoresAsync()
    {
        var provs = await _proveedorService.ObtenerProveedoresAsync(soloActivos: false);
        var items = new List<ProveedorComboItem>
        {
            new(0, "-- Todos los proveedores --")
        };
        items.AddRange(provs.Select(p => new ProveedorComboItem(p.Id, p.Nombre)));

        _cbProveedores.DisplayMember = nameof(ProveedorComboItem.Nombre);
        _cbProveedores.ValueMember = nameof(ProveedorComboItem.Id);
        _cbProveedores.DataSource = items;
    }

    private async Task RecargarComprasAsync()
    {
        var desde = _dtpDesde.Value.Date;
        var hasta = _dtpHasta.Value.Date;
        int? provId = null;

        if (_cbProveedores.SelectedValue is int pid && pid > 0)
        {
            provId = pid;
        }

        _listaCompras = await _compraService.ObtenerComprasAsync(desde, hasta, provId);

        _gridCompras.Rows.Clear();
        decimal totalGasto = 0;

        foreach (var c in _listaCompras)
        {
            _gridCompras.Rows.Add(
                $"#{c.Id}",
                c.Proveedor.Nombre,
                c.NumeroFactura ?? "-",
                c.FechaCompra.ToLocalTime().ToString("dd/MM/yyyy hh:mm tt"),
                AppCulture.FormatearMoneda(c.Total),
                c.Usuario.NombreCompleto,
                c.Observaciones ?? "-"
            );
            totalGasto += c.Total;
        }

        _lblTotalPeriodo.Text = $"Total facturado en el periodo: {AppCulture.FormatearMoneda(totalGasto)} ({_listaCompras.Count} compras registradas)";

        MostrarDetallesCompraSeleccionada();
    }

    private void MostrarDetallesCompraSeleccionada()
    {
        _gridDetalles.Rows.Clear();
        if (_gridCompras.SelectedRows.Count == 0) return;

        var idStr = _gridCompras.SelectedRows[0].Cells["Id"].Value?.ToString()?.Replace("#", "");
        if (int.TryParse(idStr, out int id))
        {
            var compra = _listaCompras.FirstOrDefault(c => c.Id == id);
            if (compra != null)
            {
                foreach (var d in compra.Detalles)
                {
                    _gridDetalles.Rows.Add(
                        d.Producto.Sku,
                        d.Producto.Nombre,
                        d.Cantidad,
                        AppCulture.FormatearMoneda(d.CostoUnitario),
                        AppCulture.FormatearMoneda(d.Subtotal)
                    );
                }
            }
        }
    }

    private async Task AbrirRegistrarCompraAsync()
    {
        var modal = new RegistrarCompraForm(_compraService, _proveedorService, _productoService, _sesionActual);
        if (modal.ShowDialog(this) == DialogResult.OK)
        {
            await RecargarComprasAsync();
        }
    }

    private record ProveedorComboItem(int Id, string Nombre);
}
