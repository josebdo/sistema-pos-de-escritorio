using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Constants;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class ProveedoresForm : Form
{
    private readonly IProveedorService _proveedorService;
    private readonly SesionUsuario _sesionActual;

    private DataGridView _gridProveedores = null!;
    private TextBox _txtBuscar = null!;
    private CheckBox _chkSoloActivos = null!;
    private Button _btnNuevo = null!;
    private Button _btnEditar = null!;
    private Button _btnToggleEstado = null!;

    private List<Proveedor> _listaProveedores = new();

    public ProveedoresForm(IProveedorService proveedorService, SesionUsuario sesionActual)
    {
        _proveedorService = proveedorService;
        _sesionActual = sesionActual;

        InitializeCustomComponents();
        Load += async (s, e) => await RecargarProveedoresAsync();
    }

    private void InitializeCustomComponents()
    {
        Text = "Gestión de Proveedores";
        Size = new Size(1050, 620);
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
            Text = "Proveedores Comerciales",
            Font = UITheme.TitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(0, 5),
            AutoSize = true
        };
        header.Controls.Add(lblTitulo);

        var lblSub = new Label
        {
            Text = "Administre el directorio de distribuidores e importadores para compras e inventario",
            Font = UITheme.SmallFont,
            ForeColor = UITheme.TextMuted,
            Location = new Point(0, 35),
            AutoSize = true
        };
        header.Controls.Add(lblSub);
        panelPrincipal.Controls.Add(header);

        // Toolbar Contenedor
        var flowToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 45,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 4, 0, 6)
        };

        var lblBuscar = new Label { Text = "🔍 Buscar:", AutoSize = true, Font = UITheme.SectionFont, Margin = new Padding(0, 5, 4, 0) };
        flowToolbar.Controls.Add(lblBuscar);

        _txtBuscar = new TextBox { Size = new Size(180, 26), Font = new Font("Segoe UI", 9F), Margin = new Padding(0, 2, 8, 0) };
        _txtBuscar.TextChanged += (s, e) => FiltrarGrid();
        flowToolbar.Controls.Add(_txtBuscar);

        _chkSoloActivos = new CheckBox { Text = "Solo activos", Checked = true, AutoSize = true, Margin = new Padding(0, 4, 15, 0) };
        _chkSoloActivos.CheckedChanged += async (s, e) => await RecargarProveedoresAsync();
        flowToolbar.Controls.Add(_chkSoloActivos);

        _btnNuevo = new Button { Text = "➕ Nuevo", Size = new Size(95, 32), Margin = new Padding(0, 0, 6, 0) };
        UITheme.AplicarBotonPrimario(_btnNuevo);
        _btnNuevo.Click += async (s, e) => await AbrirCrearProveedorAsync();
        _btnNuevo.Visible = _sesionActual.TienePermiso(Permisos.ProveedoresGestionar);
        flowToolbar.Controls.Add(_btnNuevo);

        _btnEditar = new Button { Text = "✏️ Editar", Size = new Size(85, 32), Margin = new Padding(0, 0, 6, 0) };
        UITheme.AplicarBotonSecundario(_btnEditar);
        _btnEditar.Click += async (s, e) => await AbrirEditarProveedorAsync();
        _btnEditar.Visible = _sesionActual.TienePermiso(Permisos.ProveedoresGestionar);
        flowToolbar.Controls.Add(_btnEditar);

        _btnToggleEstado = new Button { Text = "🔄 Act/Desc", Size = new Size(95, 32), Margin = new Padding(0, 0, 6, 0) };
        UITheme.AplicarBotonSecundario(_btnToggleEstado);
        _btnToggleEstado.Click += async (s, e) => await ToggleEstadoProveedorAsync();
        _btnToggleEstado.Visible = _sesionActual.TienePermiso(Permisos.ProveedoresGestionar);
        flowToolbar.Controls.Add(_btnToggleEstado);

        panelPrincipal.Controls.Add(flowToolbar);

        // DataGridView
        var panelGrid = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(1)
        };

        _gridProveedores = new DataGridView { Dock = DockStyle.Fill };
        UITheme.EstilizarDataGridView(_gridProveedores);
        ConfigurarColumnasGrid();
        panelGrid.Controls.Add(_gridProveedores);

        panelPrincipal.Controls.Add(panelGrid);
    }

    private void ConfigurarColumnasGrid()
    {
        _gridProveedores.Columns.Clear();
        _gridProveedores.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Visible = false });
        _gridProveedores.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nombre", HeaderText = "Razón Social / Proveedor", FillWeight = 140 });
        _gridProveedores.Columns.Add(new DataGridViewTextBoxColumn { Name = "Rnc", HeaderText = "RNC", FillWeight = 90 });
        _gridProveedores.Columns.Add(new DataGridViewTextBoxColumn { Name = "Telefono", HeaderText = "Teléfono", FillWeight = 90 });
        _gridProveedores.Columns.Add(new DataGridViewTextBoxColumn { Name = "Contacto", HeaderText = "Contacto", FillWeight = 100 });
        _gridProveedores.Columns.Add(new DataGridViewTextBoxColumn { Name = "Email", HeaderText = "Email", FillWeight = 110 });
        _gridProveedores.Columns.Add(new DataGridViewTextBoxColumn { Name = "Direccion", HeaderText = "Dirección", FillWeight = 130 });
        _gridProveedores.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", HeaderText = "Estado", FillWeight = 70 });
    }

    private async Task RecargarProveedoresAsync()
    {
        _listaProveedores = await _proveedorService.ObtenerProveedoresAsync(
            soloActivos: !_chkSoloActivos.Checked
        );
        FiltrarGrid();
    }

    private void FiltrarGrid()
    {
        var q = _txtBuscar.Text.Trim().ToLower();
        var filtrados = _listaProveedores.Where(p =>
            string.IsNullOrEmpty(q) ||
            p.Nombre.ToLower().Contains(q) ||
            (p.Rnc != null && p.Rnc.ToLower().Contains(q)) ||
            (p.Telefono != null && p.Telefono.ToLower().Contains(q)) ||
            (p.Contacto != null && p.Contacto.ToLower().Contains(q)) ||
            (p.Email != null && p.Email.ToLower().Contains(q))
        ).ToList();

        _gridProveedores.Rows.Clear();
        foreach (var p in filtrados)
        {
            var rowIndex = _gridProveedores.Rows.Add(
                p.Id,
                p.Nombre,
                p.Rnc ?? "-",
                p.Telefono ?? "-",
                p.Contacto ?? "-",
                p.Email ?? "-",
                p.Direccion ?? "-",
                p.Activo ? "Activo" : "Inactivo"
            );

            if (!p.Activo)
            {
                _gridProveedores.Rows[rowIndex].DefaultCellStyle.ForeColor = UITheme.TextMuted;
            }
        }
    }

    private Proveedor? ObtenerSeleccionado()
    {
        if (_gridProveedores.SelectedRows.Count == 0) return null;
        var id = (int)_gridProveedores.SelectedRows[0].Cells["Id"].Value;
        return _listaProveedores.FirstOrDefault(p => p.Id == id);
    }

    private async Task AbrirCrearProveedorAsync()
    {
        var modal = new ProveedorModalForm(_proveedorService);
        if (modal.ShowDialog(this) == DialogResult.OK)
        {
            await RecargarProveedoresAsync();
        }
    }

    private async Task AbrirEditarProveedorAsync()
    {
        var p = ObtenerSeleccionado();
        if (p == null)
        {
            MessageBox.Show("Por favor seleccione un proveedor para editar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var modal = new ProveedorModalForm(_proveedorService, p.Id);
        if (modal.ShowDialog(this) == DialogResult.OK)
        {
            await RecargarProveedoresAsync();
        }
    }

    private async Task ToggleEstadoProveedorAsync()
    {
        var p = ObtenerSeleccionado();
        if (p == null)
        {
            MessageBox.Show("Por favor seleccione un proveedor.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var nuevo = !p.Activo;
        var accion = nuevo ? "activar" : "desactivar";

        var conf = MessageBox.Show(
            $"¿Está seguro de que desea {accion} al proveedor '{p.Nombre}'?\n(Se conserva la trazabilidad para compras existentes).",
            "Confirmación",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        );

        if (conf == DialogResult.Yes)
        {
            try
            {
                var ok = await _proveedorService.CambiarEstadoActivoAsync(p.Id, nuevo);
                if (ok)
                {
                    await RecargarProveedoresAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
