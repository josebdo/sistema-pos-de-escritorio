using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;

namespace SistemaCelulares.App.Forms;

public class DetalleTurnoVentasModalForm : Form
{
    private readonly Turno _turno;
    private readonly IVentaService _ventaService;

    private DataGridView _gridVentas = null!;
    private Label _lblResumenVentas = null!;
    private List<Venta> _ventasDelTurno = new();

    public DetalleTurnoVentasModalForm(Turno turno, IVentaService ventaService)
    {
        _turno = turno;
        _ventaService = ventaService;

        InitializeCustomComponents();
        Load += async (s, e) => await CargarVentasTurnoAsync();
    }

    private void InitializeCustomComponents()
    {
        Text = $"Detalle y Arqueo del Turno #{_turno.Id} — Veyra POS";
        Size = new Size(1000, 680);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = true;
        MinimizeBox = false;
        BackColor = UITheme.AppBg;
        Font = UITheme.BodyFont;

        var panelPrincipal = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };
        Controls.Add(panelPrincipal);

        // Header Superior
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 60 };
        var lblTitulo = new Label
        {
            Text = $"Resumen de Caja — Turno #{_turno.Id} ({(_turno.Estado == TurnoEstado.Abierto ? "🟢 ABIERTO / EN CURSO" : "⚪ CERRADO")})",
            Font = UITheme.TitleFont,
            ForeColor = _turno.Estado == TurnoEstado.Abierto ? UITheme.Success : UITheme.DarkBg,
            Location = new Point(0, 4),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblTitulo);

        var fechaAperturaStr = _turno.FechaApertura.ToLocalTime().ToString("dd/MM/yyyy hh:mm tt");
        var fechaCierreStr = _turno.FechaCierre.HasValue ? _turno.FechaCierre.Value.ToLocalTime().ToString("dd/MM/yyyy hh:mm tt") : "Aún en operación";
        var cajeroApertura = _turno.UsuarioApertura?.NombreCompleto ?? "Usuario #" + _turno.UsuarioAperturaId;
        var cajeroCierre = _turno.UsuarioCierre?.NombreCompleto ?? (_turno.Estado == TurnoEstado.Abierto ? "En curso" : "-");

        var lblSub = new Label
        {
            Text = $"Abierto por: {cajeroApertura} ({fechaAperturaStr})  |  Cerrado por: {cajeroCierre} ({fechaCierreStr})",
            Font = UITheme.SmallFont,
            ForeColor = UITheme.TextMuted,
            Location = new Point(0, 34),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblSub);

        // Tarjetas de Métricas de Arqueo y Dinero
        var pnlMetricas = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 85,
            ColumnCount = 5,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 10)
        };
        pnlMetricas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
        pnlMetricas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
        pnlMetricas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
        pnlMetricas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
        pnlMetricas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));

        var cardInicial = CrearCardMetrica("Fondo Inicial", AppCulture.FormatearMoneda(_turno.MontoApertura), UITheme.Primary);
        var cardVentasEfec = CrearCardMetrica("Ventas en Efectivo", AppCulture.FormatearMoneda(_turno.TotalVentasEfectivo), UITheme.Pro);
        
        decimal esperado = _turno.Estado == TurnoEstado.Cerrado ? _turno.MontoEsperado : (_turno.MontoApertura + _turno.TotalVentasEfectivo);
        var cardEsperado = CrearCardMetrica("Efectivo Esperado", AppCulture.FormatearMoneda(esperado), UITheme.DarkBg);
        
        string contadoStr = _turno.MontoCierre.HasValue ? AppCulture.FormatearMoneda(_turno.MontoCierre.Value) : "En Curso";
        var cardContado = CrearCardMetrica("Efectivo Contado", contadoStr, UITheme.TextPrimary);

        string diffTexto;
        Color diffColor;
        if (_turno.Diferencia.HasValue)
        {
            if (_turno.Diferencia.Value < 0)
            {
                diffTexto = $"🔴 Faltante: {AppCulture.FormatearMoneda(_turno.Diferencia.Value)}";
                diffColor = UITheme.Danger;
            }
            else if (_turno.Diferencia.Value > 0)
            {
                diffTexto = $"🔵 Sobrante: +{AppCulture.FormatearMoneda(_turno.Diferencia.Value)}";
                diffColor = UITheme.Primary;
            }
            else
            {
                diffTexto = "🟢 Cuadrada (Exacta)";
                diffColor = UITheme.Success;
            }
        }
        else
        {
            diffTexto = "🟢 En Curso";
            diffColor = UITheme.Success;
        }

        var cardDiferencia = CrearCardMetrica("Cuadre / Déficit", diffTexto, diffColor);

        pnlMetricas.Controls.Add(cardInicial, 0, 0);
        pnlMetricas.Controls.Add(cardVentasEfec, 1, 0);
        pnlMetricas.Controls.Add(cardEsperado, 2, 0);
        pnlMetricas.Controls.Add(cardContado, 3, 0);
        pnlMetricas.Controls.Add(cardDiferencia, 4, 0);

        // Barra de Título de la Grilla de Ventas
        var pnlBarraGrid = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(0, 8, 0, 4) };
        _lblResumenVentas = new Label
        {
            Text = "🛒 Ventas realizadas en este turno:",
            Font = UITheme.SubtitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(0, 6),
            AutoSize = true
        };
        pnlBarraGrid.Controls.Add(_lblResumenVentas);

        // Panel Inferior con Observaciones y Botón Cerrar
        var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 65, Padding = new Padding(0, 10, 0, 0) };

        var lblObs = new Label
        {
            Text = $"Obs. Apertura: {_turno.ObservacionesApertura ?? "Ninguna"}  |  Obs. Cierre: {_turno.ObservacionesCierre ?? "Ninguna"}",
            Font = UITheme.SmallFont,
            ForeColor = UITheme.TextSecondary,
            Location = new Point(0, 15),
            AutoSize = true
        };
        pnlBottom.Controls.Add(lblObs);

        var btnCerrar = new Button
        {
            Text = "Cerrar Vista",
            Size = new Size(130, 38),
            Dock = DockStyle.Right,
            DialogResult = DialogResult.OK
        };
        UITheme.AplicarBotonSecundario(btnCerrar);
        pnlBottom.Controls.Add(btnCerrar);

        // DataGridView de Ventas
        var panelGrid = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(1)
        };

        _gridVentas = new DataGridView
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
        UITheme.EstilizarDataGridView(_gridVentas);
        ConfigurarColumnasGrid();
        panelGrid.Controls.Add(_gridVentas);

        // Construcción de la jerarquía visual ordenada en WinForms
        panelPrincipal.Controls.Add(panelGrid);       // Fill
        panelPrincipal.Controls.Add(pnlBottom);       // Bottom
        panelPrincipal.Controls.Add(pnlBarraGrid);     // Top (3ro)
        panelPrincipal.Controls.Add(pnlMetricas);      // Top (2do)
        panelPrincipal.Controls.Add(pnlHeader);        // Top (1ro)
    }

    private void ConfigurarColumnasGrid()
    {
        _gridVentas.Columns.Clear();
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Factura", HeaderText = "No. Factura", FillWeight = 90 });
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Hora", HeaderText = "Hora", FillWeight = 75 });
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cliente", HeaderText = "Cliente", FillWeight = 110 });
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Productos", HeaderText = "Artículos / Celulares Vendidos", FillWeight = 220 });
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "MetodoPago", HeaderText = "Método Pago", FillWeight = 85 });
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Total", HeaderText = "Total Venta", FillWeight = 85 });
        _gridVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", HeaderText = "Estado", FillWeight = 75 });
    }

    private async Task CargarVentasTurnoAsync()
    {
        try
        {
            _ventasDelTurno = await _ventaService.ObtenerHistorialAsync(turnoId: _turno.Id);
            _gridVentas.Rows.Clear();

            decimal granTotalVentas = 0m;
            int totalItems = 0;

            foreach (var v in _ventasDelTurno)
            {
                granTotalVentas += v.Total;
                totalItems += v.Detalles.Sum(d => d.Cantidad);

                var itemsStr = string.Join("; ", v.Detalles.Select(d =>
                    $"{d.Cantidad}x {d.Producto?.Nombre ?? "Prod #" + d.ProductoId}" +
                    (!string.IsNullOrWhiteSpace(d.Imei) ? $" (IMEI: {d.Imei})" : string.Empty)
                ));

                var metodoPagoStr = v.Pago != null ? v.Pago.MetodoPrincipal.ToString() : "Efectivo";

                _gridVentas.Rows.Add(
                    v.NumeroFactura,
                    v.FechaVenta.ToLocalTime().ToString("hh:mm tt"),
                    v.Cliente?.NombreCompleto ?? v.NombreClienteAnonimo ?? "Consumidor Final",
                    itemsStr,
                    metodoPagoStr,
                    AppCulture.FormatearMoneda(v.Total),
                    v.Estado == EstadoVenta.Completada ? "✅ Completada" : "❌ " + v.Estado
                );
            }

            _lblResumenVentas.Text = $"🛒 Ventas del turno ({_ventasDelTurno.Count} transacciones, {totalItems} artículos vendidos, Total Facturado: {AppCulture.FormatearMoneda(granTotalVentas)})";
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error al cargar ventas del turno: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private Panel CrearCardMetrica(string titulo, string valor, Color colorValor)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(12, 10, 12, 10),
            Margin = new Padding(4)
        };

        var lblT = new Label
        {
            Text = titulo,
            Font = UITheme.SmallFont,
            ForeColor = UITheme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 18
        };

        var lblV = new Label
        {
            Text = valor,
            Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
            ForeColor = colorValor,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft
        };

        card.Controls.Add(lblV);
        card.Controls.Add(lblT);
        return card;
    }
}
