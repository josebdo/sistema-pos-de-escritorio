using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaCelulares.App.Common;
using SistemaCelulares.App.Forms;
using SistemaCelulares.Core.Constants;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Infrastructure.Data;
using SistemaCelulares.Infrastructure.Security;
using SistemaCelulares.Infrastructure.Services;

namespace SistemaCelulares.App;

internal static class Program
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    [STAThread]
    static void Main()
    {
        // 1. Configuración de Windows Forms
        ApplicationConfiguration.Initialize();

        // 2. Configuración de Localización para República Dominicana (Moneda RD$ / DOP, es-DO)
        AppCulture.ConfigurarCulturaDominicana();

        // 3. Configuración de Inyección de Dependencias
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        // 4. Inicialización y Sembrado de la Base de Datos SQLite
        using (var scope = ServiceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            DbInitializer.InitializeAsync(dbContext, hasher).GetAwaiter().GetResult();
        }

        // 5. Flujo de Autenticación Principal (Ciclo controlado de Login / Sesión)
        var authService = ServiceProvider.GetRequiredService<IAuthService>();
        while (true)
        {
            using var loginForm = new LoginForm(authService);
            var dialogResult = loginForm.ShowDialog();

            if (dialogResult != DialogResult.OK || loginForm.SesionIniciada == null)
            {
                // Usuario cerró el login con la 'X' o Cancelar -> Salir limpiamente
                break;
            }

            var mainForm = new MainForm(ServiceProvider, loginForm.SesionIniciada);
            Application.Run(mainForm);

            // Si se cerró la ventana principal sin solicitar cerrar sesión -> Salir
            if (!mainForm.CierreSesionSolicitado)
            {
                break;
            }
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Base de Datos SQLite local en ruta segura y aislada (ProgramData)
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={AppPaths.RutaBaseDatos}"));

        // Servicios del Dominio y Seguridad
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IRolService, RolService>();
        services.AddScoped<ITurnoService, TurnoService>();
        services.AddScoped<ICategoriaService, CategoriaService>();
        services.AddScoped<IProductoService, ProductoService>();
        services.AddScoped<IProveedorService, ProveedorService>();
        services.AddScoped<ICompraService, CompraService>();
        services.AddScoped<IAlertaStockService, AlertaStockService>();
        services.AddScoped<IFinanzasService, FinanzasService>();
        services.AddScoped<IEan13GeneratorService, Ean13GeneratorService>();
        services.AddScoped<IPagoService, PagoService>();
        services.AddSingleton<IConfiguracionRedService, ConfiguracionRedService>();
        services.AddScoped<IDataMigrationService, DataMigrationService>();
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<IReparacionService, ReparacionService>();
        services.AddScoped<IVentaService, VentaService>();
        services.AddScoped<IConfiguracionNegocioService, ConfiguracionNegocioService>();
        services.AddScoped<IBackupService, BackupService>();

        // Formularios
        services.AddTransient<LoginForm>();
        services.AddTransient<UsuariosForm>();
        services.AddTransient<RolesPermisosForm>();
        services.AddTransient<HistorialTurnosForm>();
        services.AddTransient<ClientesForm>();
        services.AddTransient<ProductosForm>();
        services.AddTransient<InventarioForm>();
        services.AddTransient<ProveedoresForm>();
        services.AddTransient<HistorialComprasForm>();
        services.AddTransient<AlertasStockForm>();
        services.AddTransient<ReparacionesForm>();
        services.AddTransient<FinanzasForm>();
        services.AddTransient<ConfiguracionRedMultiCajaForm>();
        services.AddTransient<ConfiguracionSistemaForm>();
        services.AddTransient<PuntoVentaForm>();
        services.AddTransient<HistorialVentasForm>();
    }
}