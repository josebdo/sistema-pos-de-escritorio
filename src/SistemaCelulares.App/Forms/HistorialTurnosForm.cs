using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Constants;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class HistorialTurnosForm : Form
{
    private readonly ITurnoService _turnoService;
    private readonly IUsuarioService _usuarioService;
    private readonly IVentaService _ventaService;
    private readonly SesionUsuario _sesionActual;

    private Panel _cardTurnoActual = null!;
    private Label _lblEstadoTurno = null!;
    private Label _lblDetalleTurno = null!;
    private Button _btnAccionTurno = null!;

    private DateTimePicker _dtpDesde = null!;
    private DateTimePicker _dtpHasta = null!;
    private ComboBox _cbUsuarios = null!;
    private Button _btnVerDetalle = null!;
    private DataGridView _gridTurnos = null!;
    private List<Turno> _listaTurnos = new();
    private Turno? _turnoAbiertoActual;

    public HistorialTurnosForm(
        ITurnoService turnoService,
        IUsuarioService usuarioService,
        IVentaService ventaService,
        SesionUsuario sesionActual)
    {
        _turnoService = turnoService;
        _usuarioService = usuarioService;
        _ventaService = ventaService;
        _sesionActual = sesionActual;

        InitializeCustomComponents();
        Load += async (s, e) =>
        {
            await CargarUsuariosFiltroAsync();
            await RecargarTodoAsync();
        };
    }

    private void InitializeCustomComponents()
    {
        Text = "Gestión de Caja, Arqueos e Historial de Turnos — Veyra POS";
        Size = new Size(1100, 700);
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
            Text = "Control de Caja, Turnos y Arqueos",
            Font = UITheme.TitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(0, 5),
            AutoSize = true
        };
        header.Controls.Add(lblTitulo);

        var lblSub = new Label
        {
            Text = "Apertura y cierre de caja, auditoría de quién abrió/cerró, ventas realizadas y control de déficit o sobrante",
            Font = UITheme.SmallFont,
            ForeColor = UITheme.TextMuted,
            Location = new Point(0, 35),
            AutoSize = true
        };
        header.Controls.Add(lblSub);
        panelPrincipal.Controls.Add(header);

        // Card de Turno Actual del Usuario
        _cardTurnoActual = new Panel
        {
            Dock = DockStyle.Top,
            Height = 85,
            BackColor = Color.White,
            Padding = new Padding(15),
            Margin = new Padding(0, 0, 0, 15)
        };

        var panelTurnoTexto = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 0, 10, 0)
        };

        _lblEstadoTurno = new Label
        {
            Text = "Estado de su turno actual:",
            Font = UITheme.SectionFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(0, 5),
            AutoSize = true
        };
        panelTurnoTexto.Controls.Add(_lblEstadoTurno);

        _lblDetalleTurno = new Label
        {
            Text = "Verificando estado de caja...",
            Font = UITheme.BodyFont,
            ForeColor = UITheme.TextMuted,
            Location = new Point(0, 32),
            AutoSize = true
        };
        panelTurnoTexto.Controls.Add(_lblDetalleTurno);
        _cardTurnoActual.Controls.Add(panelTurnoTexto);

        _btnAccionTurno = new Button
        {
            Text = "🔓 Abrir Turno",
            Size = new Size(160, 44),
            Dock = DockStyle.Right
        };
        UITheme.AplicarBotonPrimario(_btnAccionTurno);
        _btnAccionTurno.Click += async (s, e) => await EjecutarAccionTurnoAsync();
        _cardTurnoActual.Controls.Add(_btnAccionTurno);

        panelPrincipal.Controls.Add(_cardTurnoActual);

        // Barra de Filtros y Acciones
        var flowFiltros = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 8, 0, 8)
        };

        var lblDesde = new Label { Text = "Desde:", AutoSize = true, Font = UITheme.BodyFont, Margin = new Padding(0, 6, 6, 4) };
        flowFiltros.Controls.Add(lblDesde);

        _dtpDesde = new DateTimePicker { Size = new Size(125, 28), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-14), Margin = new Padding(0, 2, 12, 4) };
        _dtpDesde.ValueChanged += async (s, e) => await RecargarHistorialAsync();
        flowFiltros.Controls.Add(_dtpDesde);

        var lblHasta = new Label { Text = "Hasta:", AutoSize = true, Font = UITheme.BodyFont, Margin = new Padding(0, 6, 6, 4) };
        flowFiltros.Controls.Add(lblHasta);

        _dtpHasta = new DateTimePicker { Size = new Size(125, 28), Format = DateTimePickerFormat.Short, Value = DateTime.Today, Margin = new Padding(0, 2, 12, 4) };
        _dtpHasta.ValueChanged += async (s, e) => await RecargarHistorialAsync();
        flowFiltros.Controls.Add(_dtpHasta);

        var lblUser = new Label { Text = "Cajero:", AutoSize = true, Font = UITheme.BodyFont, Margin = new Padding(0, 6, 6, 4) };
        flowFiltros.Controls.Add(lblUser);

        _cbUsuarios = new ComboBox { Size = new Size(170, 28), DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 2, 12, 4) };
        _cbUsuarios.SelectedIndexChanged += async (s, e) => await RecargarHistorialAsync();
        flowFiltros.Controls.Add(_cbUsuarios);

        var btnRefrescar = new Button { Text = "🔄 Recargar", Size = new Size(105, 32), Margin = new Padding(0, 0, 8, 4) };
        UITheme.AplicarBotonSecundario(btnRefrescar);
        btnRefrescar.Click += async (s, e) => await RecargarTodoAsync();
        flowFiltros.Controls.Add(btnRefrescar);

        _btnVerDetalle = new Button { Text = "🔍 Ver Ventas del Turno", Size = new Size(185, 32), Margin = new Padding(0, 0, 8, 4) };
        UITheme.AplicarBotonPrimario(_btnVerDetalle);
        _btnVerDetalle.Click += (s, e) => AbrirDetalleVentasTurnoSeleccionado();
        flowFiltros.Controls.Add(_btnVerDetalle);

        // DataGridView de Historial
        var panelGrid = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(1)
        };

        _gridTurnos = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 34 }
        };
        UITheme.EstilizarDataGridView(_gridTurnos);
        ConfigurarColumnasGrid();
        _gridTurnos.DoubleClick += (s, e) => AbrirDetalleVentasTurnoSeleccionado();
        panelGrid.Controls.Add(_gridTurnos);

        // Jerarquía de Acoplamiento ordenada
        panelPrincipal.Controls.Add(panelGrid);         // Fill
        panelPrincipal.Controls.Add(flowFiltros);        // Top 3
        panelPrincipal.Controls.Add(_cardTurnoActual);   // Top 2
        panelPrincipal.Controls.Add(header);             // Top 1
    }

    private void ConfigurarColumnasGrid()
    {
        _gridTurnos.Columns.Clear();
        _gridTurnos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "# Turno", FillWeight = 65 });
        _gridTurnos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", HeaderText = "Estado", FillWeight = 85 });
        _gridTurnos.Columns.Add(new DataGridViewTextBoxColumn { Name = "CajeroApertura", HeaderText = "Apertura Por", FillWeight = 110 });
        _gridTurnos.Columns.Add(new DataGridViewTextBoxColumn { Name = "FechaApertura", HeaderText = "Fecha Apertura", FillWeight = 115 });
        _gridTurnos.Columns.Add(new DataGridViewTextBoxColumn { Name = "MontoApertura", HeaderText = "Fondo Inicial", FillWeight = 90 });
        _gridTurnos.Columns.Add(new DataGridViewTextBoxColumn { Name = "CajeroCierre", HeaderText = "Cierre Por", FillWeight = 110 });
        _gridTurnos.Columns.Add(new DataGridViewTextBoxColumn { Name = "FechaCierre", HeaderText = "Fecha Cierre", FillWeight = 115 });
        _gridTurnos.Columns.Add(new DataGridViewTextBoxColumn { Name = "VentasEfectivo", HeaderText = "Ventas Efec.", FillWeight = 90 });
        _gridTurnos.Columns.Add(new DataGridViewTextBoxColumn { Name = "MontoEsperado", HeaderText = "Esperado", FillWeight = 90 });
        _gridTurnos.Columns.Add(new DataGridViewTextBoxColumn { Name = "MontoCierre", HeaderText = "Contado", FillWeight = 90 });
        _gridTurnos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Diferencia", HeaderText = "Déficit / Cuadre", FillWeight = 110 });
    }

    private async Task CargarUsuariosFiltroAsync()
    {
        var usuarios = await _usuarioService.ObtenerTodosAsync(incluirInactivos: false);
        var listaCombo = new List<UsuarioFiltroItem>
        {
            new(0, "-- Todos los cajeros --")
        };
        listaCombo.AddRange(usuarios.Select(u => new UsuarioFiltroItem(u.Id, u.NombreCompleto)));

        _cbUsuarios.DisplayMember = nameof(UsuarioFiltroItem.Nombre);
        _cbUsuarios.ValueMember = nameof(UsuarioFiltroItem.Id);
        _cbUsuarios.DataSource = listaCombo;
    }

    private async Task RecargarTodoAsync()
    {
        // 1. Estado del turno actual del usuario logueado
        _turnoAbiertoActual = await _turnoService.ObtenerTurnoAbiertoAsync(_sesionActual.UsuarioId);
        if (_turnoAbiertoActual != null)
        {
            _lblEstadoTurno.Text = $"🟢 Caja Abierta por {_sesionActual.NombreCompleto} (Turno #{_turnoAbiertoActual.Id})";
            _lblEstadoTurno.ForeColor = UITheme.Success;
            var esperado = _turnoAbiertoActual.MontoApertura + _turnoAbiertoActual.TotalVentasEfectivo;
            _lblDetalleTurno.Text = $"Apertura: {_turnoAbiertoActual.FechaApertura.ToLocalTime():dd/MM/yyyy hh:mm tt} | Fondo Inicial: {AppCulture.FormatearMoneda(_turnoAbiertoActual.MontoApertura)} | Ventas Efectivo: {AppCulture.FormatearMoneda(_turnoAbiertoActual.TotalVentasEfectivo)} | Total Esperado: {AppCulture.FormatearMoneda(esperado)}";
            _btnAccionTurno.Text = "🔒 Cerrar Caja / Turno";
            UITheme.AplicarBotonPeligro(_btnAccionTurno);
            _btnAccionTurno.Visible = _sesionActual.TienePermiso(Permisos.TurnosCerrar);
        }
        else
        {
            _lblEstadoTurno.Text = "🔴 Caja Cerrada — No tiene un turno activo";
            _lblEstadoTurno.ForeColor = UITheme.Danger;
            _lblDetalleTurno.Text = "Para empezar a facturar en el Punto de Venta (POS) debe abrir un turno con su fondo inicial en caja.";
            _btnAccionTurno.Text = "🔓 Abrir Turno de Caja";
            UITheme.AplicarBotonPrimario(_btnAccionTurno);
            _btnAccionTurno.Visible = _sesionActual.TienePermiso(Permisos.TurnosAbrir);
        }

        // 2. Historial
        await RecargarHistorialAsync();
    }

    private async Task RecargarHistorialAsync()
    {
        var desde = _dtpDesde.Value.Date;
        var hasta = _dtpHasta.Value.Date;
        int? usuarioId = null;

        if (_cbUsuarios.SelectedValue is int uid && uid > 0)
        {
            usuarioId = uid;
        }

        _listaTurnos = await _turnoService.ObtenerHistorialTurnosAsync(desde, hasta, usuarioId);

        _gridTurnos.Rows.Clear();
        foreach (var t in _listaTurnos)
        {
            string diffStr;
            Color diffColor = UITheme.TextPrimary;

            if (t.Estado == TurnoEstado.Abierto)
            {
                diffStr = "🟢 En Curso";
                diffColor = UITheme.Success;
            }
            else if (t.Diferencia.HasValue)
            {
                if (t.Diferencia.Value < 0)
                {
                    diffStr = $"🔴 Faltante: {AppCulture.FormatearMoneda(t.Diferencia.Value)}";
                    diffColor = UITheme.Danger;
                }
                else if (t.Diferencia.Value > 0)
                {
                    diffStr = $"🔵 Sobrante: +{AppCulture.FormatearMoneda(t.Diferencia.Value)}";
                    diffColor = UITheme.Primary;
                }
                else
                {
                    diffStr = "🟢 Cuadrada (RD$0.00)";
                    diffColor = UITheme.Success;
                }
            }
            else
            {
                diffStr = "-";
            }

            var rowIndex = _gridTurnos.Rows.Add(
                $"#{t.Id}",
                t.Estado == TurnoEstado.Abierto ? "🟢 ABIERTA" : "⚪ CERRADA",
                t.UsuarioApertura?.NombreCompleto ?? "Usuario #" + t.UsuarioAperturaId,
                t.FechaApertura.ToLocalTime().ToString("dd/MM/yyyy hh:mm tt"),
                AppCulture.FormatearMoneda(t.MontoApertura),
                t.UsuarioCierre?.NombreCompleto ?? (t.Estado == TurnoEstado.Abierto ? "En Curso" : "-"),
                t.FechaCierre.HasValue ? t.FechaCierre.Value.ToLocalTime().ToString("dd/MM/yyyy hh:mm tt") : "-",
                AppCulture.FormatearMoneda(t.TotalVentasEfectivo),
                t.Estado == TurnoEstado.Cerrado ? AppCulture.FormatearMoneda(t.MontoEsperado) : AppCulture.FormatearMoneda(t.MontoApertura + t.TotalVentasEfectivo),
                t.MontoCierre.HasValue ? AppCulture.FormatearMoneda(t.MontoCierre.Value) : "-",
                diffStr
            );

            var row = _gridTurnos.Rows[rowIndex];
            row.Cells["Diferencia"].Style.ForeColor = diffColor;
            row.Cells["Diferencia"].Style.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

            if (t.Estado == TurnoEstado.Abierto)
            {
                row.Cells["Estado"].Style.ForeColor = UITheme.Success;
                row.Cells["Estado"].Style.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            }
        }
    }

    private void AbrirDetalleVentasTurnoSeleccionado()
    {
        if (_gridTurnos.SelectedRows.Count == 0)
        {
            MessageBox.Show("Por favor seleccione un turno de la lista para ver su detalle de ventas y arqueo.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var rowIndex = _gridTurnos.SelectedRows[0].Index;
        if (rowIndex >= 0 && rowIndex < _listaTurnos.Count)
        {
            var turnoSeleccionado = _listaTurnos[rowIndex];
            using var modal = new DetalleTurnoVentasModalForm(turnoSeleccionado, _ventaService);
            modal.ShowDialog(this);
        }
    }

    private async Task EjecutarAccionTurnoAsync()
    {
        if (_turnoAbiertoActual != null)
        {
            // Cerrar turno
            var modal = new CerrarTurnoModalForm(_turnoService, _sesionActual, _turnoAbiertoActual);
            if (modal.ShowDialog(this) == DialogResult.OK)
            {
                await RecargarTodoAsync();
            }
        }
        else
        {
            // Abrir turno
            var modal = new AbrirTurnoModalForm(_turnoService, _sesionActual);
            if (modal.ShowDialog(this) == DialogResult.OK)
            {
                await RecargarTodoAsync();
            }
        }
    }

    private record UsuarioFiltroItem(int Id, string Nombre);
}
