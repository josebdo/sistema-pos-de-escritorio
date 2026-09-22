using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;

namespace SistemaCelulares.App.Forms;

public class GestionarImeisModalForm : Form
{
    private readonly IProductoService _productoService;
    private readonly Producto _producto;

    private DataGridView _dgvUnidades = null!;
    private TextBox _txtNuevoImei = null!;
    private TextBox _txtNotas = null!;
    private Button _btnAgregar = null!;
    private Label _lblResumen = null!;

    public GestionarImeisModalForm(IProductoService productoService, Producto producto)
    {
        _productoService = productoService;
        _producto = producto;

        InitializeComponent();
        _ = CargarUnidadesAsync();
    }

    private void InitializeComponent()
    {
        Text = $"Control de IMEIs / Series - {_producto.Nombre}";
        Size = new Size(680, 520);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        BackColor = Color.FromArgb(245, 247, 250);

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Color.FromArgb(26, 35, 126),
            Padding = new Padding(20, 10, 20, 10)
        };

        var lblTitulo = new Label
        {
            Text = $"UNIDADES Y SERIES (IMEI): {_producto.Nombre}",
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = $"SKU: {_producto.Sku} | Precio Venta: RD${_producto.PrecioVenta:N2}",
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(197, 202, 233),
            Location = new Point(20, 36),
            AutoSize = true
        };

        pnlHeader.Controls.Add(lblTitulo);
        pnlHeader.Controls.Add(lblSub);
        Controls.Add(pnlHeader);

        // Panel de ingreso de nuevo IMEI
        var pnlIngreso = new GroupBox
        {
            Text = "Registrar Nueva Unidad por IMEI",
            Dock = DockStyle.Top,
            Height = 85,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(26, 35, 126),
            Padding = new Padding(15, 8, 15, 8)
        };

        var lblImei = new Label { Text = "IMEI (15 dígitos):", Location = new Point(15, 25), AutoSize = true, ForeColor = Color.FromArgb(33, 33, 33), Font = new Font("Segoe UI", 9F) };
        _txtNuevoImei = new TextBox { Location = new Point(15, 45), Width = 220, Height = 26, PlaceholderText = "Ej. 356789123456789", Font = new Font("Segoe UI", 9.5F) };

        var lblNotas = new Label { Text = "Notas / Color / Condición:", Location = new Point(245, 25), AutoSize = true, ForeColor = Color.FromArgb(33, 33, 33), Font = new Font("Segoe UI", 9F) };
        _txtNotas = new TextBox { Location = new Point(245, 45), Width = 260, Height = 26, PlaceholderText = "Opcional (ej: Color Azul, Sellado)", Font = new Font("Segoe UI", 9.5F) };

        _btnAgregar = new Button
        {
            Text = "+ Agregar",
            Location = new Point(515, 43),
            Size = new Size(130, 29),
            BackColor = Color.FromArgb(46, 125, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnAgregar.FlatAppearance.BorderSize = 0;
        _btnAgregar.Click += BtnAgregar_Click;

        pnlIngreso.Controls.Add(lblImei);
        pnlIngreso.Controls.Add(_txtNuevoImei);
        pnlIngreso.Controls.Add(lblNotas);
        pnlIngreso.Controls.Add(_txtNotas);
        pnlIngreso.Controls.Add(_btnAgregar);
        Controls.Add(pnlIngreso);

        // Grid de Unidades
        _dgvUnidades = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 28 }
        };

        _dgvUnidades.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", FillWeight = 20 });
        _dgvUnidades.Columns.Add(new DataGridViewTextBoxColumn { Name = "Imei", HeaderText = "IMEI / Serie", FillWeight = 65 });
        _dgvUnidades.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", HeaderText = "Estado", FillWeight = 40 });
        _dgvUnidades.Columns.Add(new DataGridViewTextBoxColumn { Name = "FechaIngreso", HeaderText = "Fecha Ingreso", FillWeight = 45 });
        _dgvUnidades.Columns.Add(new DataGridViewTextBoxColumn { Name = "Venta", HeaderText = "Venta / Factura", FillWeight = 45 });
        _dgvUnidades.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notas", HeaderText = "Notas", FillWeight = 50 });

        Controls.Add(_dgvUnidades);

        // Status Panel
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 45,
            BackColor = Color.FromArgb(238, 238, 238),
            Padding = new Padding(15, 8, 15, 8)
        };

        _lblResumen = new Label
        {
            Text = "Unidades en Stock: 0 | Vendidas: 0",
            Location = new Point(15, 12),
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(26, 35, 126)
        };

        var btnCerrar = new Button
        {
            Text = "Cerrar",
            Location = new Point(545, 8),
            Size = new Size(100, 30),
            BackColor = Color.FromArgb(224, 224, 224),
            FlatStyle = FlatStyle.Flat,
            DialogResult = DialogResult.OK,
            Cursor = Cursors.Hand
        };
        btnCerrar.FlatAppearance.BorderSize = 0;

        pnlBottom.Controls.Add(_lblResumen);
        pnlBottom.Controls.Add(btnCerrar);
        Controls.Add(pnlBottom);
    }

    private async Task CargarUnidadesAsync()
    {
        try
        {
            var unidades = await _productoService.ObtenerUnidadesPorProductoAsync(_producto.Id);
            _dgvUnidades.Rows.Clear();

            int enStock = 0;
            int vendidas = 0;

            foreach (var u in unidades)
            {
                if (u.Estado == EstadoUnidadProducto.EnStock) enStock++;
                else if (u.Estado == EstadoUnidadProducto.Vendido) vendidas++;

                int rowIndex = _dgvUnidades.Rows.Add(
                    u.Id,
                    u.Imei,
                    u.Estado switch
                    {
                        EstadoUnidadProducto.EnStock => "🟢 En Stock",
                        EstadoUnidadProducto.Vendido => "🔵 Vendido",
                        EstadoUnidadProducto.EnGarantia => "🟠 En Garantía",
                        EstadoUnidadProducto.Devuelto => "🔴 Devuelto",
                        _ => u.Estado.ToString()
                    },
                    u.FechaIngreso.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                    u.Venta != null ? u.Venta.NumeroFactura : "—",
                    u.Notas ?? "—"
                );

                if (u.Estado == EstadoUnidadProducto.Vendido)
                {
                    _dgvUnidades.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Gray;
                }
            }

            _lblResumen.Text = $"Total Unidades: {unidades.Count} | En Stock: {enStock} | Vendidas: {vendidas}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar unidades: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void BtnAgregar_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtNuevoImei.Text))
        {
            MessageBox.Show("Por favor ingrese el IMEI del celular.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtNuevoImei.Focus();
            return;
        }

        var imei = _txtNuevoImei.Text.Trim();
        try
        {
            await _productoService.RegistrarUnidadImeiAsync(_producto.Id, imei, _txtNotas.Text);
            _txtNuevoImei.Clear();
            _txtNotas.Clear();
            _txtNuevoImei.Focus();
            await CargarUnidadesAsync();
            MessageBox.Show($"Unidad con IMEI '{imei}' agregada al stock exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al registrar IMEI: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
