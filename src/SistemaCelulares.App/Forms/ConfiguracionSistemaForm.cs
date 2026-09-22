using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Constants;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class ConfiguracionSistemaForm : Form
{
    private readonly IConfiguracionNegocioService _configNegocioService;
    private readonly IBackupService _backupService;
    private readonly IConfiguracionRedService _configRedService;
    private readonly IRolService _rolService;
    private readonly SesionUsuario _sesion;

    // Controles Empresa
    private TextBox _txtNombreEmpresa = null!;
    private TextBox _txtRncCedula = null!;
    private TextBox _txtTelefono = null!;
    private TextBox _txtWhatsApp = null!;
    private TextBox _txtEmail = null!;
    private TextBox _txtDireccion = null!;
    private TextBox _txtCiudad = null!;
    private TextBox _txtMoneda = null!;
    private NumericUpDown _numItbis = null!;
    private TextBox _txtMensajePieFactura = null!;
    private TextBox _txtMensajeGarantia = null!;

    // Controles Backup
    private Label _lblRutaDb = null!;
    private Label _lblTamanoDb = null!;
    private Label _lblModifDb = null!;
    private DataGridView _dgvBackups = null!;

    // Controles Red
    private ComboBox _cmbModoRed = null!;
    private TextBox _txtServidorIp = null!;
    private NumericUpDown _numPuerto = null!;
    private NumericUpDown _numCajaId = null!;
    private Label _lblEstadoRed = null!;

    public ConfiguracionSistemaForm(
        IConfiguracionNegocioService configNegocioService,
        IBackupService backupService,
        IConfiguracionRedService configRedService,
        IRolService rolService,
        SesionUsuario sesion)
    {
        _configNegocioService = configNegocioService;
        _backupService = backupService;
        _configRedService = configRedService;
        _rolService = rolService;
        _sesion = sesion;

        InitializeCustomComponents();
        CargarDatosEmpresaAsync();
        CargarInfoBackup();
        CargarConfiguracionRed();
    }

    private void InitializeCustomComponents()
    {
        Text = "Configuración del Sistema y Negocio";
        Size = new Size(1100, 720);
        MinimumSize = new Size(950, 600);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = UITheme.Surface2;
        Font = UITheme.BodyFont;

        // Panel Encabezado
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = UITheme.Surface1,
            Padding = new Padding(24, 12, 24, 12)
        };

        var lblTitulo = new Label
        {
            Text = "⚙️  Configuración General del Negocio",
            Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
            ForeColor = UITheme.TextPrimary,
            Location = new Point(24, 10),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Personalice los datos de su empresa, copias de seguridad de la base de datos y parámetros de red.",
            Font = UITheme.BodyFont,
            ForeColor = UITheme.TextSecondary,
            Location = new Point(24, 34),
            AutoSize = true
        };

        pnlHeader.Controls.Add(lblTitulo);
        pnlHeader.Controls.Add(lblSub);

        var sepHeader = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = UITheme.Border };
        pnlHeader.Controls.Add(sepHeader);

        // TabControl Moderno
        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(16, 8),
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
        };

        // Tab 1: Datos de la Empresa
        var tabEmpresa = new TabPage("🏢  Datos del Negocio") { BackColor = UITheme.Surface2, AutoScroll = true, Padding = new Padding(20) };
        ConstruirPestañaEmpresa(tabEmpresa);
        tabControl.TabPages.Add(tabEmpresa);

        // Tab 2: Copias de Seguridad (Backup)
        var tabBackup = new TabPage("💾  Copias de Seguridad (Backup)") { BackColor = UITheme.Surface2, AutoScroll = true, Padding = new Padding(20) };
        ConstruirPestañaBackup(tabBackup);
        tabControl.TabPages.Add(tabBackup);

        // Tab 3: Red y Multicaja
        var tabRed = new TabPage("🌐  Red y Multicaja") { BackColor = UITheme.Surface2, AutoScroll = true, Padding = new Padding(20) };
        ConstruirPestañaRed(tabRed);
        tabControl.TabPages.Add(tabRed);

        Controls.Add(tabControl);
        Controls.Add(pnlHeader);
    }

    // ==========================================
    // TAB 1: DATOS DEL NEGOCIO
    // ==========================================
    private void ConstruirPestañaEmpresa(TabPage tab)
    {
        var pnlContenedor = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(10)
        };

        var grpGeneral = new GroupBox
        {
            Text = " Identificación y Contacto ",
            ForeColor = UITheme.Primary,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 220,
            BackColor = UITheme.Surface1,
            Padding = new Padding(16)
        };

        // Fila 1: Nombre Empresa & RNC
        var lblNom = new Label { Text = "Nombre Comercial del Negocio:", Location = new Point(20, 30), AutoSize = true, ForeColor = UITheme.TextPrimary, Font = UITheme.BodyBoldFont };
        _txtNombreEmpresa = new TextBox { Location = new Point(20, 52), Width = 460, Height = 28, Font = UITheme.BodyFont };

        var lblRnc = new Label { Text = "RNC o Cédula Fiscal:", Location = new Point(500, 30), AutoSize = true, ForeColor = UITheme.TextPrimary, Font = UITheme.BodyBoldFont };
        _txtRncCedula = new TextBox { Location = new Point(500, 52), Width = 280, Height = 28, Font = UITheme.BodyFont };

        // Fila 2: Teléfono & WhatsApp & Email
        var lblTel = new Label { Text = "Teléfono:", Location = new Point(20, 90), AutoSize = true, ForeColor = UITheme.TextPrimary, Font = UITheme.BodyFont };
        _txtTelefono = new TextBox { Location = new Point(20, 110), Width = 230, Height = 28, Font = UITheme.BodyFont };

        var lblWs = new Label { Text = "WhatsApp:", Location = new Point(265, 90), AutoSize = true, ForeColor = UITheme.TextPrimary, Font = UITheme.BodyFont };
        _txtWhatsApp = new TextBox { Location = new Point(265, 110), Width = 215, Height = 28, Font = UITheme.BodyFont };

        var lblEmail = new Label { Text = "Email de Contacto:", Location = new Point(500, 90), AutoSize = true, ForeColor = UITheme.TextPrimary, Font = UITheme.BodyFont };
        _txtEmail = new TextBox { Location = new Point(500, 110), Width = 280, Height = 28, Font = UITheme.BodyFont };

        // Fila 3: Dirección & Ciudad
        var lblDir = new Label { Text = "Dirección Física:", Location = new Point(20, 150), AutoSize = true, ForeColor = UITheme.TextPrimary, Font = UITheme.BodyFont };
        _txtDireccion = new TextBox { Location = new Point(20, 170), Width = 460, Height = 28, Font = UITheme.BodyFont };

        var lblCiu = new Label { Text = "Ciudad / Provincia:", Location = new Point(500, 150), AutoSize = true, ForeColor = UITheme.TextPrimary, Font = UITheme.BodyFont };
        _txtCiudad = new TextBox { Location = new Point(500, 170), Width = 280, Height = 28, Font = UITheme.BodyFont };

        grpGeneral.Controls.AddRange(new Control[] {
            lblNom, _txtNombreEmpresa, lblRnc, _txtRncCedula,
            lblTel, _txtTelefono, lblWs, _txtWhatsApp, lblEmail, _txtEmail,
            lblDir, _txtDireccion, lblCiu, _txtCiudad
        });

        // Grupo 2: Facturación e Impresión
        var grpTicket = new GroupBox
        {
            Text = " Textos para Facturas, Tickets y Garantías ",
            ForeColor = UITheme.Primary,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 230,
            BackColor = UITheme.Surface1,
            Padding = new Padding(16),
            Margin = new Padding(0, 16, 0, 0)
        };

        var lblPie = new Label { Text = "Mensaje al pie de Ticket / Factura de Venta:", Location = new Point(20, 30), AutoSize = true, ForeColor = UITheme.TextPrimary, Font = UITheme.BodyBoldFont };
        _txtMensajePieFactura = new TextBox { Location = new Point(20, 52), Width = 760, Height = 45, Multiline = true, Font = UITheme.BodyFont };

        var lblGar = new Label { Text = "Términos de Garantía para Órdenes de Reparación:", Location = new Point(20, 105), AutoSize = true, ForeColor = UITheme.TextPrimary, Font = UITheme.BodyBoldFont };
        _txtMensajeGarantia = new TextBox { Location = new Point(20, 127), Width = 760, Height = 45, Multiline = true, Font = UITheme.BodyFont };

        var lblMon = new Label { Text = "Símbolo Moneda:", Location = new Point(20, 182), AutoSize = true, ForeColor = UITheme.TextPrimary, Font = UITheme.BodyFont };
        _txtMoneda = new TextBox { Location = new Point(140, 180), Width = 90, Height = 26, Font = UITheme.BodyFont, Text = "RD$" };

        var lblItb = new Label { Text = "ITBIS por Defecto (%):", Location = new Point(260, 182), AutoSize = true, ForeColor = UITheme.TextPrimary, Font = UITheme.BodyFont };
        _numItbis = new NumericUpDown { Location = new Point(410, 180), Width = 80, Height = 26, DecimalPlaces = 2, Value = 18.00m, Font = UITheme.BodyFont };

        grpTicket.Controls.AddRange(new Control[] {
            lblPie, _txtMensajePieFactura, lblGar, _txtMensajeGarantia,
            lblMon, _txtMoneda, lblItb, _numItbis
        });

        // Panel de Botón Guardar
        var pnlGuardar = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(0, 14, 0, 0) };
        var btnGuardar = new Button
        {
            Text = "💾  Guardar Datos del Negocio",
            Size = new Size(260, 42),
            Location = new Point(20, 10),
            BackColor = UITheme.Primary,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnGuardar.FlatAppearance.BorderSize = 0;
        btnGuardar.Click += async (s, e) => await GuardarDatosEmpresaAsync();
        pnlGuardar.Controls.Add(btnGuardar);

        pnlContenedor.Controls.Add(pnlGuardar);
        pnlContenedor.Controls.Add(grpTicket);
        pnlContenedor.Controls.Add(grpGeneral);
        tab.Controls.Add(pnlContenedor);
    }

    private async void CargarDatosEmpresaAsync()
    {
        try
        {
            var config = await _configNegocioService.ObtenerConfiguracionAsync();
            _txtNombreEmpresa.Text = config.NombreEmpresa;
            _txtRncCedula.Text = config.RncCedula;
            _txtTelefono.Text = config.Telefono;
            _txtWhatsApp.Text = config.WhatsApp ?? string.Empty;
            _txtEmail.Text = config.Email ?? string.Empty;
            _txtDireccion.Text = config.Direccion;
            _txtCiudad.Text = config.Ciudad ?? string.Empty;
            _txtMensajePieFactura.Text = config.MensajePieFactura;
            _txtMensajeGarantia.Text = config.MensajeGarantiaReparacion;
            _txtMoneda.Text = config.MonedaSimbolo;
            _numItbis.Value = config.ItbisPorcentaje > 0 ? config.ItbisPorcentaje : 18.00m;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar configuración: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task GuardarDatosEmpresaAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtNombreEmpresa.Text))
        {
            MessageBox.Show("El nombre comercial del negocio es obligatorio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtNombreEmpresa.Focus();
            return;
        }

        var config = new ConfiguracionNegocio
        {
            NombreEmpresa = _txtNombreEmpresa.Text.Trim(),
            RncCedula = _txtRncCedula.Text.Trim(),
            Telefono = _txtTelefono.Text.Trim(),
            WhatsApp = _txtWhatsApp.Text.Trim(),
            Email = _txtEmail.Text.Trim(),
            Direccion = _txtDireccion.Text.Trim(),
            Ciudad = _txtCiudad.Text.Trim(),
            MensajePieFactura = _txtMensajePieFactura.Text.Trim(),
            MensajeGarantiaReparacion = _txtMensajeGarantia.Text.Trim(),
            MonedaSimbolo = _txtMoneda.Text.Trim(),
            ItbisPorcentaje = _numItbis.Value
        };

        var ok = await _configNegocioService.GuardarConfiguracionAsync(config);
        if (ok)
        {
            MessageBox.Show("Los datos del negocio han sido guardados correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            MessageBox.Show("No se pudo guardar la configuración.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ==========================================
    // TAB 2: COPIAS DE SEGURIDAD (BACKUP & RESTORE)
    // ==========================================
    private void ConstruirPestañaBackup(TabPage tab)
    {
        var grpInfo = new GroupBox
        {
            Text = " Estado de la Base de Datos Activa ",
            ForeColor = UITheme.Primary,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 145,
            BackColor = UITheme.Surface1,
            Padding = new Padding(16)
        };

        _lblRutaDb = new Label { Text = "Ruta: Cargando...", Location = new Point(20, 30), AutoSize = true, ForeColor = UITheme.TextPrimary, Font = UITheme.BodyFont };
        _lblTamanoDb = new Label { Text = "Tamaño: Cargando...", Location = new Point(20, 58), AutoSize = true, ForeColor = UITheme.TextPrimary, Font = UITheme.BodyFont };
        _lblModifDb = new Label { Text = "Última Modificación: Cargando...", Location = new Point(20, 86), AutoSize = true, ForeColor = UITheme.TextPrimary, Font = UITheme.BodyFont };

        grpInfo.Controls.AddRange(new Control[] { _lblRutaDb, _lblTamanoDb, _lblModifDb });

        // Panel de Acciones de Backup
        var pnlAcciones = new Panel
        {
            Dock = DockStyle.Top,
            Height = 85,
            BackColor = UITheme.Surface2,
            Padding = new Padding(0, 16, 0, 16)
        };

        var btnCrearBackup = new Button
        {
            Text = "📦  Crear Copia de Seguridad (.db)",
            Size = new Size(260, 44),
            Location = new Point(0, 16),
            BackColor = UITheme.Success,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCrearBackup.FlatAppearance.BorderSize = 0;
        btnCrearBackup.Click += async (s, e) => await EjecutarCrearBackupAsync();

        var btnRestaurar = new Button
        {
            Text = "🔄  Restaurar Base de Datos",
            Size = new Size(230, 44),
            Location = new Point(280, 16),
            BackColor = UITheme.Warning,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnRestaurar.FlatAppearance.BorderSize = 0;
        btnRestaurar.Click += async (s, e) => await EjecutarRestaurarBackupAsync();

        var btnAbrirCarpeta = new Button
        {
            Text = "📂  Abrir Carpeta de Respaldos",
            Size = new Size(220, 44),
            Location = new Point(530, 16),
            BackColor = UITheme.Surface1,
            ForeColor = UITheme.TextPrimary,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnAbrirCarpeta.FlatAppearance.BorderColor = UITheme.BorderStrong;
        btnAbrirCarpeta.Click += (s, e) =>
        {
            try
            {
                if (!Directory.Exists(AppPaths.CarpetaBackups))
                {
                    Directory.CreateDirectory(AppPaths.CarpetaBackups);
                }
                Process.Start(new ProcessStartInfo { FileName = AppPaths.CarpetaBackups, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo abrir la carpeta: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        pnlAcciones.Controls.AddRange(new Control[] { btnCrearBackup, btnRestaurar, btnAbrirCarpeta });

        // Grupo Historial de Backups
        var grpHistorial = new GroupBox
        {
            Text = " Copias de Seguridad Recientes ",
            ForeColor = UITheme.Primary,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Dock = DockStyle.Fill,
            BackColor = UITheme.Surface1,
            Padding = new Padding(12)
        };

        _dgvBackups = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = UITheme.Surface1,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = UITheme.BodyFont
        };

        _dgvBackups.Columns.Add("Nombre", "Nombre de Archivo");
        _dgvBackups.Columns.Add("Tamano", "Tamaño");
        _dgvBackups.Columns.Add("Fecha", "Fecha y Hora de Creación");
        _dgvBackups.Columns.Add("RutaCompleta", "Ruta de Almacenamiento");

        grpHistorial.Controls.Add(_dgvBackups);

        tab.Controls.Add(grpHistorial);
        tab.Controls.Add(pnlAcciones);
        tab.Controls.Add(grpInfo);
    }

    private void CargarInfoBackup()
    {
        try
        {
            var info = _backupService.ObtenerInfoBaseDatos();
            _lblRutaDb.Text = $"Ruta del archivo: {info.RutaArchivo}";
            _lblTamanoDb.Text = $"Tamaño en disco: {info.TamanoFormateado}";
            _lblModifDb.Text = $"Última actualización: {(info.Existe ? info.UltimaModificacion.ToString("dd/MM/yyyy hh:mm tt") : "No encontrada")}";

            _dgvBackups.Rows.Clear();
            var lista = _backupService.ListarBackupsExistentes();
            foreach (var f in lista)
            {
                _dgvBackups.Rows.Add(
                    f.Name,
                    FormatearTamano(f.Length),
                    f.LastWriteTime.ToString("dd/MM/yyyy hh:mm tt"),
                    f.FullName
                );
            }
        }
        catch { }
    }

    private async Task EjecutarCrearBackupAsync()
    {
        using var sfd = new SaveFileDialog
        {
            Title = "Guardar Copia de Seguridad",
            Filter = "Base de Datos SQLite (*.db)|*.db",
            FileName = $"backup_sistemacelulares_{DateTime.Now:yyyyMMdd_HHmmss}.db",
            InitialDirectory = AppPaths.CarpetaBackups
        };

        if (sfd.ShowDialog(this) == DialogResult.OK)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                var resultado = await _backupService.CrearBackupAsync(sfd.FileName);
                Cursor = Cursors.Default;

                if (resultado.Exitoso)
                {
                    MessageBox.Show(resultado.Mensaje, "Backup Completado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    CargarInfoBackup();
                }
                else
                {
                    MessageBox.Show(resultado.Mensaje, "Error al crear backup", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
    }

    private async Task EjecutarRestaurarBackupAsync()
    {
        var confirm = MessageBox.Show(
            "⚠️ ¡ADVERTENCIA CRÍTICA!\n\nRestaurar una copia de seguridad sobrescribirá todos los datos actuales por los del archivo seleccionado.\nSe generará un respaldo automático preventivo antes de la restauración.\n\n¿Desea continuar?",
            "Confirmación de Restauración",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        using var ofd = new OpenFileDialog
        {
            Title = "Seleccionar Copia de Seguridad para Restaurar",
            Filter = "Base de Datos SQLite (*.db)|*.db",
            InitialDirectory = AppPaths.CarpetaBackups
        };

        if (ofd.ShowDialog(this) == DialogResult.OK)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                var resultado = await _backupService.RestaurarBackupAsync(ofd.FileName);
                Cursor = Cursors.Default;

                if (resultado.Exitoso)
                {
                    MessageBox.Show(resultado.Mensaje + "\n\nLa aplicación se reiniciará para recargar la base de datos.", "Restauración Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Application.Restart();
                }
                else
                {
                    MessageBox.Show(resultado.Mensaje, "Error en Restauración", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
    }

    // ==========================================
    // TAB 3: RED Y MULTICAJA
    // ==========================================
    private void ConstruirPestañaRed(TabPage tab)
    {
        var grpRed = new GroupBox
        {
            Text = " Modo de Operación y Red ",
            ForeColor = UITheme.Primary,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 280,
            BackColor = UITheme.Surface1,
            Padding = new Padding(16)
        };

        var lblModo = new Label { Text = "Modo de Operación:", Location = new Point(20, 35), AutoSize = true, ForeColor = UITheme.TextPrimary, Font = UITheme.BodyBoldFont };
        _cmbModoRed = new ComboBox
        {
            Location = new Point(20, 58),
            Width = 360,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = UITheme.BodyFont
        };
        _cmbModoRed.Items.Add("Caja Única Local (Monocaja)");
        _cmbModoRed.Items.Add("Servidor Principal (Multicaja - Base de datos central)");
        _cmbModoRed.Items.Add("Terminal de Caja (Cliente de Red)");

        var lblCaja = new Label { Text = "Número de Caja / Terminal:", Location = new Point(420, 35), AutoSize = true, ForeColor = UITheme.TextPrimary, Font = UITheme.BodyBoldFont };
        _numCajaId = new NumericUpDown { Location = new Point(420, 58), Width = 120, Minimum = 1, Maximum = 99, Value = 1, Font = UITheme.BodyFont };

        var lblIp = new Label { Text = "IP o Nombre del Servidor:", Location = new Point(20, 105), AutoSize = true, ForeColor = UITheme.TextPrimary, Font = UITheme.BodyFont };
        _txtServidorIp = new TextBox { Location = new Point(20, 127), Width = 260, Height = 28, Text = "127.0.0.1", Font = UITheme.BodyFont };

        var lblPto = new Label { Text = "Puerto de Red:", Location = new Point(310, 105), AutoSize = true, ForeColor = UITheme.TextPrimary, Font = UITheme.BodyFont };
        _numPuerto = new NumericUpDown { Location = new Point(310, 127), Width = 120, Minimum = 1000, Maximum = 65535, Value = 5000, Font = UITheme.BodyFont };

        _lblEstadoRed = new Label
        {
            Text = "Estado: Red configurada localmente.",
            Location = new Point(20, 175),
            AutoSize = true,
            ForeColor = UITheme.Success,
            Font = UITheme.BodyBoldFont
        };

        var btnProbar = new Button
        {
            Text = "🔌  Probar Conexión",
            Size = new Size(180, 38),
            Location = new Point(20, 210),
            BackColor = UITheme.Surface2,
            ForeColor = UITheme.TextPrimary,
            Font = UITheme.BodyBoldFont,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnProbar.FlatAppearance.BorderColor = UITheme.BorderStrong;
        btnProbar.Click += async (s, e) => await ProbarConexionRedAsync();

        var btnGuardarRed = new Button
        {
            Text = "💾  Guardar Configuración de Red",
            Size = new Size(240, 38),
            Location = new Point(220, 210),
            BackColor = UITheme.Primary,
            ForeColor = Color.White,
            Font = UITheme.BodyBoldFont,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnGuardarRed.FlatAppearance.BorderSize = 0;
        btnGuardarRed.Click += async (s, e) => await GuardarConfiguracionRedAsync();

        grpRed.Controls.AddRange(new Control[] {
            lblModo, _cmbModoRed, lblCaja, _numCajaId,
            lblIp, _txtServidorIp, lblPto, _numPuerto,
            _lblEstadoRed, btnProbar, btnGuardarRed
        });

        tab.Controls.Add(grpRed);
    }

    private void CargarConfiguracionRed()
    {
        try
        {
            var config = _configRedService.ObtenerConfiguracion();
            _cmbModoRed.SelectedIndex = (int)config.ModoOperacion;
            _numCajaId.Value = config.CajaId >= 1 ? config.CajaId : 1;
            _txtServidorIp.Text = config.ServidorIpOHost;
            _numPuerto.Value = config.Puerto > 0 ? config.Puerto : 5000;
        }
        catch
        {
            _cmbModoRed.SelectedIndex = 0;
        }
    }

    private async Task ProbarConexionRedAsync()
    {
        var config = ObtenerDtoDeFormularioRed();
        _lblEstadoRed.Text = "Probando conexión...";
        _lblEstadoRed.ForeColor = UITheme.Primary;

        var ok = await _configRedService.ProbarConexionAsync(config);
        if (ok)
        {
            _lblEstadoRed.Text = "✓ Conexión exitosa.";
            _lblEstadoRed.ForeColor = UITheme.Success;
        }
        else
        {
            _lblEstadoRed.Text = "✗ No se pudo conectar al servidor.";
            _lblEstadoRed.ForeColor = UITheme.Danger;
        }
    }

    private async Task GuardarConfiguracionRedAsync()
    {
        var config = ObtenerDtoDeFormularioRed();
        await _configRedService.GuardarConfiguracionAsync(config);
        MessageBox.Show("Configuración de red guardada correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private ConfiguracionRedDto ObtenerDtoDeFormularioRed()
    {
        return new ConfiguracionRedDto
        {
            ModoOperacion = (ModoOperacionCaja)_cmbModoRed.SelectedIndex,
            CajaId = (int)_numCajaId.Value,
            ServidorIpOHost = _txtServidorIp.Text.Trim(),
            Puerto = (int)_numPuerto.Value
        };
    }

    private static string FormatearTamano(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }
}
