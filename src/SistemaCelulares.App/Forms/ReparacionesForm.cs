using System.Globalization;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Constants;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class ReparacionesForm : Form
{
    private readonly IReparacionService _reparacionService;
    private readonly IClienteService _clienteService;
    private readonly ITurnoService _turnoService;
    private readonly IPagoService _pagoService;
    private readonly SesionUsuario _sesion;

    private DataGridView _dgvOrdenes = null!;
    private TextBox _txtBusqueda = null!;
    private ComboBox _cbEstado = null!;
    private Button _btnNueva = null!;
    private Button _btnEditar = null!;
    private Button _btnMarcarLista = null!;
    private Button _btnCobrarEntregar = null!;
    private Button _btnImprimir = null!;
    private Label _lblResumen = null!;

    public ReparacionesForm(
        IReparacionService reparacionService,
        IClienteService clienteService,
        ITurnoService turnoService,
        IPagoService pagoService,
        SesionUsuario sesion)
    {
        _reparacionService = reparacionService;
        _clienteService = clienteService;
        _turnoService = turnoService;
        _pagoService = pagoService;
        _sesion = sesion;

        InitializeComponent();
        _ = CargarOrdenesAsync();
    }

    private void InitializeComponent()
    {
        Text = "Taller de Reparaciones de Celulares";
        Size = new Size(1100, 650);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        BackColor = Color.FromArgb(245, 247, 250);

        // Header Panel
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Color.FromArgb(0, 105, 92),
            Padding = new Padding(20, 12, 20, 10)
        };

        var lblTitulo = new Label
        {
            Text = "TALLER Y SERVICIO TÉCNICO DE CELULARES",
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Control de órdenes de reparación: recepción, diagnóstico, listos para entrega y cobro final",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(178, 223, 219),
            Location = new Point(20, 38),
            AutoSize = true
        };

        pnlHeader.Controls.Add(lblTitulo);
        pnlHeader.Controls.Add(lblSub);

        // Toolbar Flow Panel
        var pnlToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.White,
            Padding = new Padding(15, 8, 15, 8)
        };

        var lblBuscar = new Label { Text = "Buscar:", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Margin = new Padding(0, 7, 5, 0) };
        _txtBusqueda = new TextBox { Width = 200, Height = 28, PlaceholderText = "Orden, cliente, IMEI...", Margin = new Padding(0, 3, 12, 4) };
        _txtBusqueda.TextChanged += async (s, e) => await CargarOrdenesAsync();

        var lblEst = new Label { Text = "Estado:", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Margin = new Padding(0, 7, 5, 0) };
        _cbEstado = new ComboBox { Width = 150, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 3, 12, 4) };
        _cbEstado.Items.AddRange(new object[] { "-- Todos los estados --", "En Reparación", "Listas para Entrega", "Entregadas", "Canceladas" });
        _cbEstado.SelectedIndex = 0;
        _cbEstado.SelectedIndexChanged += async (s, e) => await CargarOrdenesAsync();

        _btnNueva = new Button
        {
            Text = "➕ Nueva Orden",
            Size = new Size(130, 32),
            BackColor = Color.FromArgb(0, 105, 92),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 2, 6, 4),
            Visible = _sesion.TienePermiso(Permisos.ReparacionesCrear)
        };
        _btnNueva.FlatAppearance.BorderSize = 0;
        _btnNueva.Click += BtnNueva_Click;

        _btnEditar = new Button
        {
            Text = "✏️ Editar / Notas",
            Size = new Size(120, 32),
            BackColor = Color.FromArgb(2, 136, 209),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 2, 6, 4),
            Visible = _sesion.TienePermiso(Permisos.ReparacionesEditar)
        };
        _btnEditar.FlatAppearance.BorderSize = 0;
        _btnEditar.Click += BtnEditar_Click;

        _btnMarcarLista = new Button
        {
            Text = "✅ Marcar Lista",
            Size = new Size(125, 32),
            BackColor = Color.FromArgb(239, 108, 0),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 2, 6, 4),
            Visible = _sesion.TienePermiso(Permisos.ReparacionesEditar)
        };
        _btnMarcarLista.FlatAppearance.BorderSize = 0;
        _btnMarcarLista.Click += BtnMarcarLista_Click;

        _btnCobrarEntregar = new Button
        {
            Text = "💳 Cobrar y Entregar",
            Size = new Size(150, 32),
            BackColor = Color.FromArgb(46, 125, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 2, 6, 4),
            Visible = _sesion.TienePermiso(Permisos.ReparacionesCobrar)
        };
        _btnCobrarEntregar.FlatAppearance.BorderSize = 0;
        _btnCobrarEntregar.Click += BtnCobrarEntregar_Click;

        _btnImprimir = new Button
        {
            Text = "📄 Boleta / Factura",
            Size = new Size(130, 32),
            BackColor = Color.FromArgb(100, 116, 139),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 2, 6, 4)
        };
        _btnImprimir.FlatAppearance.BorderSize = 0;
        _btnImprimir.Click += BtnImprimir_Click;

        pnlToolbar.Controls.Add(lblBuscar);
        pnlToolbar.Controls.Add(_txtBusqueda);
        pnlToolbar.Controls.Add(lblEst);
        pnlToolbar.Controls.Add(_cbEstado);
        pnlToolbar.Controls.Add(_btnNueva);
        pnlToolbar.Controls.Add(_btnEditar);
        pnlToolbar.Controls.Add(_btnMarcarLista);
        pnlToolbar.Controls.Add(_btnCobrarEntregar);
        pnlToolbar.Controls.Add(_btnImprimir);

        // Grid
        _dgvOrdenes = new DataGridView
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
            RowTemplate = { Height = 36 },
            EnableHeadersVisualStyles = false
        };
        UITheme.EstilizarDataGridView(_dgvOrdenes);

        _dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Visible = false });
        _dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "NumeroOrden", HeaderText = "N° Orden", Width = 95 });
        _dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cliente", HeaderText = "Cliente", Width = 140 });
        _dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Telefono", HeaderText = "Teléfono", Width = 100 });
        _dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Equipo", HeaderText = "Equipo", Width = 130 });
        _dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Imei", HeaderText = "IMEI / Serie", Width = 130 });
        _dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Problema", HeaderText = "Falla Reportada", Width = 170 });
        _dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Total", HeaderText = "Total RD$", Width = 90 });
        _dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", HeaderText = "Estado", Width = 130 });
        _dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fecha", HeaderText = "Fecha", Width = 110 });

        _dgvOrdenes.SelectionChanged += DgvOrdenes_SelectionChanged;
        _dgvOrdenes.DoubleClick += (s, e) => BtnEditar_Click(s, e);

        // Barra Inferior de Estado
        var pnlStatus = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 35,
            BackColor = Color.FromArgb(238, 242, 246),
            Padding = new Padding(15, 8, 15, 8)
        };

        _lblResumen = new Label
        {
            Text = "Cargando órdenes...",
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = Color.FromArgb(71, 85, 105),
            AutoSize = true,
            Dock = DockStyle.Left
        };
        pnlStatus.Controls.Add(_lblResumen);

        // Agregar al formulario en orden de acoplamiento estricto
        Controls.Add(_dgvOrdenes); // Fill
        Controls.Add(pnlStatus);   // Bottom
        Controls.Add(pnlToolbar);  // Top 2
        Controls.Add(pnlHeader);   // Top 1
    }

    private async Task CargarOrdenesAsync()
    {
        try
        {
            EstadoReparacion? filtroEstado = _cbEstado.SelectedIndex switch
            {
                1 => EstadoReparacion.EnReparacion,
                2 => EstadoReparacion.ListaParaEntrega,
                3 => EstadoReparacion.Entregada,
                4 => EstadoReparacion.Cancelada,
                _ => null
            };

            IReadOnlyList<OrdenReparacion> lista;
            if (!string.IsNullOrWhiteSpace(_txtBusqueda.Text))
            {
                lista = await _reparacionService.BuscarAsync(_txtBusqueda.Text);
                if (filtroEstado.HasValue)
                {
                    lista = lista.Where(o => o.Estado == filtroEstado.Value).ToList();
                }
            }
            else
            {
                lista = await _reparacionService.ObtenerTodasAsync(filtroEstado);
            }

            _dgvOrdenes.Rows.Clear();
            int enRep = 0, listas = 0, entregadas = 0;

            foreach (var o in lista)
            {
                if (o.Estado == EstadoReparacion.EnReparacion) enRep++;
                else if (o.Estado == EstadoReparacion.ListaParaEntrega) listas++;
                else if (o.Estado == EstadoReparacion.Entregada) entregadas++;

                int rowIndex = _dgvOrdenes.Rows.Add(
                    o.Id,
                    o.NumeroOrden,
                    o.Cliente?.NombreCompleto ?? "—",
                    o.Cliente?.Telefono ?? "—",
                    $"{o.Marca} {o.Modelo}",
                    o.ImeiOSerie ?? "—",
                    o.DescripcionProblema,
                    $"RD${o.PrecioFinal:N2}",
                    o.Estado switch
                    {
                        EstadoReparacion.EnReparacion => "🔧 En Reparación",
                        EstadoReparacion.ListaParaEntrega => "🔔 Lista para Entrega",
                        EstadoReparacion.Entregada => "✅ Entregada",
                        EstadoReparacion.Cancelada => "❌ Cancelada",
                        _ => o.Estado.ToString()
                    },
                    o.FechaRecepcion.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                );

                _dgvOrdenes.Rows[rowIndex].Tag = o;

                if (o.Estado == EstadoReparacion.ListaParaEntrega)
                {
                    _dgvOrdenes.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 243, 224);
                    _dgvOrdenes.Rows[rowIndex].DefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                }
                else if (o.Estado == EstadoReparacion.Entregada)
                {
                    _dgvOrdenes.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Gray;
                }
            }

            _lblResumen.Text = $"Total: {lista.Count} | 🔧 En Taller: {enRep} | 🔔 Listas para entrega: {listas} | ✅ Entregadas: {entregadas}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar reparaciones: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DgvOrdenes_SelectionChanged(object? sender, EventArgs e)
    {
        if (_dgvOrdenes.SelectedRows.Count > 0 && _dgvOrdenes.SelectedRows[0].Tag is OrdenReparacion o)
        {
            _btnMarcarLista.Enabled = o.Estado == EstadoReparacion.EnReparacion;
            _btnCobrarEntregar.Enabled = o.Estado == EstadoReparacion.ListaParaEntrega || o.Estado == EstadoReparacion.EnReparacion;
        }
    }

    private async void BtnNueva_Click(object? sender, EventArgs e)
    {
        using var modal = new OrdenReparacionModalForm(_clienteService, _reparacionService, _sesion);
        if (modal.ShowDialog(this) == DialogResult.OK && modal.OrdenGuardada != null)
        {
            await CargarOrdenesAsync();
            MessageBox.Show($"Orden {modal.OrdenGuardada.NumeroOrden} creada exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private async void BtnEditar_Click(object? sender, EventArgs e)
    {
        if (_dgvOrdenes.SelectedRows.Count == 0 || _dgvOrdenes.SelectedRows[0].Tag is not OrdenReparacion o)
        {
            MessageBox.Show("Seleccione una orden para editar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var modal = new OrdenReparacionModalForm(_clienteService, _reparacionService, _sesion, o);
        if (modal.ShowDialog(this) == DialogResult.OK)
        {
            await CargarOrdenesAsync();
            MessageBox.Show("Orden de reparación actualizada.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private async void BtnMarcarLista_Click(object? sender, EventArgs e)
    {
        if (_dgvOrdenes.SelectedRows.Count == 0 || _dgvOrdenes.SelectedRows[0].Tag is not OrdenReparacion o) return;

        try
        {
            await _reparacionService.CambiarEstadoAsync(o.Id, EstadoReparacion.ListaParaEntrega);
            await CargarOrdenesAsync();
            MessageBox.Show($"La orden {o.NumeroOrden} ha sido marcada como 'Lista para Entrega'.\nSe puede notificar al cliente {o.Cliente?.NombreCompleto} al teléfono {o.Cliente?.Telefono}.", "Lista para Entrega", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al actualizar estado: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void BtnCobrarEntregar_Click(object? sender, EventArgs e)
    {
        if (_dgvOrdenes.SelectedRows.Count == 0 || _dgvOrdenes.SelectedRows[0].Tag is not OrdenReparacion o) return;

        if (o.Estado == EstadoReparacion.Entregada)
        {
            MessageBox.Show("Esta orden ya fue entregada y cobrada anteriormente.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var modal = new CobroReparacionModalForm(o, _turnoService, _pagoService, _reparacionService, _sesion);
        if (modal.ShowDialog(this) == DialogResult.OK && modal.CobradoExitosamente)
        {
            await CargarOrdenesAsync();
        }
    }

    private void BtnImprimir_Click(object? sender, EventArgs e)
    {
        if (_dgvOrdenes.SelectedRows.Count == 0 || _dgvOrdenes.SelectedRows[0].Tag is not OrdenReparacion o)
        {
            MessageBox.Show("Seleccione una orden para imprimir su comprobante o factura.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var dialog = MessageBox.Show(
            $"¿Desea enviar a imprimir el comprobante / ticket de la orden {o.NumeroOrden}?\n(Seleccione 'No' para ver la boleta en pantalla)",
            "Impresión de Comprobante / Factura",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question
        );

        if (dialog == DialogResult.Yes)
        {
            var ticket = new TicketReparacionDto
            {
                NombreEmpresa = "Veyra POS — Tienda y Taller de Celulares",
                RncEmpresa = "131-12345-6",
                DireccionEmpresa = "Av. Principal #123, Santo Domingo",
                TelefonoEmpresa = "(809) 555-0199",
                NumeroOrden = o.NumeroOrden,
                Fecha = o.FechaEntrega?.ToLocalTime() ?? o.FechaRecepcion.ToLocalTime(),
                TecnicoEntrega = o.UsuarioEntrega?.NombreCompleto ?? _sesion.NombreCompleto,
                ClienteNombre = o.Cliente?.NombreCompleto ?? "Cliente General",
                ClienteTelefono = o.Cliente?.Telefono ?? "—",
                ClienteRnc = o.Cliente?.RncOCedula,
                EquipoMarcaModelo = $"{o.Marca} {o.Modelo}",
                ImeiOSerie = o.ImeiOSerie ?? "—",
                DescripcionProblema = o.DescripcionProblema,
                DiagnosticoSolucion = o.NotasDiagnostico ?? "Diagnóstico y servicio técnico",
                MontoTotal = o.PrecioFinal > 0 ? o.PrecioFinal : o.PrecioEstimado,
                MetodosPagoTexto = o.Pago != null ? string.Join(", ", o.Pago.Detalles.Select(d => d.MetodoPago.ToString())) : (o.Estado == EstadoReparacion.Entregada ? "Efectivo" : "Pendiente de cobro"),
                MontoEntregado = o.PrecioFinal > 0 ? o.PrecioFinal : o.PrecioEstimado,
                MontoVuelto = 0,
                EstadoOrden = o.Estado switch
                {
                    EstadoReparacion.EnReparacion => "En Taller",
                    EstadoReparacion.ListaParaEntrega => "Lista para Entrega",
                    EstadoReparacion.Entregada => "Entregada y Cobrada",
                    EstadoReparacion.Cancelada => "Cancelada",
                    _ => o.Estado.ToString()
                }
            };
            TicketRenderer.ImprimirTicketReparacion(ticket);
            MessageBox.Show($"Ticket de la orden {o.NumeroOrden} enviado a la impresora.", "Impresión Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else if (dialog == DialogResult.No)
        {
            string boleta = $"========================================\n" +
                            $"       VEYRA POS - SERVICIO TÉCNICO     \n" +
                            $"         COMPROBANTE / FACTURA          \n" +
                            $"========================================\n" +
                            $"N° ORDEN:     {o.NumeroOrden}\n" +
                            $"FECHA:        {o.FechaRecepcion.ToLocalTime():dd/MM/yyyy HH:mm}\n" +
                            $"ESTADO:       {o.Estado}\n" +
                            $"----------------------------------------\n" +
                            $"CLIENTE:      {o.Cliente?.NombreCompleto}\n" +
                            $"TELÉFONO:     {o.Cliente?.Telefono ?? "—"}\n" +
                            $"----------------------------------------\n" +
                            $"EQUIPO:       {o.Marca} {o.Modelo}\n" +
                            $"IMEI / SERIE: {o.ImeiOSerie ?? "—"}\n" +
                            $"FALLA:        {o.DescripcionProblema}\n" +
                            $"DIAGNÓSTICO:  {o.NotasDiagnostico ?? "En evaluación"}\n" +
                            $"----------------------------------------\n" +
                            $"TOTAL:        RD${(o.PrecioFinal > 0 ? o.PrecioFinal : o.PrecioEstimado):N2}\n" +
                            $"========================================\n" +
                            $"  Garantía de 30 días en servicio técnico \n";

            MessageBox.Show(boleta, $"Comprobante de Servicio - {o.NumeroOrden}", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
