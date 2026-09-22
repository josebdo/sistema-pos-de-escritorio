using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class OrdenReparacionModalForm : Form
{
    private readonly IClienteService _clienteService;
    private readonly IReparacionService _reparacionService;
    private readonly SesionUsuario _sesion;
    private readonly OrdenReparacion? _ordenExistente;

    public OrdenReparacion? OrdenGuardada { get; private set; }

    private ComboBox _cbClientes = null!;
    private Button _btnNuevoCliente = null!;
    private TextBox _txtMarca = null!;
    private TextBox _txtModelo = null!;
    private TextBox _txtImei = null!;
    private TextBox _txtProblema = null!;
    private NumericUpDown _numPrecioEstimado = null!;
    private NumericUpDown _numPrecioFinal = null!;
    private Label _lblError = null!;
    private Button _btnGuardar = null!;
    private Button _btnCancelar = null!;

    public OrdenReparacionModalForm(
        IClienteService clienteService,
        IReparacionService reparacionService,
        SesionUsuario sesion,
        OrdenReparacion? ordenExistente = null)
    {
        _clienteService = clienteService;
        _reparacionService = reparacionService;
        _sesion = sesion;
        _ordenExistente = ordenExistente;

        InitializeComponent();
        Load += async (s, e) =>
        {
            await CargarClientesAsync();
            if (_ordenExistente != null)
            {
                CargarDatosOrden();
            }
        };
    }

    private void InitializeComponent()
    {
        Text = _ordenExistente == null ? "Recepción de Equipo - Nueva Orden de Reparación" : $"Editar Orden {_ordenExistente.NumeroOrden}";
        Size = new Size(580, 560);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        BackColor = Color.FromArgb(248, 249, 250);

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Color.FromArgb(0, 105, 92),
            Padding = new Padding(20, 12, 20, 10)
        };

        var lblTitulo = new Label
        {
            Text = _ordenExistente == null ? "TALLER: RECEPCIÓN DE CELULAR" : $"EDITAR ORDEN: {_ordenExistente.NumeroOrden}",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Registre los datos del equipo recibido del cliente, falla y presupuesto",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(178, 223, 219),
            Location = new Point(20, 36),
            AutoSize = true
        };

        pnlHeader.Controls.Add(lblTitulo);
        pnlHeader.Controls.Add(lblSub);
        Controls.Add(pnlHeader);

        int top = 80;
        int left = 30;
        int width = 505;

        // Cliente
        var lblCli = new Label { Text = "Cliente *", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
        _cbClientes = new ComboBox { Location = new Point(left, top + 22), Width = 380, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList };
        _btnNuevoCliente = new Button
        {
            Text = "➕ Nuevo",
            Location = new Point(left + 390, top + 20),
            Size = new Size(115, 30),
            BackColor = Color.FromArgb(0, 105, 92),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnNuevoCliente.FlatAppearance.BorderSize = 0;
        _btnNuevoCliente.Click += BtnNuevoCliente_Click;

        Controls.Add(lblCli);
        Controls.Add(_cbClientes);
        Controls.Add(_btnNuevoCliente);
        top += 58;

        // Marca y Modelo
        var lblMarca = new Label { Text = "Marca del Equipo *", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
        _txtMarca = new TextBox { Location = new Point(left, top + 22), Width = 245, Height = 28, PlaceholderText = "Samsung, iPhone, Xiaomi..." };

        var lblModelo = new Label { Text = "Modelo / Color *", Location = new Point(left + 260, top), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
        _txtModelo = new TextBox { Location = new Point(left + 260, top + 22), Width = 245, Height = 28, PlaceholderText = "Galaxy A54 Azul, iPhone 11..." };

        Controls.Add(lblMarca);
        Controls.Add(_txtMarca);
        Controls.Add(lblModelo);
        Controls.Add(_txtModelo);
        top += 58;

        // IMEI / Serie (Obligatorio)
        var lblImei = new Label { Text = "IMEI o Número de Serie del Equipo *", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
        _txtImei = new TextBox { Location = new Point(left, top + 22), Width = width, Height = 28, PlaceholderText = "IMEI de 15 dígitos o número de serie del equipo..." };
        Controls.Add(lblImei);
        Controls.Add(_txtImei);
        top += 58;

        // Descripción de la falla
        var lblProb = new Label { Text = "Problema / Falla Reportada por el Cliente *", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
        _txtProblema = new TextBox { Location = new Point(left, top + 22), Width = width, Height = 48, Multiline = true, PlaceholderText = "Pantalla rota, no enciende, no carga, cambio de batería..." };
        Controls.Add(lblProb);
        Controls.Add(_txtProblema);
        top += 76;

        // Precios
        var lblEstimado = new Label { Text = "Precio Estimado (RD$):", Location = new Point(left, top), AutoSize = true };
        _numPrecioEstimado = new NumericUpDown
        {
            Location = new Point(left, top + 22),
            Width = 245,
            Height = 28,
            DecimalPlaces = 2,
            ThousandsSeparator = true,
            Maximum = 1000000m,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };

        var lblFinal = new Label { Text = "Precio Final Acordado (RD$):", Location = new Point(left + 260, top), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
        _numPrecioFinal = new NumericUpDown
        {
            Location = new Point(left + 260, top + 22),
            Width = 245,
            Height = 28,
            DecimalPlaces = 2,
            ThousandsSeparator = true,
            Maximum = 1000000m,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };

        Controls.Add(lblEstimado);
        Controls.Add(_numPrecioEstimado);
        Controls.Add(lblFinal);
        Controls.Add(_numPrecioFinal);
        top += 58;

        // Error
        _lblError = new Label
        {
            Location = new Point(left, top),
            Size = new Size(width, 22),
            ForeColor = Color.Red,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Visible = false
        };
        Controls.Add(_lblError);
        top += 28;

        // Botones
        _btnGuardar = new Button
        {
            Text = _ordenExistente == null ? "Registrar Recepción" : "Guardar Cambios",
            Location = new Point(left + 150, top),
            Size = new Size(180, 36),
            BackColor = Color.FromArgb(0, 105, 92),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnGuardar.FlatAppearance.BorderSize = 0;
        _btnGuardar.Click += BtnGuardar_Click;

        _btnCancelar = new Button
        {
            Text = "Cancelar",
            Location = new Point(left + 340, top),
            Size = new Size(110, 36),
            BackColor = Color.FromArgb(224, 224, 224),
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5F),
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.Cancel
        };
        _btnCancelar.FlatAppearance.BorderSize = 0;

        Controls.Add(_btnGuardar);
        Controls.Add(_btnCancelar);

        AcceptButton = _btnGuardar;
        CancelButton = _btnCancelar;
    }

    private async Task CargarClientesAsync()
    {
        try
        {
            var clientes = await _clienteService.ObtenerTodosAsync(soloActivos: true);
            _cbClientes.DataSource = clientes;
            _cbClientes.DisplayMember = "NombreCompleto";
            _cbClientes.ValueMember = "Id";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar clientes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CargarDatosOrden()
    {
        if (_ordenExistente == null) return;

        _cbClientes.SelectedValue = _ordenExistente.ClienteId;
        _txtMarca.Text = _ordenExistente.Marca;
        _txtModelo.Text = _ordenExistente.Modelo;
        _txtImei.Text = _ordenExistente.ImeiOSerie ?? string.Empty;
        _txtProblema.Text = _ordenExistente.DescripcionProblema;
        _numPrecioEstimado.Value = _ordenExistente.PrecioEstimado;
        _numPrecioFinal.Value = _ordenExistente.PrecioFinal;
    }

    private async void BtnNuevoCliente_Click(object? sender, EventArgs e)
    {
        using var modal = new ClienteModalForm();
        if (modal.ShowDialog(this) == DialogResult.OK && modal.ClienteGuardado != null)
        {
            try
            {
                var nuevo = await _clienteService.CrearAsync(modal.ClienteGuardado);
                await CargarClientesAsync();
                _cbClientes.SelectedValue = nuevo.Id;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear cliente: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async void BtnGuardar_Click(object? sender, EventArgs e)
    {
        _lblError.Visible = false;

        if (_cbClientes.SelectedValue == null)
        {
            MessageBox.Show("Debe seleccionar un cliente.", "Campo Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _cbClientes.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtMarca.Text))
        {
            MessageBox.Show("La marca del equipo es obligatoria (ej: Samsung, iPhone, Xiaomi).", "Campo Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtMarca.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtModelo.Text))
        {
            MessageBox.Show("El modelo del equipo es obligatorio (ej: Galaxy A54, iPhone 11).", "Campo Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtModelo.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtImei.Text))
        {
            MessageBox.Show("El IMEI o número de serie del equipo es obligatorio.", "Campo Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtImei.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtProblema.Text))
        {
            MessageBox.Show("La descripción del problema o falla reportada es obligatoria.", "Campo Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtProblema.Focus();
            return;
        }

        try
        {
            var clienteId = (int)_cbClientes.SelectedValue;
            var marca = _txtMarca.Text.Trim();
            var modelo = _txtModelo.Text.Trim();
            var imei = _txtImei.Text.Trim();
            var problema = _txtProblema.Text.Trim();
            var estimado = _numPrecioEstimado.Value;
            var final = _numPrecioFinal.Value;

            if (_ordenExistente == null)
            {
                var nuevaOrden = new OrdenReparacion
                {
                    ClienteId = clienteId,
                    Marca = marca,
                    Modelo = modelo,
                    ImeiOSerie = imei,
                    DescripcionProblema = problema,
                    PrecioEstimado = estimado,
                    PrecioFinal = final > 0 ? final : estimado,
                    UsuarioRecepcionId = _sesion.UsuarioId
                };

                OrdenGuardada = await _reparacionService.RegistrarRecepcionAsync(nuevaOrden);
            }
            else
            {
                _ordenExistente.ClienteId = clienteId;
                _ordenExistente.Marca = marca;
                _ordenExistente.Modelo = modelo;
                _ordenExistente.ImeiOSerie = imei;
                _ordenExistente.DescripcionProblema = problema;
                _ordenExistente.PrecioEstimado = estimado;
                _ordenExistente.PrecioFinal = final;

                await _reparacionService.ActualizarDiagnosticoAsync(_ordenExistente.Id, _ordenExistente.NotasDiagnostico ?? string.Empty, final);
                OrdenGuardada = _ordenExistente;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar orden de reparación: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
