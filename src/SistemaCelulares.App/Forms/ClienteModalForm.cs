using SistemaCelulares.Core.Entities;

namespace SistemaCelulares.App.Forms;

public class ClienteModalForm : Form
{
    private readonly Cliente? _clienteExistente;
    public Cliente? ClienteGuardado { get; private set; }

    private TextBox _txtNombre = null!;
    private TextBox _txtTelefono = null!;
    private TextBox _txtRncOCedula = null!;
    private TextBox _txtEmail = null!;
    private TextBox _txtDireccion = null!;
    private CheckBox _chkEsFrecuente = null!;
    private NumericUpDown _numDescuento = null!;
    private Button _btnGuardar = null!;
    private Button _btnCancelar = null!;
    private Label _lblError = null!;

    public ClienteModalForm(Cliente? clienteExistente = null, string? telefonoInicial = null, string? nombreInicial = null)
    {
        _clienteExistente = clienteExistente;
        InitializeComponent();

        if (_clienteExistente != null)
        {
            Text = "Editar Cliente";
            _txtNombre.Text = _clienteExistente.NombreCompleto;
            _txtTelefono.Text = _clienteExistente.Telefono ?? string.Empty;
            _txtRncOCedula.Text = _clienteExistente.RncOCedula ?? string.Empty;
            _txtEmail.Text = _clienteExistente.Email ?? string.Empty;
            _txtDireccion.Text = _clienteExistente.Direccion ?? string.Empty;
            _chkEsFrecuente.Checked = _clienteExistente.EsFrecuente;
            _numDescuento.Value = _clienteExistente.PorcentajeDescuento;
            _numDescuento.Enabled = _clienteExistente.EsFrecuente;
        }
        else
        {
            Text = "Nuevo Cliente";
            if (!string.IsNullOrWhiteSpace(telefonoInicial)) _txtTelefono.Text = telefonoInicial;
            if (!string.IsNullOrWhiteSpace(nombreInicial)) _txtNombre.Text = nombreInicial;
        }
    }

    private void InitializeComponent()
    {
        Size = new Size(520, 500);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        BackColor = Color.FromArgb(248, 249, 250);

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Color.FromArgb(26, 35, 126),
            Padding = new Padding(20, 12, 20, 10)
        };

        var lblHeader = new Label
        {
            Text = _clienteExistente == null ? "REGISTRAR NUEVO CLIENTE" : "EDITAR DATOS DE CLIENTE",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true
        };

        var lblSubheader = new Label
        {
            Text = "Gestione los datos del cliente, contacto y descuentos preferenciales",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(197, 202, 233),
            Location = new Point(20, 36),
            AutoSize = true
        };

        pnlHeader.Controls.Add(lblHeader);
        pnlHeader.Controls.Add(lblSubheader);
        Controls.Add(pnlHeader);

        int top = 80;
        int left = 30;
        int width = 445;

