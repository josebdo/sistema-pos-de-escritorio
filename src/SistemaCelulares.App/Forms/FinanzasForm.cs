using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Constants;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class FinanzasForm : Form
{
    private readonly IFinanzasService _finanzasService;
    private readonly SesionUsuario _sesion;

    // KPI Cards
    private Label _lblKpiIngresos = null!;
    private Label _lblKpiGastosOperativos = null!;
    private Label _lblKpiComprasMercancia = null!;
    private Label _lblKpiUtilidadNeta = null!;

    // Filtros
    private ComboBox _cboPeriodoRapido = null!;
    private DateTimePicker _dtpDesde = null!;
    private DateTimePicker _dtpHasta = null!;
    private ComboBox _cboFiltroTipo = null!;
    private Button _btnFiltrar = null!;

    // Acciones
    private Button _btnRegistrarGasto = null!;
    private Button _btnRegistrarIngreso = null!;
    private Button _btnGestionarCategorias = null!;

    // Grilla
    private DataGridView _dgvMovimientos = null!;
    private Label _lblTotalRegistros = null!;

    public FinanzasForm(IFinanzasService finanzasService, SesionUsuario sesion)
    {
        _finanzasService = finanzasService;
        _sesion = sesion;

        InitializeCustomComponents();
        Load += async (s, e) =>
        {
            SetPeriodoEsteMes();
            await CargarFinanzasAsync();
        };
    }

    private void InitializeCustomComponents()
    {
        Text = "Finanzas y Reporte de Ingresos / Gastos";
        Size = new Size(1180, 740);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = UITheme.AppBg;
        Font = UITheme.BodyFont;

        // 1. Top Header
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Color.White,
            Padding = new Padding(20, 10, 20, 10)
        };

        var pnlHeaderLeft = new Panel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            Padding = new Padding(0)
        };

        var lblTitulo = new Label
        {
            Text = "📊 Control Financiero del Negocio",
            Font = UITheme.TitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(0, 4),
            AutoSize = true
        };
        pnlHeaderLeft.Controls.Add(lblTitulo);

        var lblSubtitulo = new Label
        {
            Text = "Gastos operativos, compras, ingresos adicionales y balance neto en RD$",
            Font = UITheme.SmallFont,
            ForeColor = Color.Gray,
            Location = new Point(2, 32),
            AutoSize = true
        };
        pnlHeaderLeft.Controls.Add(lblSubtitulo);
        headerPanel.Controls.Add(pnlHeaderLeft);

        // Action buttons on top right
        var panelTopButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };

        _btnRegistrarGasto = new Button
        {
            Text = "➖ Registrar Gasto",
            Size = new Size(140, 36),
            Margin = new Padding(6, 0, 0, 0)
        };
        UITheme.AplicarBotonSecundario(_btnRegistrarGasto);
        _btnRegistrarGasto.ForeColor = UITheme.Danger;
        _btnRegistrarGasto.Click += async (s, e) => await AbrirModalRegistrarAsync(TipoMovimientoFinanciero.Gasto);
        panelTopButtons.Controls.Add(_btnRegistrarGasto);

        _btnRegistrarIngreso = new Button
        {
            Text = "➕ Registrar Ingreso",
            Size = new Size(145, 36),
            Margin = new Padding(6, 0, 0, 0)
        };
        UITheme.AplicarBotonPrimario(_btnRegistrarIngreso);
        _btnRegistrarIngreso.Click += async (s, e) => await AbrirModalRegistrarAsync(TipoMovimientoFinanciero.Ingreso);
        panelTopButtons.Controls.Add(_btnRegistrarIngreso);

        _btnGestionarCategorias = new Button
        {
            Text = "🏷️ Categorías",
            Size = new Size(115, 36),
            Margin = new Padding(6, 0, 0, 0)
        };
        UITheme.AplicarBotonSecundario(_btnGestionarCategorias);
        _btnGestionarCategorias.Click += async (s, e) => await AbrirModalCategoriasAsync();
        panelTopButtons.Controls.Add(_btnGestionarCategorias);
        headerPanel.Controls.Add(panelTopButtons);

        // 2. KPI Panel
        var kpiPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 100,
            BackColor = UITheme.AppBg,
            Padding = new Padding(20, 10, 20, 10)
        };

        var kpiTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1
        };
        kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        kpiPanel.Controls.Add(kpiTable);

        kpiTable.Controls.Add(CrearCardKpi("INGRESOS TOTALES", "RD$ 0.00", UITheme.Success, out _lblKpiIngresos), 0, 0);
        kpiTable.Controls.Add(CrearCardKpi("GASTOS OPERATIVOS", "RD$ 0.00", UITheme.Danger, out _lblKpiGastosOperativos), 1, 0);
        kpiTable.Controls.Add(CrearCardKpi("COMPRAS INVENTARIO", "RD$ 0.00", UITheme.Warning, out _lblKpiComprasMercancia), 2, 0);
        kpiTable.Controls.Add(CrearCardKpi("UTILIDAD / BALANCE", "RD$ 0.00", UITheme.Primary, out _lblKpiUtilidadNeta), 3, 0);

        // 3. Filter Bar Panel
        var filterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            BackColor = Color.White,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(20, 8, 20, 4)
        };

        var lblFiltroPeriodo = new Label { Text = "Periodo:", AutoSize = true, Font = UITheme.SmallFont, Margin = new Padding(0, 6, 6, 0) };
        filterPanel.Controls.Add(lblFiltroPeriodo);

        _cboPeriodoRapido = new ComboBox
        {
            Size = new Size(115, 26),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(0, 2, 10, 0)
        };
        _cboPeriodoRapido.Items.AddRange(new object[] { "Hoy", "Esta Semana", "Este Mes", "Todo el Año", "Personalizado" });
        _cboPeriodoRapido.SelectedIndex = 2; // Este Mes
        _cboPeriodoRapido.SelectedIndexChanged += (s, e) => OnPeriodoRapidoCambiado();
        filterPanel.Controls.Add(_cboPeriodoRapido);

        var lblDesde = new Label { Text = "Desde:", AutoSize = true, Font = UITheme.SmallFont, Margin = new Padding(0, 6, 6, 0) };
        filterPanel.Controls.Add(lblDesde);

        _dtpDesde = new DateTimePicker { Size = new Size(110, 26), Format = DateTimePickerFormat.Short, Margin = new Padding(0, 2, 10, 0) };
        filterPanel.Controls.Add(_dtpDesde);

        var lblHasta = new Label { Text = "Hasta:", AutoSize = true, Font = UITheme.SmallFont, Margin = new Padding(0, 6, 6, 0) };
        filterPanel.Controls.Add(lblHasta);

        _dtpHasta = new DateTimePicker { Size = new Size(110, 26), Format = DateTimePickerFormat.Short, Margin = new Padding(0, 2, 10, 0) };
        filterPanel.Controls.Add(_dtpHasta);

        var lblTipo = new Label { Text = "Tipo:", AutoSize = true, Font = UITheme.SmallFont, Margin = new Padding(0, 6, 6, 0) };
        filterPanel.Controls.Add(lblTipo);

        _cboFiltroTipo = new ComboBox
        {
            Size = new Size(115, 26),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(0, 2, 10, 0)
        };
        _cboFiltroTipo.Items.AddRange(new object[] { "Todos", "Solo Gastos", "Solo Ingresos" });
        _cboFiltroTipo.SelectedIndex = 0;
        filterPanel.Controls.Add(_cboFiltroTipo);

        _btnFiltrar = new Button
        {
            Text = "🔍 Filtrar",
            Size = new Size(100, 30),
            Margin = new Padding(0, 0, 0, 0)
        };
        UITheme.AplicarBotonSecundario(_btnFiltrar);
        _btnFiltrar.Click += async (s, e) => await CargarFinanzasAsync();
        filterPanel.Controls.Add(_btnFiltrar);

        // 4. Grid Container Panel
        var mainContainer = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 10, 20, 15),
            BackColor = UITheme.AppBg
        };

        // Orden de Docking correcto en WinForms (Fill primero, luego Tops de abajo hacia arriba)
        Controls.Add(mainContainer);
        Controls.Add(filterPanel);
        Controls.Add(kpiPanel);
        Controls.Add(headerPanel);

        var gridCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(1)
        };
        mainContainer.Controls.Add(gridCard);

        _dgvMovimientos = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        UITheme.EstilizarDataGridView(_dgvMovimientos);
        gridCard.Controls.Add(_dgvMovimientos);

        // Bottom status panel
        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 35,
            BackColor = Color.White,
            Padding = new Padding(15, 5, 15, 5)
        };
        gridCard.Controls.Add(bottomPanel);

        _lblTotalRegistros = new Label
        {
            Text = "Mostrando 0 movimientos",
            Font = UITheme.SmallFont,
            ForeColor = Color.Gray,
            Dock = DockStyle.Left,
            AutoSize = true
        };
        bottomPanel.Controls.Add(_lblTotalRegistros);

        ConfigurarColumnasGrid();
        AplicarPermisos();
    }

    private Control CrearCardKpi(string titulo, string valorInicial, Color accentColor, out Label lblValor)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(6),
            Padding = new Padding(12)
        };

        var borderBar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 5,
            BackColor = accentColor
        };
        panel.Controls.Add(borderBar);

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10, 2, 5, 2)
        };
        panel.Controls.Add(content);

        var lblTit = new Label
        {
            Text = titulo,
            Font = UITheme.SmallFont,
            ForeColor = Color.Gray,
            Dock = DockStyle.Top,
            Height = 18
        };
        content.Controls.Add(lblTit);

        lblValor = new Label
        {
            Text = valorInicial,
            Font = UITheme.SubtitleFont,
            ForeColor = UITheme.DarkBg,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        content.Controls.Add(lblValor);

        return panel;
    }

    private void ConfigurarColumnasGrid()
    {
        _dgvMovimientos.Columns.Clear();

        _dgvMovimientos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fecha", HeaderText = "Fecha", FillWeight = 25 });
        _dgvMovimientos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tipo", HeaderText = "Tipo", FillWeight = 20 });
        _dgvMovimientos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Categoria", HeaderText = "Categoría", FillWeight = 35 });
        _dgvMovimientos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Monto", HeaderText = "Monto RD$", FillWeight = 30 });
        _dgvMovimientos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Comprobante", HeaderText = "No. Comprobante", FillWeight = 30 });
        _dgvMovimientos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Concepto", HeaderText = "Concepto / Detalle", FillWeight = 55 });
        _dgvMovimientos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Usuario", HeaderText = "Registrado Por", FillWeight = 30 });
    }

    private void AplicarPermisos()
    {
        var puedeRegistrar = _sesion.TienePermiso(Permisos.FinanzasMovimientosRegistrar);
        _btnRegistrarGasto.Visible = puedeRegistrar;
        _btnRegistrarIngreso.Visible = puedeRegistrar;
        _btnGestionarCategorias.Visible = puedeRegistrar;
    }

    private void OnPeriodoRapidoCambiado()
    {
        var hoy = DateTime.Today;
        switch (_cboPeriodoRapido.SelectedIndex)
        {
            case 0: // Hoy
                _dtpDesde.Value = hoy;
                _dtpHasta.Value = hoy;
                break;
            case 1: // Esta Semana
                var diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
                _dtpDesde.Value = hoy.AddDays(-diff);
                _dtpHasta.Value = hoy;
                break;
            case 2: // Este Mes
                SetPeriodoEsteMes();
                break;
            case 3: // Todo el Año
                _dtpDesde.Value = new DateTime(hoy.Year, 1, 1);
                _dtpHasta.Value = new DateTime(hoy.Year, 12, 31);
                break;
        }
    }

    private void SetPeriodoEsteMes()
    {
        var hoy = DateTime.Today;
        _dtpDesde.Value = new DateTime(hoy.Year, hoy.Month, 1);
        _dtpHasta.Value = new DateTime(hoy.Year, hoy.Month, DateTime.DaysInMonth(hoy.Year, hoy.Month));
    }

    private async Task CargarFinanzasAsync()
    {
        try
        {
            var desde = _dtpDesde.Value.Date;
            var hasta = _dtpHasta.Value.Date;

            TipoMovimientoFinanciero? filtroTipo = _cboFiltroTipo.SelectedIndex switch
            {
                1 => TipoMovimientoFinanciero.Gasto,
                2 => TipoMovimientoFinanciero.Ingreso,
                _ => null
            };

            // 1. Cargar Balance KPI
            var balance = await _finanzasService.ObtenerBalanceNetoPeriodoAsync(desde, hasta);

            _lblKpiIngresos.Text = balance.TotalIngresosGlobales.ToString("C2", new CultureInfo("es-DO"));
            _lblKpiGastosOperativos.Text = balance.TotalGastosOperativos.ToString("C2", new CultureInfo("es-DO"));
            _lblKpiComprasMercancia.Text = balance.TotalComprasMercancia.ToString("C2", new CultureInfo("es-DO"));
            _lblKpiUtilidadNeta.Text = balance.BalanceNeto.ToString("C2", new CultureInfo("es-DO"));

            if (balance.BalanceNeto >= 0)
            {
                _lblKpiUtilidadNeta.ForeColor = UITheme.Success;
            }
            else
            {
                _lblKpiUtilidadNeta.ForeColor = UITheme.Danger;
            }

            // 2. Cargar Movimientos Grilla
            var movimientos = await _finanzasService.ObtenerMovimientosAsync(
                desde: desde,
                hasta: hasta.AddDays(1).AddTicks(-1),
                tipo: filtroTipo
            );

            _dgvMovimientos.Rows.Clear();
            foreach (var m in movimientos)
            {
                var idx = _dgvMovimientos.Rows.Add(
                    m.Fecha.ToString("dd/MM/yyyy hh:mm tt"),
                    m.Tipo.ToString(),
                    m.CategoriaFinanciera?.Nombre ?? "Sin Categoría",
                    m.Monto.ToString("C2", new CultureInfo("es-DO")),
                    m.NumeroComprobante ?? "-",
                    m.Descripcion,
                    m.Usuario?.NombreCompleto ?? "-"
                );

                if (m.Tipo == TipoMovimientoFinanciero.Gasto)
                {
                    _dgvMovimientos.Rows[idx].Cells["Tipo"].Style.ForeColor = UITheme.Danger;
                    _dgvMovimientos.Rows[idx].Cells["Monto"].Style.ForeColor = UITheme.Danger;
                }
                else
                {
                    _dgvMovimientos.Rows[idx].Cells["Tipo"].Style.ForeColor = UITheme.Success;
                    _dgvMovimientos.Rows[idx].Cells["Monto"].Style.ForeColor = UITheme.Success;
                }
            }

            _lblTotalRegistros.Text = $"Mostrando {movimientos.Count} movimientos en el periodo seleccionado";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar información financiera: {ex.Message}", "Error de Carga", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task AbrirModalRegistrarAsync(TipoMovimientoFinanciero tipo)
    {
        using var modal = new RegistrarMovimientoModalForm(_finanzasService, _sesion.UsuarioId, tipo);
        if (modal.ShowDialog(this) == DialogResult.OK)
        {
            await CargarFinanzasAsync();
        }
    }

    private async Task AbrirModalCategoriasAsync()
    {
        using var modal = new CategoriasFinancierasModalForm(_finanzasService);
        modal.ShowDialog(this);
        await CargarFinanzasAsync();
    }
}
