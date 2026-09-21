using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Constants;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class MainForm : Form
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SesionUsuario _sesion;

    private Panel _panelSidebar = null!;
    private Panel _panelContenido = null!;
    private Panel _panelHeader = null!;
    private Label _lblTituloSeccion = null!;
    private Form? _formularioActivo;
    private Button? _botonActivo;

    public MainForm(IServiceProvider serviceProvider, SesionUsuario sesion)
    {
        _serviceProvider = serviceProvider;
        _sesion = sesion;

        InitializeCustomComponents();
        CargarPantallaInicio();
    }

    private void InitializeCustomComponents()
    {
        Text = $"Sistema de Gestión de Tienda de Celulares - {_sesion.NombreCompleto} ({_sesion.RolNombre})";
        Size = new Size(1280, 800);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1024, 700);
        Font = UITheme.BodyFont;
        BackColor = UITheme.AppBg;

        // 1. Barra Lateral (Sidebar)
        _panelSidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 240,
            BackColor = UITheme.SidebarBg
        };
        Controls.Add(_panelSidebar);

        // Logo / Nombre App
        var panelLogo = new Panel { Dock = DockStyle.Top, Height = 80, Padding = new Padding(15) };
        var lblLogo = new Label
        {
            Text = "📱 CELL SHOP",
            Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(15, 25),
            AutoSize = true
        };
        panelLogo.Controls.Add(lblLogo);
        _panelSidebar.Controls.Add(panelLogo);

        // Perfil de Usuario en Sidebar
        var panelUsuario = new Panel
        {
            Dock = DockStyle.Top,
            Height = 75,
            BackColor = Color.FromArgb(20, 30, 45),
            Padding = new Padding(15, 10, 15, 10)
        };
        var lblNomUser = new Label
        {
            Text = _sesion.NombreCompleto,
            Font = UITheme.SectionFont,
            ForeColor = Color.White,
            Location = new Point(15, 12),
            AutoSize = true
        };
        panelUsuario.Controls.Add(lblNomUser);

        var lblRolUser = new Label
        {
            Text = $"Rol: {_sesion.RolNombre}",
            Font = UITheme.SmallFont,
            ForeColor = UITheme.PrimaryLight,
            Location = new Point(15, 36),
            AutoSize = true
        };
        panelUsuario.Controls.Add(lblRolUser);
        _panelSidebar.Controls.Add(panelUsuario);

        // Contenedor de Botones de Menú
        var panelMenu = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(0, 10, 0, 10)
        };

        // Menú dinámico RBAC
        CrearBotonMenu(panelMenu, "🏠 Inicio", () => CargarPantallaInicio());

        if (_sesion.TienePermiso(Permisos.UsuariosVer))
        {
            CrearBotonMenu(panelMenu, "👥 Empleados / Usuarios", () =>
            {
                var f = new UsuariosForm(
                    _serviceProvider.GetRequiredService<IUsuarioService>(),
                    _serviceProvider.GetRequiredService<IRolService>(),
                    _serviceProvider.GetRequiredService<IAuthService>(),
                    _sesion);
                AbrirFormularioHijo(f, "Gestión de Empleados y Usuarios");
            });
        }

        if (_sesion.TienePermiso(Permisos.RolesVer))
        {
            CrearBotonMenu(panelMenu, "🛡️ Roles y Permisos", () =>
            {
                var f = new RolesPermisosForm(
                    _serviceProvider.GetRequiredService<IRolService>(),
                    _sesion);
                AbrirFormularioHijo(f, "Control de Acceso y Roles (RBAC)");
            });
        }

        if (_sesion.TienePermiso(Permisos.TurnosAbrir) || _sesion.TienePermiso(Permisos.TurnosHistorial))
        {
            CrearBotonMenu(panelMenu, "💵 Caja y Turnos", () =>
            {
                var f = new HistorialTurnosForm(
                    _serviceProvider.GetRequiredService<ITurnoService>(),
                    _serviceProvider.GetRequiredService<IUsuarioService>(),
                    _sesion);
                AbrirFormularioHijo(f, "Gestión y Arqueo de Turnos de Caja");
            });
        }

        if (_sesion.TienePermiso(Permisos.ProductosVer))
        {
            CrearBotonMenu(panelMenu, "📦 Productos e Inventario", () =>
            {
                var f = new ProductosForm(
                    _serviceProvider.GetRequiredService<IProductoService>(),
                    _serviceProvider.GetRequiredService<ICategoriaService>(),
                    _serviceProvider.GetRequiredService<IEan13GeneratorService>(),
                    _sesion);
                AbrirFormularioHijo(f, "Catálogo de Productos e Inventario");
            });
        }

        if (_sesion.TienePermiso(Permisos.AlertasStockVer))
        {
            CrearBotonMenu(panelMenu, "⚠️ Alertas de Stock", () =>
            {
                var f = new AlertasStockForm(
                    _serviceProvider.GetRequiredService<IAlertaStockService>(),
                    _serviceProvider.GetRequiredService<ICompraService>(),
                    _serviceProvider.GetRequiredService<IProveedorService>(),
                    _serviceProvider.GetRequiredService<IProductoService>(),
                    _sesion);
                AbrirFormularioHijo(f, "Alertas de Stock Mínimo y Reabastecimiento");
            });
        }

        if (_sesion.TienePermiso(Permisos.ProveedoresGestionar))
        {
            CrearBotonMenu(panelMenu, "🚚 Proveedores", () =>
            {
                var f = new ProveedoresForm(
                    _serviceProvider.GetRequiredService<IProveedorService>(),
                    _sesion);
                AbrirFormularioHijo(f, "Gestión de Proveedores Comerciales");
            });
        }

        if (_sesion.TienePermiso(Permisos.ComprasRegistrar) || _sesion.TienePermiso(Permisos.ComprasHistorial))
        {
            CrearBotonMenu(panelMenu, "🛒 Compras / Inventario", () =>
            {
                var f = new HistorialComprasForm(
                    _serviceProvider.GetRequiredService<ICompraService>(),
                    _serviceProvider.GetRequiredService<IProveedorService>(),
                    _serviceProvider.GetRequiredService<IProductoService>(),
                    _sesion);
                AbrirFormularioHijo(f, "Historial y Registro de Compras");
            });
        }

        if (_sesion.TienePermiso(Permisos.VentasRegistrar) || _sesion.TienePermiso(Permisos.VentasHistorial))
        {
            CrearBotonMenu(panelMenu, "🧾 Ventas / Facturación", () => MostrarAvisoFase("Ventas y Facturación", "Punto de venta y registro de tickets con métodos de pago."));
        }

        if (_sesion.TienePermiso(Permisos.FinanzasReportesVer))
        {
            CrearBotonMenu(panelMenu, "📊 Finanzas y Reportes", () =>
            {
                var f = new FinanzasForm(
                    _serviceProvider.GetRequiredService<IFinanzasService>(),
                    _sesion);
                AbrirFormularioHijo(f, "Control Financiero, Gastos e Ingresos");
            });
        }

        if (_sesion.EsSuperAdmin)
        {
            CrearBotonMenu(panelMenu, "🌐 Red y Multi-Caja", () =>
            {
                var f = new ConfiguracionRedMultiCajaForm(
                    _serviceProvider.GetRequiredService<IConfiguracionRedService>(),
                    _serviceProvider.GetRequiredService<IDataMigrationService>());
                f.ShowDialog(this);
            });
        }

        _panelSidebar.Controls.Add(panelMenu);

        // Botón Cerrar Sesión en Bottom del Sidebar
        var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(10) };
        var btnCerrarSesion = new Button
        {
            Text = "🚪 Cerrar Sesión",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(248, 113, 113),
            BackColor = Color.FromArgb(20, 30, 45),
            Font = UITheme.SectionFont,
            Cursor = Cursors.Hand
        };
        btnCerrarSesion.FlatAppearance.BorderSize = 0;
        btnCerrarSesion.Click += (s, e) =>
        {
            var res = MessageBox.Show("¿Desea cerrar la sesión actual?", "Cerrar Sesión", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.Yes)
            {
                Application.Restart();
            }
        };
        panelBottom.Controls.Add(btnCerrarSesion);
        _panelSidebar.Controls.Add(panelBottom);

        // 2. Panel Header Superior
        _panelHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.White,
            Padding = new Padding(20, 15, 20, 15)
        };
        Controls.Add(_panelHeader);

        _lblTituloSeccion = new Label
        {
            Text = "Inicio",
            Font = UITheme.SubtitleFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(20, 18),
            AutoSize = true
        };
        _panelHeader.Controls.Add(_lblTituloSeccion);

        var lblFecha = new Label
        {
            Text = $"🇩🇴 {DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy", AppCulture.DominicoCulture)}",
            Font = UITheme.BodyFont,
            ForeColor = UITheme.TextMuted,
            Dock = DockStyle.Right,
            TextAlign = ContentAlignment.MiddleRight,
            AutoSize = true
        };
        _panelHeader.Controls.Add(lblFecha);

        // 3. Panel de Contenido Central
        _panelContenido = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UITheme.AppBg,
            Padding = new Padding(0)
        };
        Controls.Add(_panelContenido);
    }

    private void CrearBotonMenu(FlowLayoutPanel panelMenu, string texto, Action accion)
    {
        var btn = new Button
        {
            Text = $"  {texto}",
            Size = new Size(240, 44),
            FlatStyle = FlatStyle.Flat,
            ForeColor = UITheme.TextLight,
            BackColor = UITheme.SidebarBg,
            Font = UITheme.BodyFont,
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = Cursors.Hand,
            Margin = new Padding(0)
        };
        btn.FlatAppearance.BorderSize = 0;

        btn.MouseEnter += (s, e) =>
        {
            if (btn != _botonActivo) btn.BackColor = Color.FromArgb(45, 60, 85);
        };
        btn.MouseLeave += (s, e) =>
        {
            if (btn != _botonActivo) btn.BackColor = UITheme.SidebarBg;
        };

        btn.Click += (s, e) =>
        {
            if (_botonActivo != null)
            {
                _botonActivo.BackColor = UITheme.SidebarBg;
                _botonActivo.Font = UITheme.BodyFont;
            }
            _botonActivo = btn;
            _botonActivo.BackColor = UITheme.Primary;
            _botonActivo.Font = UITheme.SectionFont;

            accion();
        };

        panelMenu.Controls.Add(btn);
    }

    private void AbrirFormularioHijo(Form formHijo, string titulo)
    {
        _formularioActivo?.Close();
        _formularioActivo = formHijo;
        _lblTituloSeccion.Text = titulo;

        formHijo.TopLevel = false;
        formHijo.FormBorderStyle = FormBorderStyle.None;
        formHijo.Dock = DockStyle.Fill;
        _panelContenido.Controls.Clear();
        _panelContenido.Controls.Add(formHijo);
        formHijo.Show();
    }

    private void CargarPantallaInicio()
    {
        _lblTituloSeccion.Text = "Panel de Control";
        _panelContenido.Controls.Clear();

        var panelDashboard = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(25)
        };

        // Banner de Bienvenida
        var banner = new Panel
        {
            Dock = DockStyle.Top,
            Height = 110,
            BackColor = UITheme.Primary,
            Padding = new Padding(25)
        };

        var lblBienvenida = new Label
        {
            Text = $"¡Bienvenido al Sistema, {_sesion.NombreCompleto}!",
            Font = new Font("Segoe UI", 15F, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(25, 20),
            AutoSize = true
        };
        banner.Controls.Add(lblBienvenida);

        var lblRolDetalle = new Label
        {
            Text = $"Sesión activa con rol [{_sesion.RolNombre}]. República Dominicana (Moneda: RD$ DOP).",
            Font = UITheme.BodyFont,
            ForeColor = UITheme.PrimaryLight,
            Location = new Point(25, 55),
            AutoSize = true
        };
        banner.Controls.Add(lblRolDetalle);
        panelDashboard.Controls.Add(banner);

        // Tarjetas informativas de estado de módulos
        var flowCards = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 300,
            Padding = new Padding(0, 20, 0, 0),
            AutoSize = true
        };

        flowCards.Controls.Add(CrearTarjetaEstado("🛡️ Seguridad y RBAC (F1)", "ACTIVO", "Usuarios, permisos granulares y contraseñas BCrypt.", UITheme.Success));
        flowCards.Controls.Add(CrearTarjetaEstado("💵 Turnos y Caja (F2)", "ACTIVO", "Apertura, arqueo y cierre con cálculo de diferencias.", UITheme.Success));
        flowCards.Controls.Add(CrearTarjetaEstado("📦 Catálogo y Stock (F3-F6)", "ACTIVO", "SKU automático, proveedores, compras y alertas stock.", UITheme.Success));
        flowCards.Controls.Add(CrearTarjetaEstado("📊 Finanzas y Gastos (F7)", "ACTIVO", "Gastos operativos, ingresos y balance neto en RD$.", UITheme.Success));

        panelDashboard.Controls.Add(flowCards);
        _panelContenido.Controls.Add(panelDashboard);
    }

    private Panel CrearTarjetaEstado(string titulo, string badge, string detalle, Color badgeColor)
    {
        var p = new Panel
        {
            Size = new Size(270, 140),
            BackColor = Color.White,
            Margin = new Padding(0, 0, 20, 20),
            Padding = new Padding(15)
        };

        var lblTit = new Label
        {
            Text = titulo,
            Font = UITheme.SectionFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(15, 15),
            Size = new Size(240, 22)
        };
        p.Controls.Add(lblTit);

        var lblBadge = new Label
        {
            Text = $" {badge} ",
            Font = UITheme.BadgeFont,
            ForeColor = Color.White,
            BackColor = badgeColor,
            Location = new Point(15, 42),
            AutoSize = true,
            Padding = new Padding(4, 2, 4, 2)
        };
        p.Controls.Add(lblBadge);

        var lblDet = new Label
        {
            Text = detalle,
            Font = UITheme.SmallFont,
            ForeColor = UITheme.TextMuted,
            Location = new Point(15, 72),
            Size = new Size(240, 50)
        };
        p.Controls.Add(lblDet);

        return p;
    }

    private void MostrarAvisoFase(string nombreFase, string descripcion)
    {
        _lblTituloSeccion.Text = nombreFase;
        _panelContenido.Controls.Clear();

        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(40) };
        var card = new Panel { Size = new Size(600, 240), Location = new Point(40, 30), BackColor = Color.White, Padding = new Padding(25) };

        var lblTit = new Label { Text = $"🚀 Módulo: {nombreFase}", Font = UITheme.TitleFont, ForeColor = UITheme.DarkBg, Location = new Point(20, 20), AutoSize = true };
        card.Controls.Add(lblTit);

        var lblDesc = new Label { Text = descripcion, Font = UITheme.BodyFont, ForeColor = UITheme.TextMuted, Location = new Point(20, 60), Size = new Size(550, 50) };
        card.Controls.Add(lblDesc);

        var lblAviso = new Label { Text = "Este módulo corresponde a las fases siguientes del roadmap en `PROJECT_CONTEXT_RULES.md` y se construirá en orden secuencial tras la aprobación.", Font = UITheme.SmallFont, ForeColor = UITheme.Primary, Location = new Point(20, 120), Size = new Size(550, 40) };
        card.Controls.Add(lblAviso);

        panel.Controls.Add(card);
        _panelContenido.Controls.Add(panel);
    }
}