        // Nombre Completo
        var lblNombre = new Label { Text = "Nombre Completo *", Location = new Point(left, top), AutoSize = true, ForeColor = Color.FromArgb(33, 33, 33), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
        _txtNombre = new TextBox { Location = new Point(left, top + 22), Width = width, Height = 28 };
        Controls.Add(lblNombre);
        Controls.Add(_txtNombre);
        top += 60;

        // Teléfono y RNC/Cédula
        var lblTel = new Label { Text = "Teléfono / WhatsApp *", Location = new Point(left, top), AutoSize = true, ForeColor = Color.FromArgb(33, 33, 33), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
        _txtTelefono = new TextBox { Location = new Point(left, top + 22), Width = 215, Height = 28, PlaceholderText = "809-555-0000" };

        var lblRnc = new Label { Text = "RNC / Cédula *", Location = new Point(left + 230, top), AutoSize = true, ForeColor = Color.FromArgb(33, 33, 33), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
        _txtRncOCedula = new TextBox { Location = new Point(left + 230, top + 22), Width = 215, Height = 28, PlaceholderText = "001-0000000-0" };

        Controls.Add(lblTel);
        Controls.Add(_txtTelefono);
        Controls.Add(lblRnc);
        Controls.Add(_txtRncOCedula);
        top += 60;

        // Email
        var lblEmail = new Label { Text = "Correo Electrónico *", Location = new Point(left, top), AutoSize = true, ForeColor = Color.FromArgb(33, 33, 33), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
        _txtEmail = new TextBox { Location = new Point(left, top + 22), Width = width, Height = 28, PlaceholderText = "cliente@correo.com" };
        Controls.Add(lblEmail);
        Controls.Add(_txtEmail);
        top += 60;

        // Dirección
        var lblDir = new Label { Text = "Dirección *", Location = new Point(left, top), AutoSize = true, ForeColor = Color.FromArgb(33, 33, 33), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
        _txtDireccion = new TextBox { Location = new Point(left, top + 22), Width = width, Height = 28, PlaceholderText = "Calle, Sector, Ciudad" };
        Controls.Add(lblDir);
        Controls.Add(_txtDireccion);
        top += 60;

        // Cliente Frecuente & Descuento
        var pnlFrecuente = new GroupBox
        {
            Text = "Programa de Fidelidad y Descuentos (Opcional)",
            Location = new Point(left, top),
            Size = new Size(width, 70),
            ForeColor = Color.FromArgb(26, 35, 126),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        _chkEsFrecuente = new CheckBox
        {
            Text = "Cliente Frecuente (VIP)",
            Location = new Point(15, 28),
            AutoSize = true,
            ForeColor = Color.FromArgb(33, 33, 33),
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
        };
        _chkEsFrecuente.CheckedChanged += (s, e) => _numDescuento.Enabled = _chkEsFrecuente.Checked;

        var lblPorc = new Label
        {
            Text = "Descuento Sugerido (%):",
            Location = new Point(220, 30),
            AutoSize = true,
            ForeColor = Color.FromArgb(33, 33, 33),
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
        };

        _numDescuento = new NumericUpDown
        {
            Location = new Point(375, 27),
            Width = 55,
            Minimum = 0,
            Maximum = 100,
            DecimalPlaces = 0,
            Value = 0,
            Enabled = false,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
        };

        pnlFrecuente.Controls.Add(_chkEsFrecuente);
        pnlFrecuente.Controls.Add(lblPorc);
        pnlFrecuente.Controls.Add(_numDescuento);
        Controls.Add(pnlFrecuente);
        top += 78;

        // Label de Error
        _lblError = new Label
        {
            Location = new Point(left, top),
            Size = new Size(width, 22),
            ForeColor = Color.Red,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Visible = false
        };
        Controls.Add(_lblError);
        top += 25;

        // Botones
        _btnGuardar = new Button
        {
            Text = "Guardar Cliente",
            Location = new Point(left + 165, top),
            Size = new Size(150, 36),
            BackColor = Color.FromArgb(26, 35, 126),
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
            Location = new Point(left + 325, top),
            Size = new Size(120, 36),
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

    private void BtnGuardar_Click(object? sender, EventArgs e)
    {
        _lblError.Visible = false;

        if (string.IsNullOrWhiteSpace(_txtNombre.Text))
        {
            MessageBox.Show("El nombre completo del cliente es obligatorio.", "Campo Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtNombre.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtTelefono.Text))
        {
            MessageBox.Show("El teléfono / WhatsApp del cliente es obligatorio.", "Campo Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtTelefono.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtRncOCedula.Text))
        {
            MessageBox.Show("El RNC o Cédula del cliente es obligatorio.", "Campo Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtRncOCedula.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtEmail.Text))
        {
            MessageBox.Show("El correo electrónico del cliente es obligatorio.", "Campo Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtEmail.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtDireccion.Text))
        {
            MessageBox.Show("La dirección del cliente es obligatoria.", "Campo Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtDireccion.Focus();
            return;
        }

        ClienteGuardado = _clienteExistente ?? new Cliente { Activo = true };
        ClienteGuardado.NombreCompleto = _txtNombre.Text.Trim();
        ClienteGuardado.Telefono = _txtTelefono.Text.Trim();
        ClienteGuardado.RncOCedula = _txtRncOCedula.Text.Trim();
        ClienteGuardado.Email = _txtEmail.Text.Trim();
        ClienteGuardado.Direccion = _txtDireccion.Text.Trim();
        ClienteGuardado.EsFrecuente = _chkEsFrecuente.Checked;
        ClienteGuardado.PorcentajeDescuento = _chkEsFrecuente.Checked ? _numDescuento.Value : 0m;
        ClienteGuardado.Activo = true;

        DialogResult = DialogResult.OK;
        Close();
    }
}
