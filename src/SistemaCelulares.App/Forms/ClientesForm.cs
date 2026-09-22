using System.Globalization;
using SistemaCelulares.Core.Constants;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class ClientesForm : Form
{
    private readonly IClienteService _clienteService;
    private readonly SesionUsuario _sesion;

    private DataGridView _dgvClientes = null!;
    private TextBox _txtBusqueda = null!;
    private CheckBox _chkSoloActivos = null!;
    private Button _btnNuevo = null!;
    private Button _btnEditar = null!;
    private Button _btnHistorial = null!;
    private Button _btnToggleEstado = null!;
    private Label _lblTotal = null!;

    public ClientesForm(IClienteService clienteService, SesionUsuario sesion)
    {
        _clienteService = clienteService;
        _sesion = sesion;

        InitializeComponent();
        _ = CargarClientesAsync();
    }

    private void InitializeComponent()
    {
        Text = "Gestión de Clientes y Fidelización";
        Size = new Size(1000, 600);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        BackColor = Color.FromArgb(245, 247, 250);

        // Header Panel
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Color.FromArgb(26, 35, 126),
            Padding = new Padding(20, 12, 20, 10)
        };

        var lblTitulo = new Label
        {
            Text = "CLIENTES Y FIDELIZACIÓN",
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true
        };

        var lblSubtitulo = new Label
        {
            Text = "Directorio de clientes, historial de compras, reparaciones asociadas y descuentos frecuentes",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(197, 202, 233),
            Location = new Point(20, 38),
            AutoSize = true
        };

        pnlHeader.Controls.Add(lblTitulo);
        pnlHeader.Controls.Add(lblSubtitulo);
        Controls.Add(pnlHeader);

        // Toolbar Panel
        var pnlToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 55,
            BackColor = Color.White,
            Padding = new Padding(15, 10, 15, 10)
        };

        var lblBuscar = new Label
        {
            Text = "Buscar:",
            Location = new Point(15, 17),
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
        };

        _txtBusqueda = new TextBox
        {
            Location = new Point(70, 14),
            Width = 260,
            Height = 28,
            PlaceholderText = "Nombre, teléfono, RNC/cédula..."
        };
        _txtBusqueda.TextChanged += async (s, e) => await CargarClientesAsync();

        _chkSoloActivos = new CheckBox
        {
            Text = "Solo Activos",
            Location = new Point(345, 16),
            Checked = true,
            AutoSize = true
        };
        _chkSoloActivos.CheckedChanged += async (s, e) => await CargarClientesAsync();

        _btnNuevo = new Button
        {
            Text = "+ Nuevo Cliente",
            Location = new Point(480, 11),
            Size = new Size(130, 32),
            BackColor = Color.FromArgb(46, 125, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Visible = _sesion.TienePermiso(Permisos.ClientesCrear)
        };
        _btnNuevo.FlatAppearance.BorderSize = 0;
        _btnNuevo.Click += BtnNuevo_Click;

        _btnEditar = new Button
        {
            Text = "Editar",
            Location = new Point(620, 11),
            Size = new Size(90, 32),
            BackColor = Color.FromArgb(26, 35, 126),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Visible = _sesion.TienePermiso(Permisos.ClientesEditar)
        };
        _btnEditar.FlatAppearance.BorderSize = 0;
        _btnEditar.Click += BtnEditar_Click;

        _btnHistorial = new Button
        {
            Text = "Ver Historial",
            Location = new Point(720, 11),
            Size = new Size(110, 32),
            BackColor = Color.FromArgb(2, 136, 209),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnHistorial.FlatAppearance.BorderSize = 0;
        _btnHistorial.Click += BtnHistorial_Click;

        _btnToggleEstado = new Button
        {
            Text = "Desactivar",
            Location = new Point(840, 11),
            Size = new Size(110, 32),
            BackColor = Color.FromArgb(198, 40, 40),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Visible = _sesion.TienePermiso(Permisos.ClientesEliminar)
        };
        _btnToggleEstado.FlatAppearance.BorderSize = 0;
        _btnToggleEstado.Click += BtnToggleEstado_Click;

        pnlToolbar.Controls.Add(lblBuscar);
        pnlToolbar.Controls.Add(_txtBusqueda);
        pnlToolbar.Controls.Add(_chkSoloActivos);
        pnlToolbar.Controls.Add(_btnNuevo);
        pnlToolbar.Controls.Add(_btnEditar);
        pnlToolbar.Controls.Add(_btnHistorial);
        pnlToolbar.Controls.Add(_btnToggleEstado);
        Controls.Add(pnlToolbar);

        // Grid
        _dgvClientes = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 32 }
        };

        _dgvClientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", FillWeight = 20 });
        _dgvClientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nombre", HeaderText = "Nombre del Cliente", FillWeight = 90 });
        _dgvClientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Telefono", HeaderText = "Teléfono / WhatsApp", FillWeight = 50 });
        _dgvClientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Rnc", HeaderText = "RNC / Cédula", FillWeight = 45 });
        _dgvClientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Email", HeaderText = "Correo Electrónico", FillWeight = 60 });
        _dgvClientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Frecuente", HeaderText = "Cliente VIP", FillWeight = 40 });
        _dgvClientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Descuento", HeaderText = "Descuento", FillWeight = 35 });
        _dgvClientes.SelectionChanged += DgvClientes_SelectionChanged;

        // Status Panel
        var pnlStatus = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 35,
            BackColor = Color.FromArgb(238, 238, 238),
            Padding = new Padding(15, 8, 15, 8)
        };

        _lblTotal = new Label
        {
            Text = "Clientes: 0",
            Dock = DockStyle.Left,
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(66, 66, 66)
        };

        pnlStatus.Controls.Add(_lblTotal);

        // Jerarquía de Acoplamiento ordenada
        Controls.Add(_dgvClientes); // Fill
        Controls.Add(pnlStatus);    // Bottom
        Controls.Add(pnlToolbar);   // Top 2
        Controls.Add(pnlHeader);    // Top 1
    }

    private async Task CargarClientesAsync()
    {
        try
        {
            IReadOnlyList<Cliente> clientes;
            if (string.IsNullOrWhiteSpace(_txtBusqueda.Text))
            {
                clientes = await _clienteService.ObtenerTodosAsync(_chkSoloActivos.Checked);
            }
            else
            {
                clientes = await _clienteService.BuscarAsync(_txtBusqueda.Text);
                if (_chkSoloActivos.Checked)
                {
                    clientes = clientes.Where(c => c.Activo).ToList();
                }
            }

            _dgvClientes.Rows.Clear();
            foreach (var c in clientes)
            {
                int rowIndex = _dgvClientes.Rows.Add(
                    c.Id,
                    c.NombreCompleto,
                    c.Telefono ?? "—",
                    c.RncOCedula ?? "—",
                    c.Email ?? "—",
                    c.EsFrecuente ? "⭐ VIP" : "Estándar",
                    c.EsFrecuente ? $"{c.PorcentajeDescuento:N0}%" : "0%",
                    c.Activo ? "Activo" : "Inactivo"
                );

                _dgvClientes.Rows[rowIndex].Tag = c;
                if (!c.Activo)
                {
                    _dgvClientes.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Gray;
                }
                else if (c.EsFrecuente)
                {
                    _dgvClientes.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 253, 231);
                }
            }

            _lblTotal.Text = $"Total Clientes: {clientes.Count}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar clientes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DgvClientes_SelectionChanged(object? sender, EventArgs e)
    {
        if (_dgvClientes.SelectedRows.Count > 0 && _dgvClientes.SelectedRows[0].Tag is Cliente c)
        {
            _btnToggleEstado.Text = c.Activo ? "Desactivar" : "Activar";
            _btnToggleEstado.BackColor = c.Activo ? Color.FromArgb(198, 40, 40) : Color.FromArgb(46, 125, 50);
        }
    }

    private async void BtnNuevo_Click(object? sender, EventArgs e)
    {
        using var modal = new ClienteModalForm();
        if (modal.ShowDialog(this) == DialogResult.OK && modal.ClienteGuardado != null)
        {
            try
            {
                await _clienteService.CrearAsync(modal.ClienteGuardado);
                await CargarClientesAsync();
                MessageBox.Show("Cliente registrado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar cliente: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async void BtnEditar_Click(object? sender, EventArgs e)
    {
        if (_dgvClientes.SelectedRows.Count == 0 || _dgvClientes.SelectedRows[0].Tag is not Cliente c)
        {
            MessageBox.Show("Seleccione un cliente para editar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var modal = new ClienteModalForm(c);
        if (modal.ShowDialog(this) == DialogResult.OK && modal.ClienteGuardado != null)
        {
            try
            {
                await _clienteService.ActualizarAsync(modal.ClienteGuardado);
                await CargarClientesAsync();
                MessageBox.Show("Cliente actualizado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar cliente: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async void BtnToggleEstado_Click(object? sender, EventArgs e)
    {
        if (_dgvClientes.SelectedRows.Count == 0 || _dgvClientes.SelectedRows[0].Tag is not Cliente c)
            return;

        try
        {
            if (c.Activo)
            {
                var confirm = MessageBox.Show($"¿Está seguro de desactivar al cliente '{c.NombreCompleto}'?\nNo se borrará físicamente para preservar su historial de compras y reparaciones.", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm == DialogResult.Yes)
                {
                    await _clienteService.DesactivarAsync(c.Id);
                    await CargarClientesAsync();
                }
            }
            else
            {
                await _clienteService.ActivarAsync(c.Id);
                await CargarClientesAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cambiar estado: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void BtnHistorial_Click(object? sender, EventArgs e)
    {
        if (_dgvClientes.SelectedRows.Count == 0 || _dgvClientes.SelectedRows[0].Tag is not Cliente c)
        {
            MessageBox.Show("Seleccione un cliente para consultar su historial.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var ventas = await _clienteService.ObtenerHistorialVentasAsync(c.Id);
            var reparaciones = await _clienteService.ObtenerHistorialReparacionesAsync(c.Id);

            string mensaje = $"HISTORIAL DE CLIENTE: {c.NombreCompleto}\n" +
                             $"Teléfono: {c.Telefono ?? "—"} | RNC/Cédula: {c.RncOCedula ?? "—"}\n" +
                             $"Condición: {(c.EsFrecuente ? $"Cliente Frecuente ({c.PorcentajeDescuento}% Desc.)" : "Cliente Regular")}\n\n" +
                             $"--- COMPRAS REGISTRADAS ({ventas.Count}) ---\n";

            if (ventas.Count == 0)
            {
                mensaje += "No tiene compras registradas.\n";
            }
            else
            {
                foreach (var v in ventas.Take(5))
                {
                    mensaje += $"• {v.FechaVenta:dd/MM/yyyy} | Factura: {v.NumeroFactura} | Total: RD${v.Total:N2} | Estado: {v.Estado}\n";
                }
                if (ventas.Count > 5) mensaje += $"... y {ventas.Count - 5} compras más.\n";
            }

            mensaje += $"\n--- REPARACIONES EN TALLER ({reparaciones.Count}) ---\n";
            if (reparaciones.Count == 0)
            {
                mensaje += "No tiene reparaciones registradas.\n";
            }
            else
            {
                foreach (var r in reparaciones.Take(5))
                {
                    mensaje += $"• {r.FechaRecepcion:dd/MM/yyyy} | Orden: {r.NumeroOrden} | {r.Marca} {r.Modelo} | Total: RD${r.PrecioFinal:N2} | Estado: {r.Estado}\n";
                }
                if (reparaciones.Count > 5) mensaje += $"... y {reparaciones.Count - 5} órdenes más.\n";
            }

            MessageBox.Show(mensaje, $"Historial - {c.NombreCompleto}", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al obtener historial: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
