using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Constants;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;

namespace SistemaCelulares.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context, IPasswordHasher hasher)
    {
        // Asegurar que la base de datos y esquema existen
        await context.Database.EnsureCreatedAsync();

        // 1. Sembrar Permisos canónicos
        var permisosExistentes = await context.Permisos.ToDictionaryAsync(p => p.Codigo, StringComparer.OrdinalIgnoreCase);
        foreach (var def in Permisos.Todos)
        {
            if (!permisosExistentes.ContainsKey(def.Codigo))
            {
                var nuevoPermiso = new Permiso
                {
                    Codigo = def.Codigo,
                    Modulo = def.Modulo,
                    Descripcion = def.Descripcion
                };
                context.Permisos.Add(nuevoPermiso);
            }
        }
        await context.SaveChangesAsync();

        var todosLosPermisos = await context.Permisos.ToListAsync();
        var permisosPorCodigo = todosLosPermisos.ToDictionary(p => p.Codigo, StringComparer.OrdinalIgnoreCase);

        // 2. Sembrar Roles Fijos
        var rolSuperAdmin = await context.Roles.Include(r => r.RolPermisos)
            .FirstOrDefaultAsync(r => r.Nombre == Rol.SuperAdmin);
        if (rolSuperAdmin == null)
        {
            rolSuperAdmin = new Rol
            {
                Nombre = Rol.SuperAdmin,
                Descripcion = "Control total y soporte técnico de la instalación",
                EsFijo = true,
                Activo = true
            };
            context.Roles.Add(rolSuperAdmin);
            await context.SaveChangesAsync();
        }

        var rolAdmin = await context.Roles.Include(r => r.RolPermisos)
            .FirstOrDefaultAsync(r => r.Nombre == Rol.Admin);
        if (rolAdmin == null)
        {
            rolAdmin = new Rol
            {
                Nombre = Rol.Admin,
                Descripcion = "Administrador y dueño del negocio",
                EsFijo = true,
                Activo = true
            };
            context.Roles.Add(rolAdmin);
            await context.SaveChangesAsync();
        }

        var rolCajero = await context.Roles.Include(r => r.RolPermisos)
            .FirstOrDefaultAsync(r => r.Nombre == Rol.Cajero);
        if (rolCajero == null)
        {
            rolCajero = new Rol
            {
                Nombre = Rol.Cajero,
                Descripcion = "Operador de punto de venta y caja",
                EsFijo = true,
                Activo = true
            };
            context.Roles.Add(rolCajero);
            await context.SaveChangesAsync();
        }

        // 3. Asignar Permisos a Roles Fijos
        // Super Admin tiene todos
        foreach (var p in todosLosPermisos)
        {
            if (!rolSuperAdmin.RolPermisos.Any(rp => rp.PermisoId == p.Id))
            {
                context.RolPermisos.Add(new RolPermiso { RolId = rolSuperAdmin.Id, PermisoId = p.Id });
            }
        }

        // Admin tiene permisos de gestión operativa completa
        var codigosPermisosAdmin = new HashSet<string>
        {
            Permisos.UsuariosVer, Permisos.UsuariosCrear, Permisos.UsuariosEditar, Permisos.UsuariosDesactivar, Permisos.UsuariosResetPassword,
            Permisos.RolesVer, Permisos.RolesCrear, Permisos.RolesEditar, Permisos.RolesEliminar,
            Permisos.TurnosAbrir, Permisos.TurnosCerrar, Permisos.TurnosHistorial,
            Permisos.ProductosVer, Permisos.ProductosCrear, Permisos.ProductosEditar, Permisos.ProductosDesactivar, Permisos.CategoriasGestionar, Permisos.AlertasStockVer,
            Permisos.ProveedoresGestionar, Permisos.ComprasRegistrar, Permisos.ComprasHistorial,
            Permisos.VentasRegistrar, Permisos.VentasAnular, Permisos.VentasHistorial,
            Permisos.FinanzasMovimientosRegistrar, Permisos.FinanzasReportesVer
        };

        foreach (var codigo in codigosPermisosAdmin)
        {
            if (permisosPorCodigo.TryGetValue(codigo, out var permiso) &&
                !rolAdmin.RolPermisos.Any(rp => rp.PermisoId == permiso.Id))
            {
                context.RolPermisos.Add(new RolPermiso { RolId = rolAdmin.Id, PermisoId = permiso.Id });
            }
        }

        // Cajero tiene permisos limitados a ventas y caja
        var codigosPermisosCajero = new HashSet<string>
        {
            Permisos.TurnosAbrir, Permisos.TurnosCerrar,
            Permisos.ProductosVer,
            Permisos.VentasRegistrar, Permisos.VentasHistorial
        };

        foreach (var codigo in codigosPermisosCajero)
        {
            if (permisosPorCodigo.TryGetValue(codigo, out var permiso) &&
                !rolCajero.RolPermisos.Any(rp => rp.PermisoId == permiso.Id))
            {
                context.RolPermisos.Add(new RolPermiso { RolId = rolCajero.Id, PermisoId = permiso.Id });
            }
        }

        await context.SaveChangesAsync();

        // 4. Sembrar Usuarios Iniciales
        if (!await context.Usuarios.AnyAsync(u => u.NombreUsuario == "superadmin"))
        {
            context.Usuarios.Add(new Usuario
            {
                NombreCompleto = "Super Administrador",
                NombreUsuario = "superadmin",
                PasswordHash = hasher.HashPassword("SuperAdmin123!"),
                RolId = rolSuperAdmin.Id,
                Activo = true,
                DebeCambiarPassword = true,
                FechaCreacion = DateTime.UtcNow
            });
        }

        if (!await context.Usuarios.AnyAsync(u => u.NombreUsuario == "admin"))
        {
            context.Usuarios.Add(new Usuario
            {
                NombreCompleto = "Administrador Principal",
                NombreUsuario = "admin",
                PasswordHash = hasher.HashPassword("Admin123!"),
                RolId = rolAdmin.Id,
                Activo = true,
                DebeCambiarPassword = true,
                FechaCreacion = DateTime.UtcNow
            });
        }

        if (!await context.Usuarios.AnyAsync(u => u.NombreUsuario == "cajero"))
        {
            context.Usuarios.Add(new Usuario
            {
                NombreCompleto = "Cajero Principal",
                NombreUsuario = "cajero",
                PasswordHash = hasher.HashPassword("Cajero123!"),
                RolId = rolCajero.Id,
                Activo = true,
                DebeCambiarPassword = true,
                FechaCreacion = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();

        // 5. Sembrar Categorías Iniciales para Tienda de Celulares
        if (!await context.Categorias.AnyAsync())
        {
            var catCel = new Categoria { Nombre = "Celulares y Smartphones", PrefijoSku = "CEL", Descripcion = "Teléfonos móviles de todas las marcas y gamas", Activo = true };
            var catAcc = new Categoria { Nombre = "Accesorios Generales", PrefijoSku = "ACC", Descripcion = "Audífonos, soportes, adaptadores", Activo = true };
            var catCar = new Categoria { Nombre = "Cargadores y Cables", PrefijoSku = "CAR", Descripcion = "Cargadores rápidos, cables Tipo C, Lightning", Activo = true };
            var catPro = new Categoria { Nombre = "Protectores y Fundas", PrefijoSku = "PRO", Descripcion = "Fundas de silicona, carcasas antigolpes, vidrios templados", Activo = true };
            var catRep = new Categoria { Nombre = "Repuestos y Pantallas", PrefijoSku = "REP", Descripcion = "Pantallas OLED/LCD, baterías de reemplazo", Activo = true };

            context.Categorias.AddRange(catCel, catAcc, catCar, catPro, catRep);
            await context.SaveChangesAsync();

            // Productos de ejemplo iniciales
            context.Productos.AddRange(
                new Producto
                {
                    Nombre = "Samsung Galaxy A54 5G 128GB",
                    CategoriaId = catCel.Id,
                    Sku = "CEL-0001",
                    PrecioCosto = 14500.00m,
                    PrecioVenta = 19500.00m,
                    StockActual = 8,
                    CantidadMinima = 2,
                    CodigoBarras = "7421001234567",
                    Descripcion = "Pantalla 6.4 FHD+ 120Hz, 8GB RAM, Cámara 50MP",
                    Activo = true
                },
                new Producto
                {
                    Nombre = "iPhone 13 128GB Midnight",
                    CategoriaId = catCel.Id,
                    Sku = "CEL-0002",
                    PrecioCosto = 28000.00m,
                    PrecioVenta = 35000.00m,
                    StockActual = 5,
                    CantidadMinima = 2,
                    CodigoBarras = "194252707203",
                    Descripcion = "Chip A15 Bionic, pantalla Super Retina XDR",
                    Activo = true
                },
                new Producto
                {
                    Nombre = "Cargador Rápido 25W Tipo C",
                    CategoriaId = catCar.Id,
                    Sku = "CAR-0001",
                    PrecioCosto = 450.00m,
                    PrecioVenta = 950.00m,
                    StockActual = 20,
                    CantidadMinima = 5,
                    CodigoBarras = "8806090558122",
                    Descripcion = "Power Delivery 3.0 para Samsung y otros",
                    Activo = true
                }
            );

            await context.SaveChangesAsync();
        }
    }
}
