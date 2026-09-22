using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Constants;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class CategoriasForm : Form
{
    private readonly ICategoriaService _categoriaService;
    private readonly SesionUsuario _sesionActual;

    private DataGridView _gridCategorias = null!;
    private Button _btnNuevo = null!;
    private Button _btnEditar = null!;
    private Button _btnToggleEstado = null!;
    private List<Categoria> _listaCategorias = new();

    public CategoriasForm(ICategoriaService categoriaService, SesionUsuario sesionActual)
    {
        _categoriaService = categoriaService;
        _sesionActual = sesionActual;

        InitializeCustomComponents();
        Load += async (s, e) => await RecargarCategoriasAsync();
    }

    private void InitializeCustomComponents()
    {
        Text = "Categorías de Productos";
        Size = new Size(800, 520);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = UITheme.AppBg;
        Font = UITheme.BodyFont;

        var panelPrincipal = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
        Controls.Add(panelPrincipal);

        // Header
        var header = new Panel { Dock = DockStyle.Top, Height = 55 };
        var lblTitulo = new Label
        {
            Text = "Categorías de Inventario",
            Font = UITheme.SubtitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(0, 5),
            AutoSize = true
        };
        header.Controls.Add(lblTitulo);

        var lblSub = new Label
        {
            Text = "Organice productos y configure prefijos para la generación correlativa de SKUs",
            Font = UITheme.SmallFont,
            ForeColor = UITheme.TextMuted,
            Location = new Point(0, 30),
            AutoSize = true
        };
        header.Controls.Add(lblSub);

        // Toolbar con FlowLayoutPanel auto-ajustable
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 4, 0, 8)
        };

        _btnNuevo = new Button { Text = "➕ Nueva Categoría", Size = new Size(150, 36), Margin = new Padding(0, 0, 8, 4) };
        UITheme.AplicarBotonPrimario(_btnNuevo);
        _btnNuevo.Click += async (s, e) => await AbrirCrearCategoriaAsync();
        _btnNuevo.Visible = _sesionActual.EsSuperAdmin || _sesionActual.EsAdmin || _sesionActual.TienePermiso(Permisos.CategoriasGestionar);
        toolbar.Controls.Add(_btnNuevo);

        _btnEditar = new Button { Text = "✏️ Editar", Size = new Size(100, 36), Margin = new Padding(0, 0, 8, 4) };
        UITheme.AplicarBotonSecundario(_btnEditar);
        _btnEditar.Click += async (s, e) => await AbrirEditarCategoriaAsync();
        _btnEditar.Visible = _sesionActual.EsSuperAdmin || _sesionActual.EsAdmin || _sesionActual.TienePermiso(Permisos.CategoriasGestionar);
        toolbar.Controls.Add(_btnEditar);

        _btnToggleEstado = new Button { Text = "🔄 Activar/Desactivar", Size = new Size(160, 36), Margin = new Padding(0, 0, 8, 4) };
        UITheme.AplicarBotonSecundario(_btnToggleEstado);
        _btnToggleEstado.Click += async (s, e) => await ToggleEstadoCategoriaAsync();
        _btnToggleEstado.Visible = _sesionActual.EsSuperAdmin || _sesionActual.EsAdmin || _sesionActual.TienePermiso(Permisos.CategoriasGestionar);
        toolbar.Controls.Add(_btnToggleEstado);

        // DataGridView
        var panelGrid = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(1) };
        _gridCategorias = new DataGridView { Dock = DockStyle.Fill };
        UITheme.EstilizarDataGridView(_gridCategorias);
        ConfigurarColumnas();
        panelGrid.Controls.Add(_gridCategorias);

        // Agregar al panel principal en orden de acoplamiento correcto
        panelPrincipal.Controls.Add(panelGrid);
        panelPrincipal.Controls.Add(toolbar);
        panelPrincipal.Controls.Add(header);
    }

    private void ConfigurarColumnas()
    {
        _gridCategorias.Columns.Clear();
        _gridCategorias.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Visible = false });
        _gridCategorias.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nombre", HeaderText = "Nombre Categoría", FillWeight = 140 });
        _gridCategorias.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrefijoSku", HeaderText = "Prefijo SKU", FillWeight = 80 });
        _gridCategorias.Columns.Add(new DataGridViewTextBoxColumn { Name = "Descripcion", HeaderText = "Descripción", FillWeight = 160 });
        _gridCategorias.Columns.Add(new DataGridViewTextBoxColumn { Name = "CantProductos", HeaderText = "Productos", FillWeight = 70 });
        _gridCategorias.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", HeaderText = "Estado", FillWeight = 70 });
    }

    private async Task RecargarCategoriasAsync()
    {
        _listaCategorias = await _categoriaService.ObtenerCategoriasAsync(soloActivas: false);
        _gridCategorias.Rows.Clear();

        foreach (var c in _listaCategorias)
        {
            var rowIndex = _gridCategorias.Rows.Add(
                c.Id,
                c.Nombre,
                c.PrefijoSku,
                c.Descripcion ?? "-",
                $"{c.Productos.Count} prods",
                c.Activo ? "Activa" : "Inactiva"
            );

            if (!c.Activo)
            {
                _gridCategorias.Rows[rowIndex].DefaultCellStyle.ForeColor = UITheme.TextMuted;
            }
        }
    }

    private Categoria? ObtenerSeleccionada()
    {
        if (_gridCategorias.SelectedRows.Count == 0) return null;
        var id = (int)_gridCategorias.SelectedRows[0].Cells["Id"].Value;
        return _listaCategorias.FirstOrDefault(c => c.Id == id);
    }

    private async Task AbrirCrearCategoriaAsync()
    {
        var modal = new CategoriaModalForm(_categoriaService);
        if (modal.ShowDialog(this) == DialogResult.OK)
        {
            await RecargarCategoriasAsync();
        }
    }

    private async Task AbrirEditarCategoriaAsync()
    {
        var cat = ObtenerSeleccionada();
        if (cat == null)
        {
            MessageBox.Show("Por favor seleccione una categoría.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var modal = new CategoriaModalForm(_categoriaService, cat.Id);
        if (modal.ShowDialog(this) == DialogResult.OK)
        {
            await RecargarCategoriasAsync();
        }
    }

    private async Task ToggleEstadoCategoriaAsync()
    {
        var cat = ObtenerSeleccionada();
        if (cat == null)
        {
            MessageBox.Show("Por favor seleccione una categoría.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            var nuevo = !cat.Activo;
            var ok = await _categoriaService.CambiarEstadoActivoAsync(cat.Id, nuevo);
            if (ok)
            {
                await RecargarCategoriasAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "No se puede cambiar el estado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
