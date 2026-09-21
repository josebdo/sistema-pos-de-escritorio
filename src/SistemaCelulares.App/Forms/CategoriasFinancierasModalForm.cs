using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;

namespace SistemaCelulares.App.Forms;

public class CategoriasFinancierasModalForm : Form
{
    private readonly IFinanzasService _finanzasService;

    private DataGridView _dgvCategorias = null!;
    private TextBox _txtNombre = null!;
    private ComboBox _cboTipo = null!;
    private TextBox _txtDescripcion = null!;
    private Button _btnCrear = null!;
    private Label _lblError = null!;

    public CategoriasFinancierasModalForm(IFinanzasService finanzasService)
    {
        _finanzasService = finanzasService;

        InitializeCustomComponents();
        Load += async (s, e) => await CargarCategoriasAsync();
    }

    private void InitializeCustomComponents()
    {
        Text = "Gestión de Categorías de Gastos e Ingresos";
        Size = new Size(720, 580);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.AppBg;
        Font = UITheme.BodyFont;

        // Panel Crear Nueva
        var panelNuevo = new Panel
        {
            Location = new Point(20, 15),
            Size = new Size(665, 145),
            BackColor = Color.White,
            Padding = new Padding(15)
        };
        Controls.Add(panelNuevo);

        var lblNuevoTitulo = new Label
        {
            Text = "➕ Crear Nueva Categoría Financiera",
            Font = UITheme.SectionFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(15, 10),
            AutoSize = true
        };
        panelNuevo.Controls.Add(lblNuevoTitulo);

        var lblNom = new Label { Text = "Nombre de la Categoría *", Font = UITheme.SmallFont, Location = new Point(15, 38), AutoSize = true };
        panelNuevo.Controls.Add(lblNom);

        _txtNombre = new TextBox { Location = new Point(15, 56), Size = new Size(240, 26) };
        panelNuevo.Controls.Add(_txtNombre);

        var lblTipo = new Label { Text = "Tipo *", Font = UITheme.SmallFont, Location = new Point(270, 38), AutoSize = true };
        panelNuevo.Controls.Add(lblTipo);

        _cboTipo = new ComboBox
        {
            Location = new Point(270, 56),
            Size = new Size(130, 26),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cboTipo.Items.Add(TipoMovimientoFinanciero.Gasto);
        _cboTipo.Items.Add(TipoMovimientoFinanciero.Ingreso);
        _cboTipo.SelectedIndex = 0;
        panelNuevo.Controls.Add(_cboTipo);

        var lblDesc = new Label { Text = "Descripción (opcional)", Font = UITheme.SmallFont, Location = new Point(415, 38), AutoSize = true };
        panelNuevo.Controls.Add(lblDesc);

        _txtDescripcion = new TextBox { Location = new Point(415, 56), Size = new Size(235, 26) };
        panelNuevo.Controls.Add(_txtDescripcion);

        _lblError = new Label
        {
            Text = string.Empty,
            Font = UITheme.SmallFont,
            ForeColor = UITheme.Danger,
            Location = new Point(15, 95),
            Size = new Size(480, 20)
        };
        panelNuevo.Controls.Add(_lblError);

        _btnCrear = new Button
        {
            Text = "Guardar Categoría",
            Location = new Point(510, 95),
            Size = new Size(140, 32)
        };
        UITheme.AplicarBotonPrimario(_btnCrear);
        _btnCrear.Click += async (s, e) => await CrearCategoriaAsync();
        panelNuevo.Controls.Add(_btnCrear);

        // Panel Grilla
        var panelGrid = new Panel
        {
            Location = new Point(20, 175),
            Size = new Size(665, 320),
            BackColor = Color.White,
            Padding = new Padding(15)
        };
        Controls.Add(panelGrid);

        var lblGridTitulo = new Label
        {
            Text = "Catálogo de Categorías Registradas",
            Font = UITheme.SectionFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(15, 10),
            AutoSize = true
        };
        panelGrid.Controls.Add(lblGridTitulo);

        _dgvCategorias = new DataGridView
        {
            Location = new Point(15, 38),
            Size = new Size(635, 265),
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        UITheme.EstilizarDataGridView(_dgvCategorias);
        _dgvCategorias.CellContentClick += async (s, e) => await OnGridCellContentClick(e);
        panelGrid.Controls.Add(_dgvCategorias);

        ConfigurarColumnasGrid();

        // Botón Cerrar
        var btnCerrar = new Button
        {
            Text = "Cerrar",
            Location = new Point(585, 505),
            Size = new Size(100, 32),
            DialogResult = DialogResult.OK
        };
        UITheme.AplicarBotonSecundario(btnCerrar);
        Controls.Add(btnCerrar);
    }

    private void ConfigurarColumnasGrid()
    {
        _dgvCategorias.Columns.Clear();

        _dgvCategorias.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", FillWeight = 15, Visible = false });
        _dgvCategorias.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nombre", HeaderText = "Nombre", FillWeight = 40 });
        _dgvCategorias.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tipo", HeaderText = "Tipo", FillWeight = 20 });
        _dgvCategorias.Columns.Add(new DataGridViewTextBoxColumn { Name = "Descripcion", HeaderText = "Descripción", FillWeight = 50 });
        _dgvCategorias.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", HeaderText = "Estado", FillWeight = 20 });

        var btnCol = new DataGridViewButtonColumn
        {
            Name = "Accion",
            HeaderText = "Acción",
            Text = "Alternar",
            UseColumnTextForButtonValue = true,
            FillWeight = 25
        };
        _dgvCategorias.Columns.Add(btnCol);
    }

    private async Task CargarCategoriasAsync()
    {
        try
        {
            var cats = await _finanzasService.ObtenerCategoriasFinancierasAsync(soloActivas: false);
            _dgvCategorias.Rows.Clear();

            foreach (var c in cats)
            {
                var idx = _dgvCategorias.Rows.Add(
                    c.Id,
                    c.Nombre,
                    c.Tipo.ToString(),
                    c.Descripcion ?? "-",
                    c.Activo ? "Activo" : "Inactivo"
                );

                if (!c.Activo)
                {
                    _dgvCategorias.Rows[idx].DefaultCellStyle.ForeColor = Color.Gray;
                }
            }
        }
        catch (Exception ex)
        {
            _lblError.Text = "Error al cargar categorías: " + ex.Message;
        }
    }

    private async Task CrearCategoriaAsync()
    {
        _lblError.Text = string.Empty;
        var nombre = _txtNombre.Text.Trim();
        var tipo = (TipoMovimientoFinanciero)_cboTipo.SelectedItem!;
        var desc = _txtDescripcion.Text.Trim();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            _lblError.Text = "El nombre de la categoría es requerido.";
            return;
        }

        _btnCrear.Enabled = false;
        try
        {
            await _finanzasService.CrearCategoriaFinancieraAsync(nombre, tipo, string.IsNullOrEmpty(desc) ? null : desc);
            _txtNombre.Clear();
            _txtDescripcion.Clear();
            await CargarCategoriasAsync();
        }
        catch (Exception ex)
        {
            _lblError.Text = ex.Message;
        }
        finally
        {
            _btnCrear.Enabled = true;
        }
    }

    private async Task OnGridCellContentClick(DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex != _dgvCategorias.Columns["Accion"].Index) return;

        var idVal = _dgvCategorias.Rows[e.RowIndex].Cells["Id"].Value;
        if (idVal == null) return;
        var id = (int)idVal;

        var estadoVal = _dgvCategorias.Rows[e.RowIndex].Cells["Estado"].Value?.ToString();
        var actualmenteActivo = estadoVal == "Activo";

        try
        {
            await _finanzasService.CambiarEstadoActivoCategoriaAsync(id, !actualmenteActivo);
            await CargarCategoriasAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cambiar estado de la categoría: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
