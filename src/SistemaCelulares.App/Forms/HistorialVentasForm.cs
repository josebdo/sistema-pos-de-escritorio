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
    private readonly SesionUsuarioDto _sesion;

    private DateTimePicker _dtpDesde = null!;
    private DateTimePicker _dtpHasta = null!;
    private TextBox _txtBusqueda = null!;
    private DataGridView _gridVentas = null!;
    private List<Venta> _ventasCargadas = new();

    public HistorialVentasForm(IVentaService ventaService, SesionUsuarioDto sesion)
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
        BackColor = Color.FromArgb(248, 249, 250);

        // Header
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.FromArgb(24, 43, 73),
            Padding = new Padding(20, 15, 20, 15)
        };

        var lblTitulo = new Label
        {
            Text = "📜 HISTORIAL DE VENTAS Y FACTURACIÓN",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Dock = DockStyle.Fill
        };
        pnlHeader.Controls.Add(lblTitulo);

        // Barra de Filtros
        var pnlFiltros = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Color.White,
            Padding = new Padding(15, 12, 15, 12)
        };

        var lblDesde = new Label { Text = "Desde:", Location = new Point(15, 18), AutoSize = true, Font = new Font("Segoe UI", 9) };
        _dtpDesde = new DateTimePicker { Location = new Point(65, 15), Size = new Size(130, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };

        var lblHasta = new Label { Text = "Hasta:", Location = new Point(210, 18), AutoSize = true, Font = new Font("Segoe UI", 9) };
        _dtpHasta = new DateTimePicker { Location = new Point(260, 15), Size = new Size(130, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(1) };

        var lblBuscar = new Label { Text = "Buscar:", Location = new Point(410, 18), AutoSize = true, Font = new Font("Segoe UI", 9) };
        _txtBusqueda = new TextBox { Location = new Point(465, 15), Size = new Size(220, 25), PlaceholderText = "No. Factura / NCF / Cliente" };
        _txtBusqueda.TextChanged += (s, e) => FiltrarVentas();

        var btnFiltrar = new Button
        {
            Text = "🔍 Filtrar",
            Location = new Point(700, 13),
            Size = new Size(95, 29),
            BackColor = Color.FromArgb(52, 152, 219),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnFiltrar.FlatAppearance.BorderSize = 0;
        btnFiltrar.Click += (s, e) => CargarHistorial();

        pnlFiltros.Controls.Add(lblDesde);
        pnlFiltros.Controls.Add(_dtpDesde);
        pnlFiltros.Controls.Add(lblHasta);
        pnlFiltros.Controls.Add(_dtpHasta);
        pnlFiltros.Controls.Add(lblBuscar);
        pnlFiltros.Controls.Add(_txtBusqueda);
        pnlFiltros.Controls.Add(btnFiltrar);

        // Grid
        _gridVentas = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            Font = new Font("Segoe UI", 9)
        };

        ConfigurarGrid();
        _gridVentas.CellContentClick += async (s, e) => await ManejarAccionGrid(e.RowIndex, e.ColumnIndex);

        Controls.Add(_gridVentas);
        Controls.Add(pnlFiltros);
        Controls.Add(pnlHeader);
    }

    private void ConfigurarGrid()
    {
        _gridVentas.Columns.Clear();
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", Visible = false });
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Factura", HeaderText = "Factura", FillWeight = 85 });
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ncf", HeaderText = "NCF DGII", FillWeight = 95 });
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fecha", HeaderText = "Fecha", FillWeight = 100 });
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cliente", HeaderText = "Cliente", FillWeight = 140 });
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Subtotal", HeaderText = "Subtotal", FillWeight = 75 });
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Itbis", HeaderText = "ITBIS (18%)", FillWeight = 75 });
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Total", HeaderText = "Total (RD$)", FillWeight = 85 });
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", HeaderText = "Estado", FillWeight = 80 });
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cajero", HeaderText = "Cajero", FillWeight = 90 });

        _gridVentas.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "Imprimir",
            HeaderText = "Ticket",
            Text = "🖨️ Ticket",
            UseColumnTextForButtonValue = true,
            FillWeight = 65
        });

        _gridVentas.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "Anular",
            HeaderText = "Anular",
            Text = "⛔ Anular",
            UseColumnTextForButtonValue = true,
            FillWeight = 65
        });
    }

    private async void CargarHistorial()
    {
        _ventasCargadas = await _ventaService.ObtenerHistorialAsync(
            _dtpDesde.Value.Date,
            _dtpHasta.Value.Date.AddDays(1).AddSeconds(-1));

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

        if (colIndex == _gridVentas.Columns["Imprimir"].Index)
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
