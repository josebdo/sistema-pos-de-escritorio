using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;

namespace SistemaCelulares.App.Forms;

public class RegistrarMovimientoModalForm : Form
{
    private readonly IFinanzasService _finanzasService;
    private readonly int _usuarioId;
    private readonly TipoMovimientoFinanciero? _tipoInicial;

    private ComboBox _cboTipo = null!;
    private ComboBox _cboCategoria = null!;
    private NumericUpDown _numMonto = null!;
    private DateTimePicker _dtpFecha = null!;
    private TextBox _txtDescripcion = null!;
    private TextBox _txtNumeroComprobante = null!;
    private Label _lblError = null!;
    private Button _btnGuardar = null!;

    public RegistrarMovimientoModalForm(
        IFinanzasService finanzasService,
        int usuarioId,
        TipoMovimientoFinanciero? tipoInicial = null)
    {
        _finanzasService = finanzasService;
        _usuarioId = usuarioId;
        _tipoInicial = tipoInicial;

        InitializeCustomComponents();
        Load += async (s, e) => await CargarCategoriasAsync();
    }

    private void InitializeCustomComponents()
    {
        Text = "Registrar Movimiento Financiero";
        Size = new Size(520, 560);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.AppBg;
        Font = UITheme.BodyFont;

        var card = new Panel
        {
            Size = new Size(460, 480),
            Location = new Point(22, 16),
            BackColor = Color.White,
            Padding = new Padding(20)
        };
        Controls.Add(card);

        var lblTitulo = new Label
        {
            Text = "Nuevo Movimiento de Finanzas",
            Font = UITheme.SubtitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(20, 15),
            AutoSize = true
        };
        card.Controls.Add(lblTitulo);

        // Tipo de movimiento
        var lblTipo = new Label { Text = "Tipo de Movimiento *", Font = UITheme.SectionFont, Location = new Point(20, 50), AutoSize = true };
        card.Controls.Add(lblTipo);

        _cboTipo = new ComboBox
        {
            Location = new Point(20, 72),
            Size = new Size(200, 28),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cboTipo.Items.Add(TipoMovimientoFinanciero.Gasto);
        _cboTipo.Items.Add(TipoMovimientoFinanciero.Ingreso);
        _cboTipo.SelectedItem = _tipoInicial ?? TipoMovimientoFinanciero.Gasto;
        _cboTipo.SelectedIndexChanged += async (s, e) => await CargarCategoriasAsync();
        card.Controls.Add(_cboTipo);

        // Fecha
        var lblFecha = new Label { Text = "Fecha del Movimiento *", Font = UITheme.SectionFont, Location = new Point(230, 50), AutoSize = true };
        card.Controls.Add(lblFecha);

        _dtpFecha = new DateTimePicker
        {
            Location = new Point(230, 72),
            Size = new Size(210, 28),
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today
        };
        card.Controls.Add(_dtpFecha);

        // Categoría Financiera
        var lblCat = new Label { Text = "Categoría Financiera *", Font = UITheme.SectionFont, Location = new Point(20, 115), AutoSize = true };
        card.Controls.Add(lblCat);

        _cboCategoria = new ComboBox
        {
            Location = new Point(20, 137),
            Size = new Size(420, 28),
            DropDownStyle = ComboBoxStyle.DropDownList,
            DisplayMember = "Nombre",
            ValueMember = "Id"
        };
        card.Controls.Add(_cboCategoria);

        // Monto en RD$
        var lblMonto = new Label { Text = "Monto en RD$ (DOP) *", Font = UITheme.SectionFont, Location = new Point(20, 180), AutoSize = true };
        card.Controls.Add(lblMonto);

        _numMonto = new NumericUpDown
        {
            Location = new Point(20, 202),
            Size = new Size(200, 28),
            DecimalPlaces = 2,
            ThousandsSeparator = true,
            Minimum = 0.01m,
            Maximum = 10000000m,
            Value = 100.00m
        };
        card.Controls.Add(_numMonto);

        // Comprobante / Factura #
        var lblComp = new Label { Text = "No. Comprobante / Factura (opcional)", Font = UITheme.SectionFont, Location = new Point(230, 180), AutoSize = true };
        card.Controls.Add(lblComp);

        _txtNumeroComprobante = new TextBox
        {
            Location = new Point(230, 202),
            Size = new Size(210, 28)
        };
        card.Controls.Add(_txtNumeroComprobante);

        // Concepto / Descripción
        var lblDesc = new Label { Text = "Concepto / Detalle del Movimiento *", Font = UITheme.SectionFont, Location = new Point(20, 245), AutoSize = true };
        card.Controls.Add(lblDesc);

        _txtDescripcion = new TextBox
        {
            Location = new Point(20, 267),
            Size = new Size(420, 60),
            Multiline = true
        };
        card.Controls.Add(_txtDescripcion);

        // Error
        _lblError = new Label
        {
            Text = string.Empty,
            Font = UITheme.SmallFont,
            ForeColor = UITheme.Danger,
            Location = new Point(20, 335),
            Size = new Size(420, 25)
        };
        card.Controls.Add(_lblError);

        // Botones
        var panelBotones = new FlowLayoutPanel
        {
            Location = new Point(20, 370),
            Size = new Size(420, 45),
            FlowDirection = FlowDirection.RightToLeft
        };

        var btnCancelar = new Button { Text = "Cancelar", Size = new Size(95, 38), DialogResult = DialogResult.Cancel };
        UITheme.AplicarBotonSecundario(btnCancelar);
        panelBotones.Controls.Add(btnCancelar);

        _btnGuardar = new Button { Text = "💾 Registrar", Size = new Size(130, 38) };
        UITheme.AplicarBotonPrimario(_btnGuardar);
        _btnGuardar.Click += async (s, e) => await GuardarMovimientoAsync();
        panelBotones.Controls.Add(_btnGuardar);

        card.Controls.Add(panelBotones);
    }

    private async Task CargarCategoriasAsync()
    {
        if (_cboTipo.SelectedItem is not TipoMovimientoFinanciero tipo) return;

        try
        {
            var categorias = await _finanzasService.ObtenerCategoriasFinancierasAsync(tipo, soloActivas: true);
            _cboCategoria.DataSource = categorias;
        }
        catch (Exception ex)
        {
            _lblError.Text = "Error al cargar categorías: " + ex.Message;
        }
    }

    private async Task GuardarMovimientoAsync()
    {
        _lblError.Text = string.Empty;

        if (_cboTipo.SelectedItem is not TipoMovimientoFinanciero tipo)
        {
            _lblError.Text = "Seleccione un tipo de movimiento válido.";
            return;
        }

        if (_cboCategoria.SelectedItem is not CategoriaFinanciera categoria)
        {
            _lblError.Text = "Debe seleccionar una categoría financiera.";
            return;
        }

        var monto = _numMonto.Value;
        if (monto <= 0)
        {
            _lblError.Text = "El monto debe ser mayor a RD$ 0.00.";
            return;
        }

        var concepto = _txtDescripcion.Text.Trim();
        if (string.IsNullOrWhiteSpace(concepto))
        {
            _lblError.Text = "El concepto o detalle del movimiento es obligatorio.";
            return;
        }

        var comprobante = _txtNumeroComprobante.Text.Trim();

        _btnGuardar.Enabled = false;
        try
        {
            await _finanzasService.RegistrarMovimientoAsync(
                tipo,
                categoria.Id,
                monto,
                _dtpFecha.Value.Date,
                concepto,
                _usuarioId,
                string.IsNullOrEmpty(comprobante) ? null : comprobante
            );

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _lblError.Text = ex.Message;
        }
        finally
        {
            _btnGuardar.Enabled = true;
        }
    }
}
