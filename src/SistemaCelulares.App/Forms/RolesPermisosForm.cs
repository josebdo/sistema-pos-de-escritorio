using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Constants;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class RolesPermisosForm : Form
{
    private readonly IRolService _rolService;
    private readonly SesionUsuario _sesionActual;

    private DataGridView _gridRoles = null!;
    private Button _btnNuevo = null!;
    private Button _btnEditar = null!;
    private Button _btnEliminar = null!;
    private List<Rol> _listaRoles = new();

    public RolesPermisosForm(IRolService rolService, SesionUsuario sesionActual)
    {
        _rolService = rolService;
        _sesionActual = sesionActual;

        InitializeCustomComponents();
        Load += async (s, e) => await RecargarRolesAsync();
    }

    private void InitializeCustomComponents()
    {
        Text = "Gestión de Roles y Permisos (RBAC)";
        Size = new Size(880, 560);
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
            Text = "Roles y Permisos de Acceso",
            Font = UITheme.TitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(0, 5),
            AutoSize = true
        };
        header.Controls.Add(lblTitulo);

        var lblSub = new Label
        {
            Text = "Cree roles personalizados y controle qué acciones puede ejecutar cada empleado",
            Font = UITheme.SmallFont,
            ForeColor = UITheme.TextMuted,
            Location = new Point(0, 35),
            AutoSize = true
        };
        header.Controls.Add(lblSub);
        panelPrincipal.Controls.Add(header);

        // Barra de Herramientas
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 50 };

        var panelBotones = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            Width = 420,
            Height = 45
        };

        _btnNuevo = new Button { Text = "➕ Nuevo Rol", Size = new Size(125, 38) };
        UITheme.AplicarBotonPrimario(_btnNuevo);
        _btnNuevo.Click += async (s, e) => await AbrirCrearRolAsync();
        _btnNuevo.Visible = _sesionActual.TienePermiso(Permisos.RolesCrear);
        panelBotones.Controls.Add(_btnNuevo);

        _btnEditar = new Button { Text = "✏️ Ver / Editar", Size = new Size(120, 38) };
        UITheme.AplicarBotonSecundario(_btnEditar);
        _btnEditar.Click += async (s, e) => await AbrirEditarRolAsync();
        _btnEditar.Visible = _sesionActual.TienePermiso(Permisos.RolesEditar);
        panelBotones.Controls.Add(_btnEditar);

        _btnEliminar = new Button { Text = "🗑️ Eliminar Rol", Size = new Size(120, 38) };
        UITheme.AplicarBotonSecundario(_btnEliminar);
        _btnEliminar.Click += async (s, e) => await EliminarRolAsync();
        _btnEliminar.Visible = _sesionActual.TienePermiso(Permisos.RolesEliminar);
        panelBotones.Controls.Add(_btnEliminar);

        toolbar.Controls.Add(panelBotones);
        panelPrincipal.Controls.Add(toolbar);

        // DataGridView
        var panelGrid = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(1)
        };

        _gridRoles = new DataGridView { Dock = DockStyle.Fill };
        UITheme.EstilizarDataGridView(_gridRoles);
        ConfigurarColumnasGrid();
        panelGrid.Controls.Add(_gridRoles);

        panelPrincipal.Controls.Add(panelGrid);
    }

    private void ConfigurarColumnasGrid()
    {
        _gridRoles.Columns.Clear();
        _gridRoles.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Visible = false });
        _gridRoles.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nombre", HeaderText = "Nombre del Rol", FillWeight = 120 });
        _gridRoles.Columns.Add(new DataGridViewTextBoxColumn { Name = "Descripcion", HeaderText = "Descripción", FillWeight = 180 });
        _gridRoles.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tipo", HeaderText = "Tipo de Rol", FillWeight = 80 });
        _gridRoles.Columns.Add(new DataGridViewTextBoxColumn { Name = "CantPermisos", HeaderText = "Permisos Asignados", FillWeight = 90 });
    }

    private async Task RecargarRolesAsync()
    {
        _listaRoles = await _rolService.ObtenerRolesAsync(soloActivos: true);
        _gridRoles.Rows.Clear();

        foreach (var r in _listaRoles)
        {
            var tipo = r.EsFijo ? "Fijo del Sistema" : "Personalizado";
            var cant = r.Nombre == Rol.SuperAdmin ? "Todos (Total)" : $"{r.RolPermisos.Count} permisos";

            _gridRoles.Rows.Add(
                r.Id,
                r.Nombre,
                r.Descripcion ?? "-",
                tipo,
                cant
            );
        }
    }

    private Rol? ObtenerRolSeleccionado()
    {
        if (_gridRoles.SelectedRows.Count == 0) return null;
        var id = (int)_gridRoles.SelectedRows[0].Cells["Id"].Value;
        return _listaRoles.FirstOrDefault(r => r.Id == id);
    }

    private async Task AbrirCrearRolAsync()
    {
        var modal = new RolModalForm(_rolService);
        if (modal.ShowDialog(this) == DialogResult.OK)
        {
            await RecargarRolesAsync();
        }
    }

    private async Task AbrirEditarRolAsync()
    {
        var rol = ObtenerRolSeleccionado();
        if (rol == null)
        {
            MessageBox.Show("Por favor seleccione un rol para editar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var modal = new RolModalForm(_rolService, rol.Id);
        if (modal.ShowDialog(this) == DialogResult.OK)
        {
            await RecargarRolesAsync();
        }
    }

    private async Task EliminarRolAsync()
    {
        var rol = ObtenerRolSeleccionado();
        if (rol == null)
        {
            MessageBox.Show("Por favor seleccione un rol para eliminar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (rol.EsFijo)
        {
            MessageBox.Show("Los roles fijos del sistema (Super Admin, Admin, Cajero) no pueden eliminarse.", "Operación Restringida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var conf = MessageBox.Show(
            $"¿Está seguro de que desea eliminar el rol '{rol.Nombre}'?",
            "Confirmar Eliminación",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        );

        if (conf == DialogResult.Yes)
        {
            try
            {
                var ok = await _rolService.EliminarRolAsync(rol.Id);
                if (ok)
                {
                    await RecargarRolesAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error al eliminar", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
