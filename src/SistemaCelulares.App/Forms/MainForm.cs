using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Constants;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class MainForm : Form
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SesionUsuario _sesion;

    private Panel _panelTopWindow = null!;
    private Panel _panelSidebar = null!;
    private Panel _panelContenido = null!;
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
        Text = $"Veyra POS ({_sesion.NombreCompleto} - {_sesion.RolNombre})";
        Size = new Size(1280, 800);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1024, 700);
        Font = UITheme.BodyFont;
        BackColor = UITheme.AppBg;
        try { var ico = Icon.ExtractAssociatedIcon(Application.ExecutablePath); if (ico != null) Icon = ico; } catch { }

        // 1. Barra superior tipo ventana / marca
        _panelTopWindow = new Panel
        {
            Dock = DockStyle.Top,
            Height = 42,
            BackColor = UITheme.Surface1,
            Padding = new Padding(16, 0, 16, 0)
        };

        var pnlBrand = new Panel { Dock = DockStyle.Left, Width = 350, BackColor = Color.Transparent };
        var lblBrand = new Label
        {
            Text = "📱 Veyra POS",
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            ForeColor = UITheme.Primary,
            Location = new Point(0, 10),
            AutoSize = true
        };
        pnlBrand.Controls.Add(lblBrand);
        _panelTopWindow.Controls.Add(pnlBrand);

        var pnlDots = new Panel { Dock = DockStyle.Right, Width = 60, BackColor = Color.Transparent };
        pnlDots.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var b1 = new SolidBrush(UITheme.BorderStrong);
            using var b2 = new SolidBrush(UITheme.BorderStrong);
            using var b3 = new SolidBrush(UITheme.Danger);

            e.Graphics.FillEllipse(b1, 5, 16, 10, 10);
            e.Graphics.FillEllipse(b2, 22, 16, 10, 10);
            e.Graphics.FillEllipse(b3, 39, 16, 10, 10);
        };
        _panelTopWindow.Controls.Add(pnlDots);

        // Separador inferior de TopWindow
        var sepTop = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = UITheme.Border };
        _panelTopWindow.Controls.Add(sepTop);

        // 2. Barra Lateral (Sidebar)
        _panelSidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 205,
            BackColor = UITheme.Surface1
        };

        var sepRightSidebar = new Panel { Dock = DockStyle.Right, Width = 1, BackColor = UITheme.Border };
        _panelSidebar.Controls.Add(sepRightSidebar);

        // Perfil de Usuario en Sidebar (Tarjeta Avatar con iniciales)
        var panelUsuario = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = UITheme.Surface1,
            Padding = new Padding(16, 12, 16, 10),
            Cursor = Cursors.Hand
        };

        string iniciales = ObtenerIniciales(_sesion.NombreCompleto);
        Color avatarBg = _sesion.EsSuperAdmin ? UITheme.BgPro : (_sesion.RolNombre == Rol.Admin ? UITheme.BgSuccess : (_sesion.RolNombre == Rol.Tecnico ? UITheme.BgPro : UITheme.BgAccent));
        Color avatarFg = _sesion.EsSuperAdmin ? UITheme.Pro : (_sesion.RolNombre == Rol.Admin ? UITheme.Success : (_sesion.RolNombre == Rol.Tecnico ? UITheme.Pro : UITheme.Primary));

        var pnlAvatar = new Panel
        {
            Size = new Size(34, 34),
            Location = new Point(14, 14),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };
        pnlAvatar.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var b = new SolidBrush(avatarBg);
            e.Graphics.FillEllipse(b, 0, 0, 33, 33);
            using var f = new SolidBrush(avatarFg);
            using var font = new Font("Segoe UI", 9F, FontStyle.Bold);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(ObtenerIniciales(_sesion.NombreCompleto), font, f, new RectangleF(0, 0, 34, 34), sf);
        };

        var lblNomUser = new Label
        {
            Text = _sesion.NombreCompleto,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = UITheme.TextPrimary,
            Location = new Point(54, 13),
            AutoSize = true,
            Cursor = Cursors.Hand
        };

        var lblRolUser = new Label
        {
            Text = _sesion.RolNombre,
            Font = new Font("Segoe UI", 8F, FontStyle.Regular),
            ForeColor = _sesion.RolNombre == Rol.Admin ? UITheme.Success : (_sesion.RolNombre == Rol.Tecnico ? UITheme.Pro : UITheme.TextSecondary),
            Location = new Point(54, 31),
            AutoSize = true,
            Cursor = Cursors.Hand
        };

        Action accionPerfil = () => AbrirMiPerfil(lblNomUser, pnlAvatar);
        panelUsuario.Click += (s, e) => accionPerfil();
        pnlAvatar.Click += (s, e) => accionPerfil();
        lblNomUser.Click += (s, e) => accionPerfil();
        lblRolUser.Click += (s, e) => accionPerfil();

        panelUsuario.Controls.Add(pnlAvatar);
        panelUsuario.Controls.Add(lblNomUser);
        panelUsuario.Controls.Add(lblRolUser);

        var sepUser = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = UITheme.Border };
        panelUsuario.Controls.Add(sepUser);

        // Contenedor de Botones de Menú
        var panelMenu = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(0, 4, 0, 4),
            BackColor = UITheme.Surface1
        };

        // Construir Menú adaptado al Rol
        ConstruirBotonesMenuPorRol(panelMenu);

        // Panel Bottom: Mi Perfil + Botón Cerrar Sesión
        var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 80, Padding = new Padding(10, 4, 10, 4), BackColor = UITheme.Surface1 };
        var sepBottom = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = UITheme.Border };
        panelBottom.Controls.Add(sepBottom);

        var btnMiPerfil = new Button
        {
            Text = "👤  Mi perfil y clave",
            Dock = DockStyle.Top,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            ForeColor = UITheme.TextSecondary,
            BackColor = UITheme.Surface1,
            Font = UITheme.BodyFont,
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = Cursors.Hand
        };
        btnMiPerfil.FlatAppearance.BorderSize = 0;
        btnMiPerfil.MouseEnter += (s, e) => { btnMiPerfil.ForeColor = UITheme.Primary; btnMiPerfil.BackColor = UITheme.BgAccent; };
        btnMiPerfil.MouseLeave += (s, e) => { btnMiPerfil.ForeColor = UITheme.TextSecondary; btnMiPerfil.BackColor = UITheme.Surface1; };
        btnMiPerfil.Click += (s, e) => accionPerfil();
        panelBottom.Controls.Add(btnMiPerfil);

        var btnCerrarSesion = new Button
        {
            Text = "🚪  Cerrar sesión",
            Dock = DockStyle.Bottom,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            ForeColor = UITheme.TextSecondary,
            BackColor = UITheme.Surface1,
            Font = UITheme.BodyFont,
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = Cursors.Hand
        };
        btnCerrarSesion.FlatAppearance.BorderSize = 0;
        btnCerrarSesion.MouseEnter += (s, e) => { btnCerrarSesion.ForeColor = UITheme.Danger; btnCerrarSesion.BackColor = UITheme.BgDanger; };
        btnCerrarSesion.MouseLeave += (s, e) => { btnCerrarSesion.ForeColor = UITheme.TextSecondary; btnCerrarSesion.BackColor = UITheme.Surface1; };
        btnCerrarSesion.Click += (s, e) =>
        {
            var res = MessageBox.Show("¿Desea cerrar la sesión actual?", "Cerrar Sesión", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.Yes)
            {
                Application.Restart();
            }
        };
        panelBottom.Controls.Add(btnCerrarSesion);

        // Orden de acoplamiento en sidebar: Fill primero, luego Bottom y Top
        _panelSidebar.Controls.Add(panelMenu);
        _panelSidebar.Controls.Add(panelBottom);
        _panelSidebar.Controls.Add(panelUsuario);

        // 3. Panel de Contenido Central
        _panelContenido = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UITheme.Surface2,
            Padding = new Padding(0)
        };

        // Orden de Docking en WinForms
        Controls.Add(_panelContenido);
        Controls.Add(_panelSidebar);
        Controls.Add(_panelTopWindow);
    }

    private void ConstruirBotonesMenuPorRol(FlowLayoutPanel panelMenu)
    {
        CrearBotonMenu(panelMenu, "🏠  Inicio", () => CargarPantallaInicio(), true);

        if (_sesion.RolNombre == Rol.Cajero)
        {
            // Menú Cajero: Ventas, Caja y Turno, Historial de Ventas, Clientes, Reparaciones (solo cobrar)
            CrearBotonMenu(panelMenu, "🛒  Ventas", () => AbrirVentasPos());
            CrearBotonMenu(panelMenu, "💵  Caja y Turno", () => AbrirCajaTurnos());
            CrearBotonMenu(panelMenu, "🧾  Historial de Ventas", () => AbrirHistorialVentas());
            CrearBotonMenu(panelMenu, "👥  Clientes", () => AbrirClientes());
            CrearBotonMenu(panelMenu, "🔧  Reparaciones (solo cobrar)", () => AbrirReparaciones(soloCobrar: true));
        }
        else if (_sesion.RolNombre == Rol.Tecnico)
        {
            // Menú Técnico: Reparaciones, Clientes, Productos (consulta), Inventario (consulta), Ventas / Caja (deshabilitado)
            CrearBotonMenu(panelMenu, "🔧  Reparaciones", () => AbrirReparaciones(soloCobrar: false));
            CrearBotonMenu(panelMenu, "👥  Clientes", () => AbrirClientes());
            CrearBotonMenu(panelMenu, "🏷️  Productos (consulta)", () => AbrirProductos(soloLectura: true));
            CrearBotonMenu(panelMenu, "📦  Inventario (consulta)", () => AbrirInventario(soloLectura: true));
            CrearBotonMenuDeshabilitado(panelMenu, "🛒  Ventas / Caja");
        }
        else if (_sesion.RolNombre == Rol.Admin)
        {
            // Menú Admin (Dueño)
            CrearBotonMenu(panelMenu, "🛒  Ventas", () => AbrirVentasPos());
            CrearBotonMenu(panelMenu, "💵  Caja y Turnos", () => AbrirCajaTurnos());
            CrearBotonMenu(panelMenu, "🧾  Historial de Ventas", () => AbrirHistorialVentas());
            CrearBotonMenu(panelMenu, "🔧  Reparaciones", () => AbrirReparaciones(soloCobrar: false));
            CrearBotonMenu(panelMenu, "👥  Clientes", () => AbrirClientes());
            CrearBotonMenu(panelMenu, "🏷️  Productos", () => AbrirProductos(soloLectura: false));
            CrearBotonMenu(panelMenu, "📦  Inventario", () => AbrirInventario(soloLectura: false));
            CrearBotonMenu(panelMenu, "🚚  Proveedores", () => AbrirProveedores());
            CrearBotonMenu(panelMenu, "💳  Gastos e ingresos", () => AbrirFinanzas());
            CrearBotonMenu(panelMenu, "👤  Usuarios y roles", () => AbrirUsuarios());
            CrearBotonMenu(panelMenu, "📊  Reportes", () => AbrirCajaTurnos());
            CrearBotonMenu(panelMenu, "⚙️  Configuración", () => AbrirConfiguracionAdmin());
        }
        else // Super Admin
        {
            // Menú Super Admin (Acceso Total y Soporte Técnico)
            CrearBotonMenu(panelMenu, "🛒  Ventas", () => AbrirVentasPos());
            CrearBotonMenu(panelMenu, "💵  Caja y Turnos", () => AbrirCajaTurnos());
            CrearBotonMenu(panelMenu, "🧾  Historial de Ventas", () => AbrirHistorialVentas());
            CrearBotonMenu(panelMenu, "🔧  Reparaciones", () => AbrirReparaciones(soloCobrar: false));
            CrearBotonMenu(panelMenu, "👥  Clientes", () => AbrirClientes());
            CrearBotonMenu(panelMenu, "🏷️  Productos", () => AbrirProductos(soloLectura: false));
            CrearBotonMenu(panelMenu, "📦  Inventario", () => AbrirInventario(soloLectura: false));
            CrearBotonMenu(panelMenu, "🚚  Proveedores", () => AbrirProveedores());
            CrearBotonMenu(panelMenu, "💳  Gastos e ingresos", () => AbrirFinanzas());
            CrearBotonMenu(panelMenu, "👤  Usuarios y roles", () => AbrirUsuarios());
            CrearBotonMenu(panelMenu, "📊  Reportes", () => AbrirCajaTurnos());
            CrearBotonMenu(panelMenu, "🌐  Configuración (Super Admin)", () => AbrirConfiguracionSuperAdmin(), esPro: true);
        }
    }

    private void CrearBotonMenu(FlowLayoutPanel panelMenu, string texto, Action accion, bool activoInicial = false, bool esPro = false)
    {
        var btn = new Button
        {
            Text = texto,
            Size = new Size(204, 38),
            FlatStyle = FlatStyle.Flat,
            ForeColor = activoInicial ? (esPro || _sesion.RolNombre == Rol.Tecnico ? UITheme.Pro : UITheme.Primary) : UITheme.TextSecondary,
            BackColor = activoInicial ? (esPro || _sesion.RolNombre == Rol.Tecnico ? UITheme.BgPro : UITheme.BgAccent) : UITheme.Surface1,
            Font = activoInicial ? UITheme.BodyBoldFont : UITheme.BodyFont,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 1, 0, 1)
        };
        btn.FlatAppearance.BorderSize = 0;

        if (activoInicial)
        {
            _botonActivo = btn;
        }

        btn.Paint += (s, e) =>
        {
            if (btn == _botonActivo)
            {
                using var p = new Pen(esPro || _sesion.RolNombre == Rol.Tecnico ? UITheme.Pro : UITheme.Primary, 3);
                e.Graphics.DrawLine(p, 0, 0, 0, btn.Height);
            }
        };

        btn.MouseEnter += (s, e) =>
        {
            if (btn != _botonActivo)
            {
                btn.BackColor = UITheme.Surface2;
                btn.ForeColor = UITheme.TextPrimary;
            }
        };
        btn.MouseLeave += (s, e) =>
        {
            if (btn != _botonActivo)
            {
                btn.BackColor = UITheme.Surface1;
                btn.ForeColor = UITheme.TextSecondary;
            }
        };

        btn.Click += (s, e) =>
        {
            if (_botonActivo != null)
            {
                _botonActivo.BackColor = UITheme.Surface1;
                _botonActivo.ForeColor = UITheme.TextSecondary;
                _botonActivo.Font = UITheme.BodyFont;
                _botonActivo.Invalidate();
            }

            _botonActivo = btn;
            _botonActivo.BackColor = esPro || _sesion.RolNombre == Rol.Tecnico ? UITheme.BgPro : UITheme.BgAccent;
            _botonActivo.ForeColor = esPro || _sesion.RolNombre == Rol.Tecnico ? UITheme.Pro : UITheme.Primary;
            _botonActivo.Font = UITheme.BodyBoldFont;
            _botonActivo.Invalidate();

            accion();
        };

        panelMenu.Controls.Add(btn);
    }

    private void CrearBotonMenuDeshabilitado(FlowLayoutPanel panelMenu, string texto)
    {
        var btn = new Button
        {
            Text = texto,
            Size = new Size(204, 38),
            FlatStyle = FlatStyle.Flat,
            ForeColor = UITheme.TextMuted,
            BackColor = UITheme.Surface1,
            Font = UITheme.BodyFont,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            Enabled = false,
            Margin = new Padding(0, 1, 0, 1)
        };
        btn.FlatAppearance.BorderSize = 0;
        panelMenu.Controls.Add(btn);
    }

    private void AbrirFormularioHijo(Form formHijo, string titulo)
    {
        _formularioActivo?.Close();
        _formularioActivo = formHijo;

        if (formHijo is PuntoVentaForm posForm)
        {
            _panelSidebar.Visible = false;
            _panelTopWindow.Visible = false;
            posForm.OnSalirPos = () =>
            {
                _panelSidebar.Visible = true;
                _panelTopWindow.Visible = true;
                CargarPantallaInicio();
            };
        }
        else
        {
            _panelSidebar.Visible = true;
            _panelTopWindow.Visible = true;
        }

        formHijo.TopLevel = false;
        formHijo.FormBorderStyle = FormBorderStyle.None;
        formHijo.Dock = DockStyle.Fill;
        _panelContenido.Controls.Clear();
        _panelContenido.Controls.Add(formHijo);
        formHijo.Show();
    }

    private string ObtenerIniciales(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return "US";
        var partes = nombre.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length == 1) return partes[0].Substring(0, Math.Min(2, partes[0].Length)).ToUpper();
        return $"{partes[0][0]}{partes[1][0]}".ToUpper();
    }

    // ==========================================
    // Carga de Vistas de Navegación
    // ==========================================
    private async void AbrirVentasPos()
    {
        var turnoService = _serviceProvider.GetRequiredService<ITurnoService>();
        var turnoActivo = await turnoService.ObtenerTurnoAbiertoAsync(_sesion.UsuarioId);
        if (turnoActivo == null)
        {
            var res = MessageBox.Show(
                "Para operar el Punto de Venta (POS) es obligatorio tener una caja/turno abierto.\n\n¿Desea abrir un turno de caja ahora?",
                "Caja Cerrada — Veyra POS",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (res == DialogResult.Yes)
            {
                using var dlg = new AbrirTurnoModalForm(turnoService, _sesion);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    turnoActivo = await turnoService.ObtenerTurnoAbiertoAsync(_sesion.UsuarioId);
                }
            }

            if (turnoActivo == null)
            {
                return;
            }
        }

        var f = new PuntoVentaForm(
            _serviceProvider.GetRequiredService<IVentaService>(),
            _serviceProvider.GetRequiredService<IProductoService>(),
            _serviceProvider.GetRequiredService<IPagoService>(),
            turnoService,
            _serviceProvider.GetRequiredService<IClienteService>(),
            _sesion,
            _serviceProvider);
        AbrirFormularioHijo(f, "Punto de Venta");
    }

    private void AbrirClientes()
    {
        var f = new ClientesForm(
            _serviceProvider.GetRequiredService<IClienteService>(),
            _sesion);
        AbrirFormularioHijo(f, "Clientes");
    }

    private void AbrirReparaciones(bool soloCobrar)
    {
        var f = new ReparacionesForm(
            _serviceProvider.GetRequiredService<IReparacionService>(),
            _serviceProvider.GetRequiredService<IClienteService>(),
            _serviceProvider.GetRequiredService<ITurnoService>(),
            _serviceProvider.GetRequiredService<IPagoService>(),
            _sesion);
        AbrirFormularioHijo(f, "Reparaciones");
    }

    private void AbrirProductos(bool soloLectura)
    {
        var f = new ProductosForm(
            _serviceProvider.GetRequiredService<IProductoService>(),
            _serviceProvider.GetRequiredService<ICategoriaService>(),
            _serviceProvider.GetRequiredService<IEan13GeneratorService>(),
            _sesion);
        AbrirFormularioHijo(f, "Catálogo de Productos");
    }

    private void AbrirInventario(bool soloLectura)
    {
        var f = new InventarioForm(
            _serviceProvider.GetRequiredService<IProductoService>(),
            _serviceProvider.GetRequiredService<ICategoriaService>(),
            _serviceProvider.GetRequiredService<ICompraService>(),
            _serviceProvider.GetRequiredService<IProveedorService>(),
            _serviceProvider.GetRequiredService<IAlertaStockService>(),
            _sesion,
            soloLectura);
        AbrirFormularioHijo(f, "Gestión de Inventario y Stock");
    }

    private void AbrirRegistrarCompra()
    {
        var compraService = _serviceProvider.GetRequiredService<ICompraService>();
        var proveedorService = _serviceProvider.GetRequiredService<IProveedorService>();
        var productoService = _serviceProvider.GetRequiredService<IProductoService>();
        using var f = new RegistrarCompraForm(compraService, proveedorService, productoService, _sesion);
        if (f.ShowDialog(this) == DialogResult.OK)
        {
            CargarPantallaInicio();
        }
    }

    private void AbrirMiPerfil(Label lblNomUser, Panel pnlAvatar)
    {
        var usuarioService = _serviceProvider.GetRequiredService<IUsuarioService>();
        var authService = _serviceProvider.GetRequiredService<IAuthService>();
        using var modal = new MiPerfilModalForm(usuarioService, authService, _sesion);
        if (modal.ShowDialog(this) == DialogResult.OK && modal.PerfilActualizado)
        {
            lblNomUser.Text = _sesion.NombreCompleto;
            pnlAvatar.Invalidate();
            Text = $"Veyra POS ({_sesion.NombreCompleto} - {_sesion.RolNombre})";
            CargarPantallaInicio();
        }
    }

    private void AbrirProveedores()
    {
        var f = new ProveedoresForm(
            _serviceProvider.GetRequiredService<IProveedorService>(),
            _sesion);
        AbrirFormularioHijo(f, "Proveedores");
    }

    private void AbrirFinanzas()
    {
        var f = new FinanzasForm(
            _serviceProvider.GetRequiredService<IFinanzasService>(),
            _sesion);
        AbrirFormularioHijo(f, "Finanzas");
    }

    private void AbrirUsuarios()
    {
        var f = new UsuariosForm(
            _serviceProvider.GetRequiredService<IUsuarioService>(),
            _serviceProvider.GetRequiredService<IRolService>(),
            _serviceProvider.GetRequiredService<IAuthService>(),
            _sesion);
        AbrirFormularioHijo(f, "Usuarios");
    }

    private void AbrirCajaTurnos()
    {
        var f = new HistorialTurnosForm(
            _serviceProvider.GetRequiredService<ITurnoService>(),
            _serviceProvider.GetRequiredService<IUsuarioService>(),
            _serviceProvider.GetRequiredService<IVentaService>(),
            _sesion);
        AbrirFormularioHijo(f, "Caja y Turnos");
    }

    private void AbrirHistorialVentas()
    {
        var f = new HistorialVentasForm(
            _serviceProvider.GetRequiredService<IVentaService>(),
            _sesion);
        AbrirFormularioHijo(f, "Historial de Ventas");
    }

    private void AbrirHistorialTurnos()
    {
        AbrirCajaTurnos();
    }

    private void AbrirConfiguracionAdmin()
    {
        var f = new ConfiguracionSistemaForm(
            _serviceProvider.GetRequiredService<IConfiguracionNegocioService>(),
            _serviceProvider.GetRequiredService<IBackupService>(),
            _serviceProvider.GetRequiredService<IConfiguracionRedService>(),
            _serviceProvider.GetRequiredService<IRolService>(),
            _sesion);
        AbrirFormularioHijo(f, "Configuración del Sistema");
    }

    private void AbrirConfiguracionSuperAdmin()
    {
        var f = new ConfiguracionSistemaForm(
            _serviceProvider.GetRequiredService<IConfiguracionNegocioService>(),
            _serviceProvider.GetRequiredService<IBackupService>(),
            _serviceProvider.GetRequiredService<IConfiguracionRedService>(),
            _serviceProvider.GetRequiredService<IRolService>(),
            _sesion);
        AbrirFormularioHijo(f, "Configuración (Super Admin)");
    }

    // ==========================================
    // PANTALLAS DE INICIO SEGÚN ROL (MOCKUP EXACTO)
    // ==========================================
    private async void CargarPantallaInicio()
    {
        _panelSidebar.Visible = true;
        _panelTopWindow.Visible = true;
        _panelContenido.Controls.Clear();

        var pnlScroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = UITheme.Surface2,
            Padding = new Padding(24, 20, 24, 24)
        };

        if (_sesion.RolNombre == Rol.Cajero)
        {
            await RenderDashboardCajeroAsync(pnlScroll);
        }
        else if (_sesion.RolNombre == Rol.Tecnico)
        {
            await RenderDashboardTecnicoAsync(pnlScroll);
        }
        else if (_sesion.RolNombre == Rol.Admin)
        {
            await RenderDashboardAdminAsync(pnlScroll);
        }
        else
        {
            await RenderDashboardSuperAdminAsync(pnlScroll);
        }

        _panelContenido.Controls.Add(pnlScroll);
    }

    // 1. Dashboard Cajero
    private async Task RenderDashboardCajeroAsync(Panel container)
    {
        var turnoService = _serviceProvider.GetRequiredService<ITurnoService>();
        var reparacionService = _serviceProvider.GetRequiredService<IReparacionService>();
        var ventaService = _serviceProvider.GetRequiredService<IVentaService>();

        var turno = await turnoService.ObtenerTurnoAbiertoAsync(_sesion.UsuarioId);
        var reparacionesListas = await reparacionService.ObtenerTodasAsync(EstadoReparacion.ListaParaEntrega);
        var ventasHoy = await ventaService.ObtenerHistorialAsync(desde: DateTime.UtcNow.Date);

        decimal totalVentasHoy = ventasHoy.Where(v => v.Estado != EstadoVenta.Anulada).Sum(v => v.Total);
        int transaccionesHoy = ventasHoy.Count(v => v.Estado != EstadoVenta.Anulada);

        // Header Turno
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.Transparent };
        var lblTurnoTit = new Label { Text = turno != null ? "Turno abierto" : "Sin turno abierto", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = UITheme.TextPrimary, Location = new Point(0, 4), AutoSize = true };
        var lblTurnoSub = new Label { Text = turno != null ? $"Apertura: RD${turno.MontoApertura:N2} · {turno.FechaApertura.ToLocalTime():h:mm tt}" : "Debe abrir turno para operar ventas", Font = UITheme.SmallFont, ForeColor = UITheme.TextSecondary, Location = new Point(0, 28), AutoSize = true };
        pnlHeader.Controls.Add(lblTurnoTit);
        pnlHeader.Controls.Add(lblTurnoSub);

        if (turno != null)
        {
            var btnCerrarCaja = new Button { Text = "🔒 Cerrar caja", Size = new Size(110, 32), Location = new Point(container.Width - 160, 8), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            UITheme.AplicarBotonPeligro(btnCerrarCaja);
            btnCerrarCaja.Click += (s, e) =>
            {
                using var f = new CerrarTurnoModalForm(turnoService, _sesion, turno);
                if (f.ShowDialog(this) == DialogResult.OK) CargarPantallaInicio();
            };
            pnlHeader.Controls.Add(btnCerrarCaja);
        }
        else
        {
            var btnAbrirCaja = new Button { Text = "💵 Abrir turno", Size = new Size(110, 32), Location = new Point(container.Width - 160, 8), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            UITheme.AplicarBotonPrimario(btnAbrirCaja);
            btnAbrirCaja.Click += (s, e) =>
            {
                using var f = new AbrirTurnoModalForm(turnoService, _sesion);
                if (f.ShowDialog(this) == DialogResult.OK) CargarPantallaInicio();
            };
            pnlHeader.Controls.Add(btnAbrirCaja);
        }

        container.Controls.Add(pnlHeader);

        // Grid 3 Tarjetas métricas
        var pnlMetrics = CrearGridTarjetas(3, 85, new (string title, string val, Color bg, Color textCol)[]
        {
            ("Ventas de hoy", $"RD${totalVentasHoy:N0}", UITheme.Surface1, UITheme.TextPrimary),
            ("Transacciones", $"{transaccionesHoy}", UITheme.Surface1, UITheme.TextPrimary),
            ("Reparaciones listas para cobrar", $"{reparacionesListas.Count}", UITheme.BgAccent, UITheme.Primary)
        });
        pnlMetrics.Dock = DockStyle.Top;
        container.Controls.Add(pnlMetrics);

        // Subtítulo Acciones rápidas
        var lblTitAcc = new Label { Text = "Acciones rápidas", Font = UITheme.SectionFont, ForeColor = UITheme.TextSecondary, Dock = DockStyle.Top, Height = 32, TextAlign = ContentAlignment.BottomLeft };
        container.Controls.Add(lblTitAcc);

        // 4 Botones de Acción
        var pnlActions = new TableLayoutPanel { Dock = DockStyle.Top, Height = 66, ColumnCount = 4, Margin = new Padding(0, 6, 0, 16) };
        pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        pnlActions.Controls.Add(CrearBotonCardAccion("🛒", "Nueva venta", () => AbrirVentasPos()), 0, 0);
        pnlActions.Controls.Add(CrearBotonCardAccion("🔍", "Escanear producto", () => { using var f = new VerificadorPrecioModalForm(_serviceProvider.GetRequiredService<IProductoService>(), _serviceProvider.GetRequiredService<ICategoriaService>(), _sesion); f.ShowDialog(this); }), 1, 0);
        pnlActions.Controls.Add(CrearBotonCardAccion("👤+", "Nuevo cliente", () => { using var f = new ClienteModalForm(); if (f.ShowDialog(this) == DialogResult.OK && f.ClienteGuardado != null) _serviceProvider.GetRequiredService<IClienteService>().CrearAsync(f.ClienteGuardado); }), 2, 0);
        pnlActions.Controls.Add(CrearBotonCardAccion("💳", "Cobrar reparación", () => AbrirReparaciones(soloCobrar: true)), 3, 0);
        container.Controls.Add(pnlActions);

        // Subtítulo Reparaciones listas
        var lblTitRep = new Label { Text = "Reparaciones listas para entregar y cobrar", Font = UITheme.SectionFont, ForeColor = UITheme.TextSecondary, Dock = DockStyle.Top, Height = 36, TextAlign = ContentAlignment.BottomLeft };
        container.Controls.Add(lblTitRep);

        // Contenedor lista de reparaciones
        var pnlRepList = new Panel { Dock = DockStyle.Top, AutoSize = true, BackColor = UITheme.Surface1, Padding = new Padding(0), Margin = new Padding(0, 6, 0, 20) };
        pnlRepList.Paint += (s, e) => e.Graphics.DrawRectangle(new Pen(UITheme.Border), 0, 0, pnlRepList.Width - 1, pnlRepList.Height - 1);

        if (reparacionesListas.Count == 0)
        {
            var lblEmpty = new Label { Text = "No hay reparaciones pendientes de entrega en este momento.", Font = UITheme.BodyFont, ForeColor = UITheme.TextMuted, Dock = DockStyle.Top, Height = 48, TextAlign = ContentAlignment.MiddleCenter };
            pnlRepList.Controls.Add(lblEmpty);
        }
        else
        {
            foreach (var rep in reparacionesListas)
            {
                var row = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(14, 8, 14, 8), BackColor = UITheme.Surface1 };
                var lblRepTit = new Label { Text = $"{rep.Marca} {rep.Modelo} — {rep.DescripcionProblema}", Font = UITheme.BodyBoldFont, ForeColor = UITheme.TextPrimary, Location = new Point(14, 8), AutoSize = true };
                var lblRepSub = new Label { Text = $"Cliente: {rep.Cliente?.NombreCompleto ?? "Cliente"} · {rep.Cliente?.Telefono ?? "Sin tel"} · RD${rep.PrecioFinal:N0}", Font = UITheme.SmallFont, ForeColor = UITheme.TextSecondary, Location = new Point(14, 28), AutoSize = true };
                row.Controls.Add(lblRepTit);
                row.Controls.Add(lblRepSub);

                var btnCobrar = new Button { Text = "Cobrar y entregar", Size = new Size(130, 30), Location = new Point(row.Width - 145, 12), Anchor = AnchorStyles.Top | AnchorStyles.Right };
                UITheme.AplicarBotonPrimario(btnCobrar);
                btnCobrar.Click += (s, e) =>
                {
                    using var f = new CobroReparacionModalForm(rep, _serviceProvider.GetRequiredService<ITurnoService>(), _serviceProvider.GetRequiredService<IPagoService>(), _serviceProvider.GetRequiredService<IReparacionService>(), _sesion);
                    if (f.ShowDialog(this) == DialogResult.OK) CargarPantallaInicio();
                };
                row.Controls.Add(btnCobrar);

                var sepRow = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = UITheme.Border };
                row.Controls.Add(sepRow);
                pnlRepList.Controls.Add(row);
            }
        }

        container.Controls.Add(pnlRepList);
    }

    // 2. Dashboard Técnico
    private async Task RenderDashboardTecnicoAsync(Panel container)
    {
        var reparacionService = _serviceProvider.GetRequiredService<IReparacionService>();
        var prodService = _serviceProvider.GetRequiredService<IProductoService>();

        var enReparacion = await reparacionService.ObtenerTodasAsync(EstadoReparacion.EnReparacion);
        var listas = await reparacionService.ObtenerTodasAsync(EstadoReparacion.ListaParaEntrega);
        var celulares = await prodService.ObtenerProductosAsync(soloActivos: true);
        int totalCelularesStock = celulares.Where(p => p.RequiereSerie).Sum(p => p.StockActual);

        // Header
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.Transparent };
        var lblTit = new Label { Text = "Panel del técnico", Font = new Font("Segoe UI", 12.5F, FontStyle.Bold), ForeColor = UITheme.TextPrimary, Location = new Point(0, 4), AutoSize = true };
        pnlHeader.Controls.Add(lblTit);
        container.Controls.Add(pnlHeader);

        // 3 Tarjetas métricas
        var pnlMetrics = CrearGridTarjetas(3, 85, new (string title, string val, Color bg, Color textCol)[]
        {
            ("En reparación", $"{enReparacion.Count}", UITheme.BgWarning, UITheme.Warning),
            ("Listas para entrega", $"{listas.Count}", UITheme.BgSuccess, UITheme.Success),
            ("Celulares en stock", $"{totalCelularesStock} unidades", UITheme.Surface1, UITheme.TextPrimary)
        });
        pnlMetrics.Dock = DockStyle.Top;
        container.Controls.Add(pnlMetrics);

        // Subtítulo Acciones rápidas
        var lblTitAcc = new Label { Text = "Acciones rápidas", Font = UITheme.SectionFont, ForeColor = UITheme.TextSecondary, Dock = DockStyle.Top, Height = 32, TextAlign = ContentAlignment.BottomLeft };
        container.Controls.Add(lblTitAcc);

        // 3 Botones de Acción
        var pnlActions = new TableLayoutPanel { Dock = DockStyle.Top, Height = 66, ColumnCount = 3, Margin = new Padding(0, 6, 0, 16) };
        pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));

        pnlActions.Controls.Add(CrearBotonCardAccion("🔧", "Nueva reparación", () =>
        {
            using var f = new OrdenReparacionModalForm(_serviceProvider.GetRequiredService<IClienteService>(), reparacionService, _sesion);
            if (f.ShowDialog(this) == DialogResult.OK) CargarPantallaInicio();
        }), 0, 0);
        pnlActions.Controls.Add(CrearBotonCardAccion("🔍", "Buscar cliente", () => AbrirClientes()), 1, 0);
        pnlActions.Controls.Add(CrearBotonCardAccion("📦", "Consultar producto", () => AbrirProductos(soloLectura: true)), 2, 0);
        container.Controls.Add(pnlActions);

        // Subtítulo Mis reparaciones activas
        var lblTitRep = new Label { Text = "Mis reparaciones activas", Font = UITheme.SectionFont, ForeColor = UITheme.TextSecondary, Dock = DockStyle.Top, Height = 36, TextAlign = ContentAlignment.BottomLeft };
        container.Controls.Add(lblTitRep);

        // Contenedor lista
        var pnlRepList = new Panel { Dock = DockStyle.Top, AutoSize = true, BackColor = UITheme.Surface1, Padding = new Padding(0), Margin = new Padding(0, 6, 0, 20) };
        pnlRepList.Paint += (s, e) => e.Graphics.DrawRectangle(new Pen(UITheme.Border), 0, 0, pnlRepList.Width - 1, pnlRepList.Height - 1);

        if (enReparacion.Count == 0)
        {
            var lblEmpty = new Label { Text = "No hay equipos en reparación pendientes.", Font = UITheme.BodyFont, ForeColor = UITheme.TextMuted, Dock = DockStyle.Top, Height = 48, TextAlign = ContentAlignment.MiddleCenter };
            pnlRepList.Controls.Add(lblEmpty);
        }
        else
        {
            foreach (var rep in enReparacion)
            {
                var row = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(14, 8, 14, 8), BackColor = UITheme.Surface1 };
                var lblRepTit = new Label { Text = $"{rep.Marca} {rep.Modelo} — {rep.DescripcionProblema}", Font = UITheme.BodyBoldFont, ForeColor = UITheme.TextPrimary, Location = new Point(14, 8), AutoSize = true };
                var lblRepSub = new Label { Text = $"Cliente: {rep.Cliente?.NombreCompleto ?? "Cliente"} · {rep.Cliente?.Telefono ?? "Sin tel"}", Font = UITheme.SmallFont, ForeColor = UITheme.TextSecondary, Location = new Point(14, 28), AutoSize = true };
                row.Controls.Add(lblRepTit);
                row.Controls.Add(lblRepSub);

                var btnMarcarLista = new Button { Text = "Marcar lista", Size = new Size(110, 30), Location = new Point(row.Width - 125, 12), Anchor = AnchorStyles.Top | AnchorStyles.Right };
                UITheme.AplicarBotonSecundario(btnMarcarLista);
                btnMarcarLista.Click += async (s, e) =>
                {
                    await reparacionService.CambiarEstadoAsync(rep.Id, EstadoReparacion.ListaParaEntrega);
                    CargarPantallaInicio();
                };
                row.Controls.Add(btnMarcarLista);

                var sepRow = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = UITheme.Border };
                row.Controls.Add(sepRow);
                pnlRepList.Controls.Add(row);
            }
        }

        container.Controls.Add(pnlRepList);
    }

    // 3. Dashboard Admin (Dueño)
    private async Task RenderDashboardAdminAsync(Panel container)
    {
        var ventaService = _serviceProvider.GetRequiredService<IVentaService>();
        var finanzasService = _serviceProvider.GetRequiredService<IFinanzasService>();
        var reparacionService = _serviceProvider.GetRequiredService<IReparacionService>();
        var usuarioService = _serviceProvider.GetRequiredService<IUsuarioService>();

        var fechaInicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var ventasMes = await ventaService.ObtenerHistorialAsync(desde: fechaInicioMes);
        var movimientosMes = await finanzasService.ObtenerMovimientosAsync(desde: fechaInicioMes);
        var reparacionesActivas = await reparacionService.ObtenerTodasAsync(EstadoReparacion.EnReparacion);
        var empleados = await usuarioService.ObtenerTodosAsync(incluirInactivos: false);

        decimal totalVentasMes = ventasMes.Where(v => v.Estado != EstadoVenta.Anulada).Sum(v => v.Total);
        decimal totalGastosMes = movimientosMes.Where(m => m.Tipo == TipoMovimientoFinanciero.Gasto).Sum(m => m.Monto);

        // Header
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = Color.Transparent };
        var lblTit = new Label { Text = "Panel del negocio", Font = new Font("Segoe UI", 12.5F, FontStyle.Bold), ForeColor = UITheme.TextPrimary, Location = new Point(0, 4), AutoSize = true };
        pnlHeader.Controls.Add(lblTit);

        var pnlBadge = new Panel { Size = new Size(195, 26), Location = new Point(container.Width - 230, 6), BackColor = UITheme.BgSuccess, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        pnlBadge.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var f = new SolidBrush(UITheme.Success);
            using var font = new Font("Segoe UI", 8F, FontStyle.Bold);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString("💼 Gestión completa del negocio", font, f, new RectangleF(0, 0, 195, 26), sf);
        };
        pnlHeader.Controls.Add(pnlBadge);
        container.Controls.Add(pnlHeader);

        // 4 Tarjetas métricas
        var pnlMetrics = CrearGridTarjetas(4, 85, new (string title, string val, Color bg, Color textCol)[]
        {
            ("Ventas del mes", $"RD${totalVentasMes:N0}", UITheme.Surface1, UITheme.TextPrimary),
            ("Gastos del mes", $"RD${totalGastosMes:N0}", UITheme.Surface1, UITheme.TextPrimary),
            ("Reparaciones activas", $"{reparacionesActivas.Count}", UITheme.Surface1, UITheme.TextPrimary),
            ("Empleados activos", $"{empleados.Count}", UITheme.Surface1, UITheme.TextPrimary)
        });
        pnlMetrics.Dock = DockStyle.Top;
        container.Controls.Add(pnlMetrics);

        // Gráficos del Dashboard (Ventas últimos 7 días y Estado de Operaciones)
        var haceSieteDias = DateTime.UtcNow.Date.AddDays(-6);
        var ventasSemana = await ventaService.ObtenerHistorialAsync(desde: haceSieteDias);
        var datosGraficoVentas = new List<(string dia, decimal monto)>();
        for (int i = 6; i >= 0; i--)
        {
            var fecha = DateTime.UtcNow.Date.AddDays(-i);
            var diaNombre = fecha.ToString("ddd d", new System.Globalization.CultureInfo("es-DO"));
            decimal totalDia = ventasSemana
                .Where(v => v.FechaVenta.Date == fecha && v.Estado != EstadoVenta.Anulada)
                .Sum(v => v.Total);
            datosGraficoVentas.Add((diaNombre, totalDia));
        }

        var prodService = _serviceProvider.GetRequiredService<IProductoService>();
        var prods = await prodService.ObtenerProductosAsync(soloActivos: true);
        var stockBajo = await prodService.ObtenerProductosBajoStockAsync();
        var reparacionesListas = await reparacionService.ObtenerTodasAsync(EstadoReparacion.ListaParaEntrega);

        var datosDistribucion = new List<(string label, int cantidad, Color color)>
        {
            ("Celulares en stock", prods.Where(p => p.RequiereSerie).Sum(p => p.StockActual), UITheme.Primary),
            ("En taller", reparacionesActivas.Count, UITheme.Warning),
            ("Listas para entrega", reparacionesListas.Count, UITheme.Success),
            ("Stock bajo", stockBajo.Count, UITheme.Danger)
        };

        var pnlChartsGrid = new TableLayoutPanel { Dock = DockStyle.Top, Height = 195, ColumnCount = 2, Margin = new Padding(0, 4, 0, 14) };
        pnlChartsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
        pnlChartsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
        pnlChartsGrid.Controls.Add(CrearPanelGraficoVentas("Ventas de los últimos 7 días", datosGraficoVentas, UITheme.Primary), 0, 0);
        pnlChartsGrid.Controls.Add(CrearPanelGraficoDistribucion("Distribución operativa y stock", datosDistribucion), 1, 0);
        container.Controls.Add(pnlChartsGrid);

        // Banner informativo multi-caja
        var bannerLock = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = UITheme.Surface1, Margin = new Padding(0, 4, 0, 14), Padding = new Padding(14, 10, 14, 10) };
        bannerLock.Paint += (s, e) => e.Graphics.DrawRectangle(new Pen(UITheme.Border), 0, 0, bannerLock.Width - 1, bannerLock.Height - 1);
        var lblLock = new Label { Text = "🔒  El modo multi-caja solo puede activarlo el Super Admin desde soporte técnico.", Font = UITheme.SmallFont, ForeColor = UITheme.TextMuted, Location = new Point(14, 11), AutoSize = true };
        bannerLock.Controls.Add(lblLock);
        container.Controls.Add(bannerLock);

        // Subtítulo Acciones del negocio
        var lblTitAcc = new Label { Text = "Acciones del negocio", Font = UITheme.SectionFont, ForeColor = UITheme.TextSecondary, Dock = DockStyle.Top, Height = 32, TextAlign = ContentAlignment.BottomLeft };
        container.Controls.Add(lblTitAcc);

        // 4 Botones de Acción
        var pnlActions = new TableLayoutPanel { Dock = DockStyle.Top, Height = 66, ColumnCount = 4, Margin = new Padding(0, 6, 0, 16) };
        pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        pnlActions.Controls.Add(CrearBotonCardAccion("📥", "Entrada inventario", () => AbrirRegistrarCompra()), 0, 0);
        pnlActions.Controls.Add(CrearBotonCardAccion("👤+", "Crear empleado", () => AbrirUsuarios()), 1, 0);
        pnlActions.Controls.Add(CrearBotonCardAccion("🔑", "Resetear contraseña", () => AbrirUsuarios()), 2, 0);
        pnlActions.Controls.Add(CrearBotonCardAccion("📊", "Reporte de turnos", () => AbrirHistorialTurnos()), 3, 0);
        container.Controls.Add(pnlActions);
    }

    // 4. Dashboard Super Admin
    private async Task RenderDashboardSuperAdminAsync(Panel container)
    {
        var ventaService = _serviceProvider.GetRequiredService<IVentaService>();
        var prodService = _serviceProvider.GetRequiredService<IProductoService>();
        var usuarioService = _serviceProvider.GetRequiredService<IUsuarioService>();
        var reparacionService = _serviceProvider.GetRequiredService<IReparacionService>();
        var configRedService = _serviceProvider.GetRequiredService<IConfiguracionRedService>();

        var ventasHoy = await ventaService.ObtenerHistorialAsync(desde: DateTime.UtcNow.Date);
        var productos = await prodService.ObtenerProductosAsync(soloActivos: true);
        var stockBajo = await prodService.ObtenerProductosBajoStockAsync();
        var empleados = await usuarioService.ObtenerTodosAsync(incluirInactivos: false);
        var configRed = configRedService.ObtenerConfiguracion();

        decimal totalVentasHoy = ventasHoy.Where(v => v.Estado != EstadoVenta.Anulada).Sum(v => v.Total);
        int totalCelularesStock = productos.Where(p => p.RequiereSerie).Sum(p => p.StockActual);

        // Header
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = Color.Transparent };
        var lblTit = new Label { Text = "Panel de soporte y control total", Font = new Font("Segoe UI", 12.5F, FontStyle.Bold), ForeColor = UITheme.TextPrimary, Location = new Point(0, 4), AutoSize = true };
        pnlHeader.Controls.Add(lblTit);

        var pnlBadge = new Panel { Size = new Size(110, 26), Location = new Point(container.Width - 145, 6), BackColor = UITheme.BgPro, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        pnlBadge.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var f = new SolidBrush(UITheme.Pro);
            using var font = new Font("Segoe UI", 8F, FontStyle.Bold);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString("🛡️ Acceso total", font, f, new RectangleF(0, 0, 110, 26), sf);
        };
        pnlHeader.Controls.Add(pnlBadge);
        container.Controls.Add(pnlHeader);

        // 4 Tarjetas métricas
        var pnlMetrics = CrearGridTarjetas(4, 85, new (string title, string val, Color bg, Color textCol)[]
        {
            ("Ventas de hoy", $"RD${totalVentasHoy:N0}", UITheme.Surface1, UITheme.TextPrimary),
            ("Celulares en stock", $"{totalCelularesStock} unidades", UITheme.Surface1, UITheme.TextPrimary),
            ("Stock bajo", $"{stockBajo.Count} productos", UITheme.BgWarning, UITheme.Warning),
            ("Empleados activos", $"{empleados.Count}", UITheme.Surface1, UITheme.TextPrimary)
        });
        pnlMetrics.Dock = DockStyle.Top;
        container.Controls.Add(pnlMetrics);

        // Gráficos del Dashboard Super Admin (Ventas 7 Días & Inventario / Taller)
        var haceSieteDias = DateTime.UtcNow.Date.AddDays(-6);
        var ventasSemana = await ventaService.ObtenerHistorialAsync(desde: haceSieteDias);
        var datosGraficoVentas = new List<(string dia, decimal monto)>();
        for (int i = 6; i >= 0; i--)
        {
            var fecha = DateTime.UtcNow.Date.AddDays(-i);
            var diaNombre = fecha.ToString("ddd d", new System.Globalization.CultureInfo("es-DO"));
            decimal totalDia = ventasSemana
                .Where(v => v.FechaVenta.Date == fecha && v.Estado != EstadoVenta.Anulada)
                .Sum(v => v.Total);
            datosGraficoVentas.Add((diaNombre, totalDia));
        }

        var enReparacion = await reparacionService.ObtenerTodasAsync(EstadoReparacion.EnReparacion);
        var reparacionesListas = await reparacionService.ObtenerTodasAsync(EstadoReparacion.ListaParaEntrega);

        var datosDistribucion = new List<(string label, int cantidad, Color color)>
        {
            ("Celulares en stock", totalCelularesStock, UITheme.Pro),
            ("En reparación", enReparacion.Count, UITheme.Warning),
            ("Listas para entrega", reparacionesListas.Count, UITheme.Success),
            ("Alertas stock bajo", stockBajo.Count, UITheme.Danger)
        };

        var pnlChartsGrid = new TableLayoutPanel { Dock = DockStyle.Top, Height = 195, ColumnCount = 2, Margin = new Padding(0, 4, 0, 14) };
        pnlChartsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
        pnlChartsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
        pnlChartsGrid.Controls.Add(CrearPanelGraficoVentas("Tendencia de ventas (últimos 7 días)", datosGraficoVentas, UITheme.Pro), 0, 0);
        pnlChartsGrid.Controls.Add(CrearPanelGraficoDistribucion("Salud de inventario y taller", datosDistribucion), 1, 0);
        container.Controls.Add(pnlChartsGrid);

        // Switch Card Multi-caja
        var pnlMultiCaja = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = UITheme.Surface1, Margin = new Padding(0, 4, 0, 14), Padding = new Padding(14, 10, 14, 10) };
        pnlMultiCaja.Paint += (s, e) => e.Graphics.DrawRectangle(new Pen(UITheme.Border), 0, 0, pnlMultiCaja.Width - 1, pnlMultiCaja.Height - 1);
        
        var lblMcTit = new Label { Text = "Modo multi-caja", Font = UITheme.BodyBoldFont, ForeColor = UITheme.TextPrimary, Location = new Point(14, 8), AutoSize = true };
        var lblMcSub = new Label { Text = $"Exclusivo del Super Admin. Actualmente: {(configRed.ModoOperacion == ModoOperacionCaja.CajaUnicaLocal ? "1 caja (Caja Única Local)" : "Multi-Caja Activo")}.", Font = UITheme.SmallFont, ForeColor = UITheme.TextSecondary, Location = new Point(14, 28), AutoSize = true };
        pnlMultiCaja.Controls.Add(lblMcTit);
        pnlMultiCaja.Controls.Add(lblMcSub);

        var btnToggleMc = new Button { Text = configRed.ModoOperacion == ModoOperacionCaja.CajaUnicaLocal ? "⚙️ Configurar red" : "🌐 Servidor LAN", Size = new Size(135, 30), Location = new Point(pnlMultiCaja.Width - 155, 12), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        UITheme.AplicarBotonSecundario(btnToggleMc);
        btnToggleMc.Click += (s, e) => AbrirConfiguracionSuperAdmin();
        pnlMultiCaja.Controls.Add(btnToggleMc);
        container.Controls.Add(pnlMultiCaja);

        // Subtítulo Acciones de soporte
        var lblTitAcc = new Label { Text = "Acciones de soporte", Font = UITheme.SectionFont, ForeColor = UITheme.TextSecondary, Dock = DockStyle.Top, Height = 32, TextAlign = ContentAlignment.BottomLeft };
        container.Controls.Add(lblTitAcc);

        // 4 Botones de Acción
        var pnlActions = new TableLayoutPanel { Dock = DockStyle.Top, Height = 66, ColumnCount = 4, Margin = new Padding(0, 6, 0, 16) };
        pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        pnlActions.Controls.Add(CrearBotonCardAccion("📥", "Entrada inventario", () => AbrirRegistrarCompra()), 0, 0);
        pnlActions.Controls.Add(CrearBotonCardAccion("🔑", "Resetear contraseña", () => AbrirUsuarios()), 1, 0);
        pnlActions.Controls.Add(CrearBotonCardAccion("👤+", "Crear cuenta Admin", () => AbrirUsuarios()), 2, 0);
        pnlActions.Controls.Add(CrearBotonCardAccion("📊", "Reporte de turnos", () => AbrirHistorialTurnos()), 3, 0);
        container.Controls.Add(pnlActions);
    }

    // Helpers UI para Gráficos
    private Panel CrearPanelGraficoVentas(string titulo, List<(string dia, decimal monto)> datos, Color colorPrimario)
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UITheme.Surface1,
            Padding = new Padding(14, 12, 14, 12),
            Margin = new Padding(0, 0, 6, 0)
        };
        pnl.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using var penBorder = new Pen(UITheme.Border);
            g.DrawRectangle(penBorder, 0, 0, pnl.Width - 1, pnl.Height - 1);

            using var fontTit = new Font("Segoe UI", 9F, FontStyle.Bold);
            using var brushTit = new SolidBrush(UITheme.TextPrimary);
            g.DrawString($"📊  {titulo}", fontTit, brushTit, 14, 10);

            float topY = 36f;
            float bottomY = pnl.Height - 30f;
            float leftX = 16f;
            float rightX = pnl.Width - 16f;
            float chartWidth = rightX - leftX;
            float chartHeight = bottomY - topY;

            if (chartHeight <= 20 || chartWidth <= 40 || datos.Count == 0) return;

            decimal maxMonto = datos.Max(d => d.monto);
            if (maxMonto <= 0) maxMonto = 1000m;
            decimal techo = Math.Max(1000m, Math.Ceiling(maxMonto / 500m) * 500m);

            using var penGrid = new Pen(Color.FromArgb(238, 240, 243)) { DashStyle = DashStyle.Dash };
            g.DrawLine(penGrid, leftX, topY, rightX, topY);
            g.DrawLine(penGrid, leftX, topY + chartHeight / 2, rightX, topY + chartHeight / 2);
            g.DrawLine(penGrid, leftX, bottomY, rightX, bottomY);

            float barWidth = Math.Min(36f, (chartWidth / datos.Count) * 0.55f);
            float stepX = chartWidth / datos.Count;

            using var fontVal = new Font("Segoe UI", 7.5F, FontStyle.Bold);
            using var fontDia = new Font("Segoe UI", 7.5F);
            using var brushTxt = new SolidBrush(UITheme.TextSecondary);
            using var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var sfTop = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Far };

            for (int i = 0; i < datos.Count; i++)
            {
                var (dia, monto) = datos[i];
                float cx = leftX + (i * stepX) + (stepX / 2);
                float barH = (float)((monto / techo) * (decimal)chartHeight);
                if (barH < 4 && monto > 0) barH = 4;
                float barY = bottomY - barH;
                float barX = cx - (barWidth / 2);

                if (monto > 0)
                {
                    var rectBar = new RectangleF(barX, barY, barWidth, barH);
                    using var brushBar = new LinearGradientBrush(new PointF(barX, barY), new PointF(barX, bottomY), colorPrimario, Color.FromArgb(160, colorPrimario));
                    g.FillRectangle(brushBar, rectBar);

                    string valStr = monto >= 1000 ? $"${monto / 1000:N1}k" : $"${monto:N0}";
                    g.DrawString(valStr, fontVal, brushTit, new PointF(cx, barY - 2), sfTop);
                }
                else
                {
                    using var brushEmpty = new SolidBrush(Color.FromArgb(235, 238, 242));
                    g.FillRectangle(brushEmpty, barX, bottomY - 3, barWidth, 3);
                }

                g.DrawString(dia, fontDia, brushTxt, new RectangleF(cx - (stepX / 2), bottomY + 2, stepX, 20), sfCenter);
            }
        };
        return pnl;
    }

    private Panel CrearPanelGraficoDistribucion(string titulo, List<(string label, int cantidad, Color color)> items)
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UITheme.Surface1,
            Padding = new Padding(14, 12, 14, 12),
            Margin = new Padding(6, 0, 0, 0)
        };
        pnl.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using var penBorder = new Pen(UITheme.Border);
            g.DrawRectangle(penBorder, 0, 0, pnl.Width - 1, pnl.Height - 1);

            using var fontTit = new Font("Segoe UI", 9F, FontStyle.Bold);
            using var brushTit = new SolidBrush(UITheme.TextPrimary);
            g.DrawString($"📈  {titulo}", fontTit, brushTit, 14, 10);

            float startY = 38f;
            int maxVal = items.Count > 0 ? items.Max(it => it.cantidad) : 1;
            if (maxVal <= 0) maxVal = 1;

            using var fontLbl = new Font("Segoe UI", 8F);
            using var fontCount = new Font("Segoe UI", 8F, FontStyle.Bold);
            using var brushLbl = new SolidBrush(UITheme.TextSecondary);

            float rowHeight = Math.Min(32f, (pnl.Height - startY - 8) / Math.Max(1, items.Count));

            for (int i = 0; i < items.Count; i++)
            {
                var (label, cant, color) = items[i];
                float y = startY + (i * rowHeight);

                using var brushDot = new SolidBrush(color);
                g.FillEllipse(brushDot, 14, y + 5, 7, 7);
                g.DrawString(label, fontLbl, brushLbl, 26, y + 1);

                float barLeft = 140;
                float barRight = pnl.Width - 45;
                float barWidth = Math.Max(20, barRight - barLeft);
                float fillWidth = (float)((double)cant / maxVal) * barWidth;

                using var brushBgBar = new SolidBrush(Color.FromArgb(240, 242, 245));
                g.FillRectangle(brushBgBar, barLeft, y + 4, barWidth, 8);

                if (cant > 0)
                {
                    g.FillRectangle(brushDot, barLeft, y + 4, Math.Max(4, fillWidth), 8);
                }

                using var brushVal = new SolidBrush(color);
                g.DrawString(cant.ToString(), fontCount, brushVal, pnl.Width - 38, y);
            }
        };
        return pnl;
    }

    // Helpers UI para Cards y Grids
    private TableLayoutPanel CrearGridTarjetas(int columnas, int altura, (string title, string val, Color bg, Color textCol)[] cards)
    {
        var grid = new TableLayoutPanel { Height = altura, ColumnCount = columnas, RowCount = 1, Margin = new Padding(0, 4, 0, 12) };
        float pct = 100f / columnas;
        for (int i = 0; i < columnas; i++)
        {
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, pct));
            var (title, val, bg, textCol) = cards[i];

            var card = new Panel { Dock = DockStyle.Fill, BackColor = bg, Padding = new Padding(14, 12, 14, 12), Margin = new Padding(i == 0 ? 0 : 6, 0, i == columnas - 1 ? 0 : 6, 0) };
            card.Paint += (s, e) => e.Graphics.DrawRectangle(new Pen(UITheme.Border), 0, 0, card.Width - 1, card.Height - 1);

            var lblT = new Label { Text = title, Font = UITheme.SmallFont, ForeColor = textCol == UITheme.TextPrimary ? UITheme.TextSecondary : textCol, Dock = DockStyle.Top, Height = 18 };
            var lblV = new Label { Text = val, Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = textCol, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };

            card.Controls.Add(lblV);
            card.Controls.Add(lblT);
            grid.Controls.Add(card, i, 0);
        }

        return grid;
    }

    private Button CrearBotonCardAccion(string icono, string texto, Action accion)
    {
        var btn = new Button
        {
            Text = $"{icono}\n{texto}",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = UITheme.Surface1,
            ForeColor = UITheme.TextPrimary,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            Margin = new Padding(4)
        };
        btn.FlatAppearance.BorderColor = UITheme.Border;
        btn.FlatAppearance.BorderSize = 1;
        btn.MouseEnter += (s, e) => { btn.BackColor = UITheme.Surface2; };
        btn.MouseLeave += (s, e) => { btn.BackColor = UITheme.Surface1; };
        btn.Click += (s, e) => accion();
        return btn;
    }
}
