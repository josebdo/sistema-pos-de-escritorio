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
    private readonly SesionUsuario _sesionActual;

    private Panel _cardTurnoActual = null!;
    private Label _lblEstadoTurno = null!;
    private Label _lblDetalleTurno = null!;
    private Button _btnAccionTurno = null!;

    private DateTimePicker _dtpDesde = null!;
    private DateTimePicker _dtpHasta = null!;
    private ComboBox _cbUsuarios = null!;
    private DataGridView _gridTurnos = null!;
    private List<Turno> _listaTurnos = new();
    private Turno? _turnoAbiertoActual;

    public HistorialTurnosForm(
        ITurnoService turnoService,
        IUsuarioService usuarioService,
        SesionUsuario sesionActual)
    {
        _turnoService = turnoService;
        _usuarioService = usuarioService;
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
        Text = "Gestión y Arqueo de Turnos de Caja";
        Size = new Size(1000, 650);
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
            Text = "Caja y Turnos de Trabajo",
            Font = UITheme.TitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(0, 5),
            AutoSize = true
        };
        header.Controls.Add(lblTitulo);

        var lblSub = new Label
        {
            Text = "Apertura, control de flujo de efectivo en tiempo real y arqueo de cierre de turnos",
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

        _lblEstadoTurno = new Label
        {
            Text = "Estado de su turno actual:",
            Font = UITheme.SectionFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(15, 15),
            AutoSize = true
        };
        _cardTurnoActual.Controls.Add(_lblEstadoTurno);

        _lblDetalleTurno = new Label
        {
            Text = "Verificando...",
            Font = UITheme.BodyFont,
            ForeColor = UITheme.TextMuted,
            Location = new Point(15, 42),
            Size = new Size(650, 25)
        };
        _cardTurnoActual.Controls.Add(_lblDetalleTurno);

        _btnAccionTurno = new Button
        {
            Text = "Abrir Turno",
            Size = new Size(140, 42),
            Location = new Point(780, 20),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        UITheme.AplicarBotonPrimario(_btnAccionTurno);
        _btnAccionTurno.Click += async (s, e) => await EjecutarAccionTurnoAsync();
        _cardTurnoActual.Controls.Add(_btnAccionTurno);

        panelPrincipal.Controls.Add(_cardTurnoActual);

        // Barra de Filtros
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 55 };

        var lblDesde = new Label { Text = "Desde:", Location = new Point(0, 15), AutoSize = true, Font = UITheme.BodyFont };
        toolbar.Controls.Add(lblDesde);

        _dtpDesde = new DateTimePicker { Location = new Point(50, 12), Size = new Size(130, 28), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-7) };
        _dtpDesde.ValueChanged += async (s, e) => await RecargarHistorialAsync();
        toolbar.Controls.Add(_dtpDesde);

        var lblHasta = new Label { Text = "Hasta:", Location = new Point(190, 15), AutoSize = true, Font = UITheme.BodyFont };
        toolbar.Controls.Add(lblHasta);

        _dtpHasta = new DateTimePicker { Location = new Point(240, 12), Size = new Size(130, 28), Format = DateTimePickerFormat.Short, Value = DateTime.Today };
        _dtpHasta.ValueChanged += async (s, e) => await RecargarHistorialAsync();
        toolbar.Controls.Add(_dtpHasta);

        var lblUser = new Label { Text = "Cajero:", Location = new Point(385, 15), AutoSize = true, Font = UITheme.BodyFont };
        toolbar.Controls.Add(lblUser);

        _cbUsuarios = new ComboBox { Location = new Point(440, 12), Size = new Size(180, 28), DropDownStyle = ComboBoxStyle.DropDownList };
        _cbUsuarios.SelectedIndexChanged += async (s, e) => await RecargarHistorialAsync();
        toolbar.Controls.Add(_cbUsuarios);

        var btnRefrescar = new Button { Text = "🔄 Recargar", Location = new Point(635, 10), Size = new Size(100, 34) };
        UITheme.AplicarBotonSecundario(btnRefrescar);
        btnRefrescar.Click += async (s, e) => await RecargarTodoAsync();
        toolbar.Controls.Add(btnRefrescar);

        panelPrincipal.Controls.Add(toolbar);

        // DataGridView de Historial
        var panelGrid = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(1)
        };

        _gridTurnos = new DataGridView { Dock = DockStyle.Fill };
        UITheme.EstilizarDataGridView(_gridTurnos);
        ConfigurarColumnasGrid();
        panelGrid.Controls.Add(_gridTurnos);

        panelPrincipal.Controls.Add(panelGrid);
    }

    private void ConfigurarColumnasGrid()
    {
        _gridTurnos.Columns.Clear();
        _gridTurnos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "# Turno", Width = 75 });
        _gridTurnos.Columns.Add(new DataGridViewTextBoxColumn { Name = "CajeroApertura", HeaderText = "Apertura Por", FillWeight = 110 });
        _gridTurnos.Columns.Add(new DataGridViewTextBoxColumn { Name = "FechaApertura", HeaderText = "Fecha Apertura", FillWeight = 120 });
        _gridTurnos.Columns.Add(new DataGridViewTextBoxColumn { Name = "MontoApertura", HeaderText = "Monto Inicial", FillWeight = 90 });
        _gridTurnos.Columns.Add(new DataGridViewTextBoxColumn { Name = "VentasEfectivo", HeaderText = "Ventas Efec.", FillWeight = 90 });
        _gridTurnos.Columns.Add(new DataGridViewTextBoxColumn { Name = "MontoEsperado", HeaderText = "Esperado", FillWeight = 90 });
        _gridTurnos.Columns.Add(new DataGridViewTextBoxColumn { Name = "MontoCierre", HeaderText = "Contado", FillWeight = 90 });
        _gridTurnos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Diferencia", HeaderText = "Diferencia", FillWeight = 90 });
        _gridTurnos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", HeaderText = "Estado", FillWeight = 80 });
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
            _lblEstadoTurno.Text = $"🟢 Turno Abierto (#{_turnoAbiertoActual.Id})";
            _lblEstadoTurno.ForeColor = UITheme.Success;
            var esperado = _turnoAbiertoActual.MontoApertura + _turnoAbiertoActual.TotalVentasEfectivo;
            _lblDetalleTurno.Text = $"Apertura: {_turnoAbiertoActual.FechaApertura.ToLocalTime():dd/MM/yyyy hh:mm tt} | Inicial: {AppCulture.FormatearMoneda(_turnoAbiertoActual.MontoApertura)} | Ventas: {AppCulture.FormatearMoneda(_turnoAbiertoActual.TotalVentasEfectivo)} | Total Esperado: {AppCulture.FormatearMoneda(esperado)}";
            _btnAccionTurno.Text = "Cerrar Turno";
            UITheme.AplicarBotonPeligro(_btnAccionTurno);
            _btnAccionTurno.Visible = _sesionActual.TienePermiso(Permisos.TurnosCerrar);
        }
        else
        {
            _lblEstadoTurno.Text = "🔴 No tiene un turno de caja abierto";
            _lblEstadoTurno.ForeColor = UITheme.Danger;
            _lblDetalleTurno.Text = "Debe abrir un turno con el fondo inicial en caja antes de registrar ventas.";
            _btnAccionTurno.Text = "Abrir Turno";
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
            var diffStr = "-";
            if (t.Diferencia.HasValue)
            {
                diffStr = AppCulture.FormatearMoneda(t.Diferencia.Value);
            }

            var rowIndex = _gridTurnos.Rows.Add(
                $"#{t.Id}",
                t.UsuarioApertura?.NombreCompleto ?? "Desconocido",
                t.FechaApertura.ToLocalTime().ToString("dd/MM/yyyy hh:mm tt"),
                AppCulture.FormatearMoneda(t.MontoApertura),
                AppCulture.FormatearMoneda(t.TotalVentasEfectivo),
                t.Estado == TurnoEstado.Cerrado ? AppCulture.FormatearMoneda(t.MontoEsperado) : AppCulture.FormatearMoneda(t.MontoApertura + t.TotalVentasEfectivo),
                t.MontoCierre.HasValue ? AppCulture.FormatearMoneda(t.MontoCierre.Value) : "-",
                diffStr,
                t.Estado == TurnoEstado.Abierto ? "🟢 Abierto" : "⚪ Cerrado"
            );

            var row = _gridTurnos.Rows[rowIndex];
            if (t.Diferencia.HasValue)
            {
                if (t.Diferencia.Value < 0)
                    row.Cells["Diferencia"].Style.ForeColor = UITheme.Danger;
                else if (t.Diferencia.Value > 0)
                    row.Cells["Diferencia"].Style.ForeColor = UITheme.Warning;
                else
                    row.Cells["Diferencia"].Style.ForeColor = UITheme.Success;
            }
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
