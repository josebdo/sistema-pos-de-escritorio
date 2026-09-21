using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class ConfiguracionRedMultiCajaForm : Form
{
    private readonly IConfiguracionRedService _configService;
    private readonly IDataMigrationService _migrationService;
    private ConfiguracionRedDto _configActual;

    private RadioButton _rbCajaUnica = null!;
    private RadioButton _rbServidorCentral = null!;
    private RadioButton _rbCajaCliente = null!;

    private GroupBox _gbParametrosRed = null!;
    private TextBox _txtServidorIp = null!;
    private NumericUpDown _numPuerto = null!;
    private NumericUpDown _numCajaId = null!;
    private TextBox _txtNombreCaja = null!;
    private Button _btnProbarConexion = null!;
    private Label _lblResultadoConexion = null!;

    private Button _btnExportar = null!;
    private Button _btnImportar = null!;
    private Button _btnGuardar = null!;
    private Button _btnCerrar = null!;

    public ConfiguracionRedMultiCajaForm(
        IConfiguracionRedService configService,
        IDataMigrationService migrationService)
    {
        _configService = configService;
        _migrationService = migrationService;
        _configActual = _configService.ObtenerConfiguracion();

        InitializeComponents();
        CargarDatos();
    }

    private void InitializeComponents()
    {
        Text = "Configuración de Topología y Modo Multi-Caja (Super Admin)";
        Size = new Size(720, 680);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(248, 249, 250);

        // Header
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Color.FromArgb(24, 43, 73),
            Padding = new Padding(20, 12, 20, 12)
        };

        var lblTitulo = new Label
        {
            Text = "MODO DE OPERACIÓN Y TOPOLOGÍA DE RED",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 25
        };

        var lblSubtitulo = new Label
        {
            Text = "Configuración exclusiva de Super Admin para operar en caja única o en red local (LAN) multi-caja.",
            ForeColor = Color.FromArgb(173, 181, 189),
            Font = new Font("Segoe UI", 9),
            Dock = DockStyle.Fill
        };

        pnlHeader.Controls.Add(lblSubtitulo);
        pnlHeader.Controls.Add(lblTitulo);

        // 1. Selector de Modo
        var gbModo = new GroupBox
        {
            Text = "Seleccione el Modo de Operación de este Equipo",
            Location = new Point(20, 85),
            Size = new Size(665, 125),
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };

        _rbCajaUnica = new RadioButton
        {
            Text = "Modo Caja Única (Offline / SQLite local independiente)",
            Location = new Point(20, 25),
            AutoSize = true,
            Font = new Font("Segoe UI", 9)
        };
        _rbCajaUnica.CheckedChanged += (s, e) => ActualizarEstadoControles();

        _rbServidorCentral = new RadioButton
        {
            Text = "Modo Servidor Central (Esta PC aloja la Base de Datos Central y atiende a otras cajas LAN)",
            Location = new Point(20, 55),
            AutoSize = true,
            Font = new Font("Segoe UI", 9)
        };
        _rbServidorCentral.CheckedChanged += (s, e) => ActualizarEstadoControles();

        _rbCajaCliente = new RadioButton
        {
            Text = "Modo Caja Cliente LAN (Terminal secundaria conectada al Servidor Central)",
            Location = new Point(20, 85),
            AutoSize = true,
            Font = new Font("Segoe UI", 9)
        };
        _rbCajaCliente.CheckedChanged += (s, e) => ActualizarEstadoControles();

        gbModo.Controls.Add(_rbCajaUnica);
        gbModo.Controls.Add(_rbServidorCentral);
        gbModo.Controls.Add(_rbCajaCliente);

        // 2. Parámetros de Red
        _gbParametrosRed = new GroupBox
        {
            Text = "Parámetros de Conexión de Red LAN y Terminal",
            Location = new Point(20, 220),
            Size = new Size(665, 175),
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };

        var lblIp = new Label { Text = "IP / Nombre del Servidor Central:", Location = new Point(20, 28), AutoSize = true, Font = new Font("Segoe UI", 9) };
        _txtServidorIp = new TextBox { Location = new Point(20, 48), Size = new Size(200, 25), Font = new Font("Segoe UI", 9) };

        var lblPuerto = new Label { Text = "Puerto de Comunicación:", Location = new Point(240, 28), AutoSize = true, Font = new Font("Segoe UI", 9) };
        _numPuerto = new NumericUpDown { Location = new Point(240, 48), Size = new Size(120, 25), Maximum = 65535, Font = new Font("Segoe UI", 9) };

        var lblCajaId = new Label { Text = "ID de Caja:", Location = new Point(380, 28), AutoSize = true, Font = new Font("Segoe UI", 9) };
        _numCajaId = new NumericUpDown { Location = new Point(380, 48), Size = new Size(90, 25), Minimum = 1, Maximum = 99, Font = new Font("Segoe UI", 9) };

        var lblNombreCaja = new Label { Text = "Nombre / Identificador de la Caja:", Location = new Point(20, 85), AutoSize = true, Font = new Font("Segoe UI", 9) };
        _txtNombreCaja = new TextBox { Location = new Point(20, 105), Size = new Size(340, 25), Font = new Font("Segoe UI", 9) };

        _btnProbarConexion = new Button
        {
            Text = "🔌 Probar Conectividad",
            Location = new Point(380, 103),
            Size = new Size(150, 28),
            BackColor = Color.FromArgb(52, 152, 219),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnProbarConexion.FlatAppearance.BorderSize = 0;
        _btnProbarConexion.Click += async (s, e) => await ProbarConexionAsync();

        _lblResultadoConexion = new Label
        {
            Text = "Estado: No verificado",
            Location = new Point(20, 142),
            Size = new Size(620, 20),
            Font = new Font("Segoe UI", 9, FontStyle.Italic),
            ForeColor = Color.Gray
        };

        _gbParametrosRed.Controls.Add(lblIp);
        _gbParametrosRed.Controls.Add(_txtServidorIp);
        _gbParametrosRed.Controls.Add(lblPuerto);
        _gbParametrosRed.Controls.Add(_numPuerto);
        _gbParametrosRed.Controls.Add(lblCajaId);
        _gbParametrosRed.Controls.Add(_numCajaId);
        _gbParametrosRed.Controls.Add(lblNombreCaja);
        _gbParametrosRed.Controls.Add(_txtNombreCaja);
        _gbParametrosRed.Controls.Add(_btnProbarConexion);
        _gbParametrosRed.Controls.Add(_lblResultadoConexion);

        // 3. Migración y Respaldo
        var gbMigracion = new GroupBox
        {
            Text = "Herramientas de Migración de Base de Datos (SQLite ➔ Servidor Central)",
            Location = new Point(20, 405),
            Size = new Size(665, 140),
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };

        var lblInfoMigracion = new Label
        {
            Text = "Al pasar de Caja Única a Multi-Caja, use 'Exportar Snapshot' para generar un respaldo íntegro de todos los datos (productos, usuarios, inventario, finanzas) y luego 'Importar' en el nuevo servidor central sin pérdida de información.",
            Location = new Point(20, 25),
            Size = new Size(625, 45),
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(108, 117, 125)
        };

        _btnExportar = new Button
        {
            Text = "📤 Exportar Snapshot (JSON con Hash)",
            Location = new Point(20, 80),
            Size = new Size(240, 35),
            BackColor = Color.FromArgb(41, 128, 185),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnExportar.FlatAppearance.BorderSize = 0;
        _btnExportar.Click += async (s, e) => await ExportarSnapshotAsync();

        _btnImportar = new Button
        {
            Text = "📥 Importar Datos a Base Central",
            Location = new Point(280, 80),
            Size = new Size(240, 35),
            BackColor = Color.FromArgb(39, 174, 96),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnImportar.FlatAppearance.BorderSize = 0;
        _btnImportar.Click += async (s, e) => await ImportarSnapshotAsync();

        gbMigracion.Controls.Add(lblInfoMigracion);
        gbMigracion.Controls.Add(_btnExportar);
        gbMigracion.Controls.Add(_btnImportar);

        // 4. Botones Guardar / Cerrar
        _btnGuardar = new Button
        {
            Text = "💾 Guardar y Aplicar Configuración",
            Location = new Point(365, 565),
            Size = new Size(210, 40),
            BackColor = Color.FromArgb(39, 174, 96),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnGuardar.FlatAppearance.BorderSize = 0;
        _btnGuardar.Click += async (s, e) => await GuardarConfiguracionAsync();

        _btnCerrar = new Button
        {
            Text = "Cerrar",
            Location = new Point(585, 565),
            Size = new Size(100, 40),
            BackColor = Color.FromArgb(149, 165, 166),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9),
            Cursor = Cursors.Hand
        };
        _btnCerrar.FlatAppearance.BorderSize = 0;
        _btnCerrar.Click += (s, e) => Close();

        Controls.Add(pnlHeader);
        Controls.Add(gbModo);
        Controls.Add(_gbParametrosRed);
        Controls.Add(gbMigracion);
        Controls.Add(_btnGuardar);
        Controls.Add(_btnCerrar);
    }

    private void CargarDatos()
    {
        switch (_configActual.ModoOperacion)
        {
            case ModoOperacionCaja.CajaUnicaLocal:
                _rbCajaUnica.Checked = true;
                break;
            case ModoOperacionCaja.ServidorCentral:
                _rbServidorCentral.Checked = true;
                break;
            case ModoOperacionCaja.CajaClienteLan:
                _rbCajaCliente.Checked = true;
                break;
        }

        _txtServidorIp.Text = _configActual.ServidorIpOHost;
        _numPuerto.Value = _configActual.Puerto;
        _numCajaId.Value = _configActual.CajaId;
        _txtNombreCaja.Text = _configActual.NombreCaja;

        ActualizarEstadoControles();
    }

    private void ActualizarEstadoControles()
    {
        bool esRed = _rbServidorCentral.Checked || _rbCajaCliente.Checked;
        _gbParametrosRed.Enabled = esRed;
        _btnProbarConexion.Enabled = _rbCajaCliente.Checked;
    }

    private async Task ProbarConexionAsync()
    {
        _lblResultadoConexion.Text = "Probando conexión con el servidor...";
        _lblResultadoConexion.ForeColor = Color.FromArgb(41, 128, 185);

        var configTemp = new ConfiguracionRedDto
        {
            ModoOperacion = ModoOperacionCaja.CajaClienteLan,
            ServidorIpOHost = _txtServidorIp.Text.Trim(),
            Puerto = (int)_numPuerto.Value
        };

        bool ok = await _configService.ProbarConexionAsync(configTemp);
        if (ok)
        {
            _lblResultadoConexion.Text = "✔ Conexión exitosa con el Servidor Central.";
            _lblResultadoConexion.ForeColor = Color.FromArgb(39, 174, 96);
        }
        else
        {
            _lblResultadoConexion.Text = "✖ No se pudo establecer conexión con el Servidor Central en la IP y puerto indicados.";
            _lblResultadoConexion.ForeColor = Color.FromArgb(231, 76, 60);
        }
    }

    private async Task ExportarSnapshotAsync()
    {
        using var sfd = new SaveFileDialog
        {
            Filter = "Archivo Snapshot JSON (*.json)|*.json",
            FileName = $"snapshot_tienda_{DateTime.Now:yyyyMMdd_HHmmss}.json",
            Title = "Guardar Snapshot de Base de Datos"
        };

        if (sfd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                string json = await _migrationService.ExportarSnapshotJsonAsync();
                await File.WriteAllTextAsync(sfd.FileName, json);
                MessageBox.Show("Snapshot de base de datos exportado correctamente con firma de integridad SHA-256.", "Exportación Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar el snapshot: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async Task ImportarSnapshotAsync()
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Archivo Snapshot JSON (*.json)|*.json",
            Title = "Seleccionar Archivo Snapshot de Tienda"
        };

        if (ofd.ShowDialog() == DialogResult.OK)
        {
            var confirm = MessageBox.Show(
                "¿Está seguro de importar los datos de este snapshot? Se insertarán los catálogos y registros que no existan previamente.",
                "Confirmar Importación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                string json = await File.ReadAllTextAsync(ofd.FileName);
                bool ok = await _migrationService.ImportarSnapshotJsonAsync(json, sobreescribirExistente: false);
                if (ok)
                {
                    MessageBox.Show("Datos importados y validados exitosamente en la base de datos central.", "Importación Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("No se pudo importar el snapshot. El archivo puede estar corrupto o la firma SHA-256 no coincide.", "Error de Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante la importación: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async Task GuardarConfiguracionAsync()
    {
        ModoOperacionCaja nuevoModo = ModoOperacionCaja.CajaUnicaLocal;
        if (_rbServidorCentral.Checked) nuevoModo = ModoOperacionCaja.ServidorCentral;
        if (_rbCajaCliente.Checked) nuevoModo = ModoOperacionCaja.CajaClienteLan;

        _configActual.ModoOperacion = nuevoModo;
        _configActual.ServidorIpOHost = _txtServidorIp.Text.Trim();
        _configActual.Puerto = (int)_numPuerto.Value;
        _configActual.CajaId = (int)_numCajaId.Value;
        _configActual.NombreCaja = _txtNombreCaja.Text.Trim();

        await _configService.GuardarConfiguracionAsync(_configActual);
        MessageBox.Show("Configuración de topología guardada exitosamente.", "Configuración Guardada", MessageBoxButtons.OK, MessageBoxIcon.Information);
        Close();
    }
}
