using System.Drawing;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Constants;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;

namespace SistemaCelulares.App.Forms;

public class RolModalForm : Form
{
    private readonly IRolService _rolService;
    private readonly int? _rolIdParaEditar;

    private TextBox _txtNombre = null!;
    private TextBox _txtDescripcion = null!;
    private TableLayoutPanel _panelPermisos = null!;
    private readonly Dictionary<string, CheckBox> _checkPermisos = new(StringComparer.OrdinalIgnoreCase);
    private Label _lblError = null!;
    private Button _btnGuardar = null!;
    private Rol? _rolCargado;

    public RolModalForm(IRolService rolService, int? rolId = null)
    {
        _rolService = rolService;
        _rolIdParaEditar = rolId;
        InitializeCustomComponents();
        Load += async (s, e) => await CargarDatosAsync();
    }

    private void InitializeCustomComponents()
    {
        Text = _rolIdParaEditar.HasValue ? "Editar Rol y Permisos" : "Crear Rol Personalizado";
        Size = new Size(620, 680);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.AppBg;
        Font = UITheme.BodyFont;

        var panelPrincipal = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };
        Controls.Add(panelPrincipal);

        // Header
        var lblTitulo = new Label
        {
            Text = _rolIdParaEditar.HasValue ? "Configurar Rol de Acceso" : "Nuevo Rol Personalizado",
            Font = UITheme.SubtitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(0, 0),
            AutoSize = true
        };
        panelPrincipal.Controls.Add(lblTitulo);

        // Nombre
        var lblNom = new Label { Text = "Nombre del Rol *", Font = UITheme.SectionFont, Location = new Point(0, 35), AutoSize = true };
        panelPrincipal.Controls.Add(lblNom);

        _txtNombre = new TextBox { Location = new Point(0, 58), Size = new Size(560, 28) };
        panelPrincipal.Controls.Add(_txtNombre);

        // Descripción
        var lblDesc = new Label { Text = "Descripción / Propósito", Font = UITheme.BodyFont, Location = new Point(0, 92), AutoSize = true };
        panelPrincipal.Controls.Add(lblDesc);

        _txtDescripcion = new TextBox { Location = new Point(0, 114), Size = new Size(560, 28) };
        panelPrincipal.Controls.Add(_txtDescripcion);

        // Sección de Permisos
        var lblPerm = new Label { Text = "Catálogo de Permisos Asignados (RBAC):", Font = UITheme.SectionFont, Location = new Point(0, 150), AutoSize = true };
        panelPrincipal.Controls.Add(lblPerm);

        var contenedorScroll = new Panel
        {
            Location = new Point(0, 175),
            Size = new Size(560, 360),
            AutoScroll = true,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(10)
        };

        _panelPermisos = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            Padding = new Padding(5)
        };
        contenedorScroll.Controls.Add(_panelPermisos);
        panelPrincipal.Controls.Add(contenedorScroll);

        // Error
        _lblError = new Label { Text = string.Empty, Font = UITheme.SmallFont, ForeColor = UITheme.Danger, Location = new Point(0, 545), Size = new Size(560, 25) };
        panelPrincipal.Controls.Add(_lblError);

        // Botones
        var panelBotones = new FlowLayoutPanel
        {
            Location = new Point(0, 575),
            Size = new Size(560, 45),
            FlowDirection = FlowDirection.RightToLeft
        };

        var btnCancelar = new Button { Text = "Cancelar", Size = new Size(95, 38), DialogResult = DialogResult.Cancel };
        UITheme.AplicarBotonSecundario(btnCancelar);
        panelBotones.Controls.Add(btnCancelar);

        _btnGuardar = new Button { Text = "Guardar Rol", Size = new Size(120, 38) };
        UITheme.AplicarBotonPrimario(_btnGuardar);
        _btnGuardar.Click += async (s, e) => await GuardarRolAsync();
        panelBotones.Controls.Add(_btnGuardar);

        panelPrincipal.Controls.Add(panelBotones);
    }

    private async Task CargarDatosAsync()
    {
        var todosPermisos = await _rolService.ObtenerTodosLosPermisosAsync();
        var grupos = todosPermisos.GroupBy(p => p.Modulo).OrderBy(g => g.Key);

        _panelPermisos.Controls.Clear();
        _checkPermisos.Clear();

        foreach (var grupo in grupos)
        {
            // Encabezado de módulo
            var lblModulo = new Label
            {
                Text = $"📁 Módulo: {grupo.Key}",
                Font = UITheme.SectionFont,
                ForeColor = UITheme.Primary,
                Margin = new Padding(0, 10, 0, 4),
                AutoSize = true
            };
            _panelPermisos.Controls.Add(lblModulo);

            foreach (var perm in grupo)
            {
                var chk = new CheckBox
                {
                    Text = $"{perm.Descripcion} ({perm.Codigo})",
                    Font = UITheme.BodyFont,
                    AutoSize = true,
                    Margin = new Padding(15, 2, 0, 2),
                    Tag = perm.Codigo
                };
                _checkPermisos[perm.Codigo] = chk;
                _panelPermisos.Controls.Add(chk);
            }
        }

        if (_rolIdParaEditar.HasValue)
        {
            _rolCargado = await _rolService.ObtenerPorIdAsync(_rolIdParaEditar.Value);
            if (_rolCargado != null)
            {
                _txtNombre.Text = _rolCargado.Nombre;
                _txtDescripcion.Text = _rolCargado.Descripcion ?? string.Empty;

                if (_rolCargado.EsFijo)
                {
                    _txtNombre.ReadOnly = true; // No modificar nombre de roles fijos
                }

                if (_rolCargado.Nombre == Rol.SuperAdmin)
                {
                    foreach (var chk in _checkPermisos.Values)
                    {
                        chk.Checked = true;
                        chk.Enabled = false;
                    }
                }
                else
                {
                    var codigosActuales = _rolCargado.RolPermisos.Select(rp => rp.Permiso.Codigo).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    foreach (var kvp in _checkPermisos)
                    {
                        kvp.Value.Checked = codigosActuales.Contains(kvp.Key);
                    }
                }
            }
        }
    }

    private async Task GuardarRolAsync()
    {
        _lblError.Text = string.Empty;
        var nombre = _txtNombre.Text.Trim();
        var descripcion = _txtDescripcion.Text.Trim();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            _lblError.Text = "El nombre del rol es obligatorio.";
            return;
        }

        var permisosSeleccionados = _checkPermisos
            .Where(p => p.Value.Checked)
            .Select(p => p.Key)
            .ToList();

        _btnGuardar.Enabled = false;
        try
        {
            if (_rolIdParaEditar.HasValue)
            {
                var ok = await _rolService.ActualizarRolAsync(_rolIdParaEditar.Value, nombre, descripcion, permisosSeleccionados);
                if (ok)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            else
            {
                await _rolService.CrearRolPersonalizadoAsync(nombre, descripcion, permisosSeleccionados);
                DialogResult = DialogResult.OK;
                Close();
            }
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
