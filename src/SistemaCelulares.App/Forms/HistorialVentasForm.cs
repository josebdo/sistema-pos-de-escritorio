using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class HistorialVentasForm : Form
{
    private readonly IVentaService _ventaService;
    private readonly SesionUsuario _sesion;

    private DateTimePicker _dtpDesde = null!;
    private DateTimePicker _dtpHasta = null!;
    private TextBox _txtBusqueda = null!;
    private DataGridView _gridVentas = null!;
    private List<Venta> _ventasCargadas = new();

    public HistorialVentasForm(IVentaService ventaService, SesionUsuario sesion)
    {
        _ventaService = ventaService;
        _sesion = sesion;

        InitializeComponents();
        CargarHistorial();
    }

    private void InitializeComponents()
    {
        Text = "Historial de Ventas y Comprobantes Fiscales";
        Size = new Size(1050, 650);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = UITheme.AppBg;
        Font = UITheme.BodyFont;

        var panelPrincipal = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
        Controls.Add(panelPrincipal);

        // Header
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60
        };

        var lblTitulo = new Label
        {
            Text = "Historial de Ventas y Comprobantes Fiscales",
            Font = UITheme.TitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(0, 5),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblTitulo);

        var lblSub = new Label
        {
            Text = "Consulte facturas emitidas, NCF DGII, comprobantes fiscales, anulación y reimpresión de tickets",
            Font = UITheme.SmallFont,
            ForeColor = UITheme.TextMuted,
            Location = new Point(0, 35),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblSub);
        panelPrincipal.Controls.Add(pnlHeader);

        // Barra de Filtros
        var flowFiltros = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 6, 0, 10)
        };

        var lblDesde = new Label { Text = "Desde:", AutoSize = true, Font = UITheme.BodyFont, Margin = new Padding(0, 6, 6, 4) };
        flowFiltros.Controls.Add(lblDesde);

        _dtpDesde = new DateTimePicker { Size = new Size(125, 28), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30), Margin = new Padding(0, 2, 10, 4) };
        _dtpDesde.ValueChanged += (s, e) => CargarHistorial();
        flowFiltros.Controls.Add(_dtpDesde);

        var lblHasta = new Label { Text = "Hasta:", AutoSize = true, Font = UITheme.BodyFont, Margin = new Padding(4, 6, 6, 4) };
        flowFiltros.Controls.Add(lblHasta);

        _dtpHasta = new DateTimePicker { Size = new Size(125, 28), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(1), Margin = new Padding(0, 2, 10, 4) };
        _dtpHasta.ValueChanged += (s, e) => CargarHistorial();
        flowFiltros.Controls.Add(_dtpHasta);

        var lblBuscar = new Label { Text = "🔍 Buscar:", AutoSize = true, Font = UITheme.BodyFont, Margin = new Padding(4, 6, 6, 4) };
        flowFiltros.Controls.Add(lblBuscar);

        _txtBusqueda = new TextBox { Size = new Size(220, 28), Font = new Font("Segoe UI", 9.5F), PlaceholderText = "No. Factura / NCF / Cliente", Margin = new Padding(0, 2, 10, 4) };
        _txtBusqueda.TextChanged += (s, e) => FiltrarVentas();
        flowFiltros.Controls.Add(_txtBusqueda);

        var btnFiltrar = new Button
        {
            Text = "🔄 Actualizar",
            Size = new Size(115, 30),
            Margin = new Padding(0, 1, 6, 4)
        };
        UITheme.AplicarBotonSecundario(btnFiltrar);
        btnFiltrar.Click += (s, e) => CargarHistorial();
        flowFiltros.Controls.Add(btnFiltrar);

        // DataGridView
        var panelGrid = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(1) };
        _gridVentas = new DataGridView { Dock = DockStyle.Fill };
        UITheme.EstilizarDataGridView(_gridVentas);
        ConfigurarGrid();
        _gridVentas.CellContentClick += async (s, e) => await ManejarAccionGrid(e.RowIndex, e.ColumnIndex);
        panelGrid.Controls.Add(_gridVentas);

        // Agregar al panel principal en orden de acoplamiento correcto
        panelPrincipal.Controls.Add(panelGrid);
        panelPrincipal.Controls.Add(flowFiltros);
        panelPrincipal.Controls.Add(pnlHeader);
    }

    private void ConfigurarGrid()
    {
        _gridVentas.Columns.Clear();
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", Visible = false });
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Factura", HeaderText = "Factura", FillWeight = 85 });
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ncf", HeaderText = "NCF DGII", FillWeight = 95 });
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fecha", HeaderText = "Fecha / Hora", FillWeight = 100 });
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cliente", HeaderText = "Cliente", FillWeight = 140 });
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Subtotal", HeaderText = "Subtotal", FillWeight = 75 });
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Itbis", HeaderText = "ITBIS (18%)", FillWeight = 75 });
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Total", HeaderText = "Total (RD$)", FillWeight = 85 });
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", HeaderText = "Estado", FillWeight = 80 });
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cajero", HeaderText = "Cajero", FillWeight = 90 });

        _gridVentas.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "Detalle",
            HeaderText = "Detalle",
            Text = "🔍 Ver Detalle",
            UseColumnTextForButtonValue = true,
            FillWeight = 85
        });

        _gridVentas.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "Imprimir",
            HeaderText = "Ticket",
            Text = "🖨️ Ticket",
            UseColumnTextForButtonValue = true,
            FillWeight = 60
        });

        _gridVentas.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "Anular",
            HeaderText = "Anular",
            Text = "⛔ Anular",
            UseColumnTextForButtonValue = true,
            FillWeight = 60
        });

        _gridVentas.DoubleClick += (s, e) =>
        {
            if (_gridVentas.SelectedRows.Count > 0)
            {
                int ventaId = (int)_gridVentas.SelectedRows[0].Cells["Id"].Value;
                AbrirDetalleVenta(ventaId);
            }
        };
    }

    private void AbrirDetalleVenta(int ventaId)
    {
        using var dlg = new DetalleVentaModalForm(ventaId, _ventaService, _sesion);
        dlg.ShowDialog(this);
        CargarHistorial();
    }

    private async void CargarHistorial()
    {
        var desdeUtc = _dtpDesde.Value.Date.ToUniversalTime();
        var hastaUtc = _dtpHasta.Value.Date.AddDays(1).ToUniversalTime();

        _ventasCargadas = await _ventaService.ObtenerHistorialAsync(desdeUtc, hastaUtc);

        FiltrarVentas();
    }

    private void FiltrarVentas()
    {
        string term = _txtBusqueda.Text.Trim().ToLower();
        var filtradas = _ventasCargadas.Where(v =>
            string.IsNullOrEmpty(term) ||
            v.NumeroFactura.ToLower().Contains(term) ||
            (v.Ncf != null && v.Ncf.ToLower().Contains(term)) ||
            (v.NombreClienteAnonimo != null && v.NombreClienteAnonimo.ToLower().Contains(term)) ||
            (v.Cliente != null && v.Cliente.NombreCompleto.ToLower().Contains(term))
        ).ToList();

        _gridVentas.Rows.Clear();
        foreach (var v in filtradas)
        {
            string cliente = v.Cliente?.NombreCompleto ?? v.NombreClienteAnonimo ?? "Consumidor Final";
            string cajero = v.Usuario?.NombreCompleto ?? "Cajero";

            int rowIndex = _gridVentas.Rows.Add(
                v.Id,
                v.NumeroFactura,
                v.Ncf ?? "-",
                v.FechaVenta.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                cliente,
                $"RD$ {v.Subtotal:N2}",
                $"RD$ {v.Itbis:N2}",
                $"RD$ {v.Total:N2}",
                v.Estado.ToString(),
                cajero
            );

            if (v.Estado == EstadoVenta.Anulada)
            {
                _gridVentas.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(149, 165, 166);
                _gridVentas.Rows[rowIndex].DefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Strikeout);
            }
        }
    }

    private async Task ManejarAccionGrid(int rowIndex, int colIndex)
    {
        if (rowIndex < 0) return;

        int ventaId = (int)_gridVentas.Rows[rowIndex].Cells["Id"].Value;

        if (colIndex == _gridVentas.Columns["Detalle"].Index)
        {
            AbrirDetalleVenta(ventaId);
        }
        else if (colIndex == _gridVentas.Columns["Imprimir"].Index)
        {
            try
            {
                var ticket = await _ventaService.GenerarTicketVentaAsync(ventaId);
                TicketRenderer.ImprimirTicket(ticket);
                MessageBox.Show("Ticket enviado a la impresora.", "Impresión", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar ticket: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        else if (colIndex == _gridVentas.Columns["Anular"].Index)
        {
            var venta = _ventasCargadas.FirstOrDefault(x => x.Id == ventaId);
            if (venta == null || venta.Estado == EstadoVenta.Anulada)
            {
                MessageBox.Show("Esta venta ya se encuentra anulada.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"¿Está seguro de anular la venta {venta.NumeroFactura}? Se revertirá el stock al inventario y se cancelará el pago registrado.",
                "Confirmar Anulación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                bool ok = await _ventaService.AnularVentaAsync(ventaId, "Anulación manual desde historial", _sesion.UsuarioId);
                if (ok)
                {
                    MessageBox.Show("Venta anulada y stock devuelto exitosamente.", "Anulación Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    CargarHistorial();
                }
            }
        }
    }
}
