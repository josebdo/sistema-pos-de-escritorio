using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Constants;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class UsuariosForm : Form
{
    private readonly IUsuarioService _usuarioService;
    private readonly IRolService _rolService;
    private readonly IAuthService _authService;
    private readonly SesionUsuario _sesionActual;

    private DataGridView _gridUsuarios = null!;
    private TextBox _txtBuscar = null!;
    private CheckBox _chkSoloActivos = null!;
    private Button _btnNuevo = null!;
    private Button _btnEditar = null!;
    private Button _btnToggleEstado = null!;
    private Button _btnResetPass = null!;
    private List<Usuario> _listaUsuarios = new();

    public UsuariosForm(
        IUsuarioService usuarioService,
        IRolService rolService,
        IAuthService authService,
        SesionUsuario sesionActual)
    {
        _usuarioService = usuarioService;
        _rolService = rolService;
        _authService = authService;
        _sesionActual = sesionActual;

        InitializeCustomComponents();
        Load += async (s, e) => await RecargarUsuariosAsync();
    }

    private void InitializeCustomComponents()
    {
        Text = "Gestión de Usuarios y Empleados";
        Size = new Size(950, 600);
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
            Text = "Gestión de Empleados y Usuarios",
            Font = UITheme.TitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(0, 5),
            AutoSize = true
        };
        header.Controls.Add(lblTitulo);

        var lblSub = new Label
        {
            Text = "Administre cuentas, roles asignados y credenciales de acceso",
            Font = UITheme.SmallFont,
            ForeColor = UITheme.TextMuted,
            Location = new Point(0, 35),
            AutoSize = true
        };
        header.Controls.Add(lblSub);
        panelPrincipal.Controls.Add(header);

        // Barra de Herramientas / Búsqueda
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 55 };

        var lblBuscar = new Label { Text = "🔍 Buscar:", Location = new Point(0, 15), AutoSize = true, Font = UITheme.SectionFont };
        toolbar.Controls.Add(lblBuscar);

        _txtBuscar = new TextBox { Location = new Point(70, 12), Size = new Size(240, 30), Font = new Font("Segoe UI", 10F) };
        _txtBuscar.TextChanged += (s, e) => FiltrarGrid();
        toolbar.Controls.Add(_txtBuscar);

        _chkSoloActivos = new CheckBox { Text = "Solo activos", Location = new Point(330, 14), Checked = true, AutoSize = true };
        _chkSoloActivos.CheckedChanged += async (s, e) => await RecargarUsuariosAsync();
        toolbar.Controls.Add(_chkSoloActivos);

        // Botones de acción a la derecha
        var panelBotones = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            Width = 480,
            Height = 50
        };

        _btnNuevo = new Button { Text = "➕ Nuevo Usuario", Size = new Size(130, 38) };
        UITheme.AplicarBotonPrimario(_btnNuevo);
        _btnNuevo.Click += async (s, e) => await AbrirCrearUsuarioAsync();
        _btnNuevo.Visible = _sesionActual.TienePermiso(Permisos.UsuariosCrear);
        panelBotones.Controls.Add(_btnNuevo);

        _btnEditar = new Button { Text = "✏️ Editar", Size = new Size(95, 38) };
        UITheme.AplicarBotonSecundario(_btnEditar);
        _btnEditar.Click += async (s, e) => await AbrirEditarUsuarioAsync();
        _btnEditar.Visible = _sesionActual.TienePermiso(Permisos.UsuariosEditar);
        panelBotones.Controls.Add(_btnEditar);

        _btnToggleEstado = new Button { Text = "🔄 Activar/Desactivar", Size = new Size(140, 38) };
        UITheme.AplicarBotonSecundario(_btnToggleEstado);
        _btnToggleEstado.Click += async (s, e) => await ToggleEstadoUsuarioAsync();
        _btnToggleEstado.Visible = _sesionActual.TienePermiso(Permisos.UsuariosDesactivar);
        panelBotones.Controls.Add(_btnToggleEstado);

        _btnResetPass = new Button { Text = "🔑 Reset Clave", Size = new Size(110, 38) };
        UITheme.AplicarBotonSecundario(_btnResetPass);
        _btnResetPass.Click += (s, e) => AbrirResetPassword();
        _btnResetPass.Visible = _sesionActual.TienePermiso(Permisos.UsuariosResetPassword);
        panelBotones.Controls.Add(_btnResetPass);

        toolbar.Controls.Add(panelBotones);
        panelPrincipal.Controls.Add(toolbar);

        // DataGridView
        var panelGrid = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(1)
        };

        _gridUsuarios = new DataGridView { Dock = DockStyle.Fill };
        UITheme.EstilizarDataGridView(_gridUsuarios);
        ConfigurarColumnasGrid();
        panelGrid.Controls.Add(_gridUsuarios);

        panelPrincipal.Controls.Add(panelGrid);
    }

    private void ConfigurarColumnasGrid()
    {
        _gridUsuarios.Columns.Clear();
        _gridUsuarios.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Width = 50, Visible = false });
        _gridUsuarios.Columns.Add(new DataGridViewTextBoxColumn { Name = "NombreCompleto", HeaderText = "Nombre Completo", FillWeight = 140 });
        _gridUsuarios.Columns.Add(new DataGridViewTextBoxColumn { Name = "NombreUsuario", HeaderText = "Usuario", FillWeight = 90 });
        _gridUsuarios.Columns.Add(new DataGridViewTextBoxColumn { Name = "Rol", HeaderText = "Rol", FillWeight = 90 });
        _gridUsuarios.Columns.Add(new DataGridViewTextBoxColumn { Name = "Telefono", HeaderText = "Teléfono", FillWeight = 80 });
        _gridUsuarios.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", HeaderText = "Estado", FillWeight = 70 });
        _gridUsuarios.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClaveTemp", HeaderText = "Cambio Clave", FillWeight = 80 });
        _gridUsuarios.Columns.Add(new DataGridViewTextBoxColumn { Name = "UltimoAcceso", HeaderText = "Último Acceso", FillWeight = 110 });
    }

    private async Task RecargarUsuariosAsync()
    {
        _listaUsuarios = await _usuarioService.ObtenerTodosAsync(incluirInactivos: !_chkSoloActivos.Checked);
        FiltrarGrid();
    }

    private void FiltrarGrid()
    {
        var filtro = _txtBuscar.Text.Trim().ToLower();
        var filtrados = _listaUsuarios.Where(u =>
            string.IsNullOrEmpty(filtro) ||
            u.NombreCompleto.ToLower().Contains(filtro) ||
            u.NombreUsuario.ToLower().Contains(filtro) ||
            u.Rol.Nombre.ToLower().Contains(filtro)
        ).ToList();

        _gridUsuarios.Rows.Clear();
        foreach (var u in filtrados)
        {
            var rowIndex = _gridUsuarios.Rows.Add(
                u.Id,
                u.NombreCompleto,
                u.NombreUsuario,
                u.Rol.Nombre,
                u.Telefono ?? "-",
                u.Activo ? "Activo" : "Inactivo",
                u.DebeCambiarPassword ? "Pendiente" : "OK",
                u.UltimoAcceso.HasValue ? u.UltimoAcceso.Value.ToLocalTime().ToString("dd/MM/yyyy hh:mm tt") : "Nunca"
            );

            // Resaltar inactivos o pendientes de cambio de clave
            var row = _gridUsuarios.Rows[rowIndex];
            if (!u.Activo)
            {
                row.DefaultCellStyle.ForeColor = UITheme.TextMuted;
            }
            if (u.DebeCambiarPassword)
            {
                row.Cells["ClaveTemp"].Style.ForeColor = UITheme.Warning;
            }
        }
    }

    private Usuario? ObtenerUsuarioSeleccionado()
    {
        if (_gridUsuarios.SelectedRows.Count == 0) return null;
        var id = (int)_gridUsuarios.SelectedRows[0].Cells["Id"].Value;
        return _listaUsuarios.FirstOrDefault(u => u.Id == id);
    }

    private async Task AbrirCrearUsuarioAsync()
    {
        var modal = new UsuarioModalForm(_usuarioService, _rolService);
        if (modal.ShowDialog(this) == DialogResult.OK)
        {
            await RecargarUsuariosAsync();
        }
    }

    private async Task AbrirEditarUsuarioAsync()
    {
        var usuario = ObtenerUsuarioSeleccionado();
        if (usuario == null)
        {
            MessageBox.Show("Por favor seleccione un usuario para editar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var modal = new UsuarioModalForm(_usuarioService, _rolService, usuario.Id);
        if (modal.ShowDialog(this) == DialogResult.OK)
        {
            await RecargarUsuariosAsync();
        }
    }

    private async Task ToggleEstadoUsuarioAsync()
    {
        var usuario = ObtenerUsuarioSeleccionado();
        if (usuario == null)
        {
            MessageBox.Show("Por favor seleccione un usuario.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var nuevoEstado = !usuario.Activo;
        var accion = nuevoEstado ? "activar" : "desactivar";

        var conf = MessageBox.Show(
            $"¿Está seguro de que desea {accion} al usuario '{usuario.NombreUsuario}'?\n(Nota: Se preserva el historial de transacciones).",
            "Confirmación",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        );

        if (conf == DialogResult.Yes)
        {
            try
            {
                var ok = await _usuarioService.CambiarEstadoActivoAsync(usuario.Id, nuevoEstado);
                if (ok)
                {
                    await RecargarUsuariosAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void AbrirResetPassword()
    {
        var usuario = ObtenerUsuarioSeleccionado();
        if (usuario == null)
        {
            MessageBox.Show("Por favor seleccione un usuario para resetear su clave.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var dialog = new ResetPasswordDialog(_authService, _sesionActual.UsuarioId, usuario.Id, usuario.NombreUsuario);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _ = RecargarUsuariosAsync();
        }
    }
}
