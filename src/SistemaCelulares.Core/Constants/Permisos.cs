namespace SistemaCelulares.Core.Constants;

/// <summary>
/// Catálogo cerrado y canónico de permisos disponibles en el sistema.
/// </summary>
public static class Permisos
{
    // Módulo: Seguridad y Usuarios
    public const string UsuariosVer = "Usuarios.Ver";
    public const string UsuariosCrear = "Usuarios.Crear";
    public const string UsuariosEditar = "Usuarios.Editar";
    public const string UsuariosDesactivar = "Usuarios.Desactivar";
    public const string UsuariosResetPassword = "Usuarios.ResetPassword";

    // Módulo: Roles y Permisos
    public const string RolesVer = "Roles.Ver";
    public const string RolesCrear = "Roles.Crear";
    public const string RolesEditar = "Roles.Editar";
    public const string RolesEliminar = "Roles.Eliminar";

    // Módulo: Turnos / Caja
    public const string TurnosAbrir = "Turnos.Abrir";
    public const string TurnosCerrar = "Turnos.Cerrar";
    public const string TurnosHistorial = "Turnos.Historial";

    // Módulo: Productos e Inventario
    public const string ProductosVer = "Productos.Ver";
    public const string ProductosCrear = "Productos.Crear";
    public const string ProductosEditar = "Productos.Editar";
    public const string ProductosDesactivar = "Productos.Desactivar";
    public const string CategoriasGestionar = "Categorias.Gestionar";
    public const string AlertasStockVer = "AlertasStock.Ver";

    // Módulo: Proveedores y Compras
    public const string ProveedoresGestionar = "Proveedores.Gestionar";
    public const string ComprasRegistrar = "Compras.Registrar";
    public const string ComprasHistorial = "Compras.Historial";

    // Módulo: Ventas y Facturación
    public const string VentasRegistrar = "Ventas.Registrar";
    public const string VentasAnular = "Ventas.Anular";
    public const string VentasHistorial = "Ventas.Historial";

    // Módulo: Finanzas
    public const string FinanzasMovimientosRegistrar = "Finanzas.MovimientosRegistrar";
    public const string FinanzasReportesVer = "Finanzas.ReportesVer";

    // Módulo: Configuración del Sistema
    public const string SistemaConfigurar = "Sistema.Configurar";

    /// <summary>
    /// Lista con la definición descriptiva de todos los permisos.
    /// </summary>
    public static readonly IReadOnlyList<PermisoDefinicion> Todos = new List<PermisoDefinicion>
    {
        // Seguridad
        new(UsuariosVer, "Usuarios", "Ver lista y detalles de usuarios y empleados"),
        new(UsuariosCrear, "Usuarios", "Crear nuevos usuarios y asignar credenciales temporales"),
        new(UsuariosEditar, "Usuarios", "Editar información de usuarios existentes"),
        new(UsuariosDesactivar, "Usuarios", "Activar o desactivar usuarios"),
        new(UsuariosResetPassword, "Usuarios", "Resetear contraseñas de usuarios a una clave temporal"),

        // Roles
        new(RolesVer, "Roles", "Ver roles y sus permisos asignados"),
        new(RolesCrear, "Roles", "Crear roles personalizados"),
        new(RolesEditar, "Roles", "Modificar permisos de roles personalizados"),
        new(RolesEliminar, "Roles", "Eliminar roles personalizados"),

        // Turnos
        new(TurnosAbrir, "Caja", "Abrir turno de caja con monto inicial"),
        new(TurnosCerrar, "Caja", "Cerrar turno de caja y registrar arqueo"),
        new(TurnosHistorial, "Caja", "Consultar historial general de turnos"),

        // Productos
        new(ProductosVer, "Inventario", "Ver catálogo de productos, precios y stock"),
        new(ProductosCrear, "Inventario", "Registrar nuevos productos"),
        new(ProductosEditar, "Inventario", "Editar productos y ajustar precios"),
        new(ProductosDesactivar, "Inventario", "Desactivar productos del catálogo"),
        new(CategoriasGestionar, "Inventario", "Crear y gestionar categorías"),
        new(AlertasStockVer, "Inventario", "Visualizar alertas de stock mínimo"),

        // Proveedores y Compras
        new(ProveedoresGestionar, "Proveedores", "Gestionar catálogo de proveedores"),
        new(ComprasRegistrar, "Compras", "Registrar compras e ingresar inventario"),
        new(ComprasHistorial, "Compras", "Consultar historial de compras"),

        // Ventas
        new(VentasRegistrar, "Ventas", "Registrar y cobrar ventas"),
        new(VentasAnular, "Ventas", "Anular ventas registradas"),
        new(VentasHistorial, "Ventas", "Consultar historial de ventas"),

        // Finanzas
        new(FinanzasMovimientosRegistrar, "Finanzas", "Registrar gastos e ingresos del negocio"),
        new(FinanzasReportesVer, "Finanzas", "Consultar reportes financieros y balance neto"),

        // Sistema
        new(SistemaConfigurar, "Sistema", "Configuración general y modo multi-caja")
    };
}

public record PermisoDefinicion(string Codigo, string Modulo, string Descripcion);
