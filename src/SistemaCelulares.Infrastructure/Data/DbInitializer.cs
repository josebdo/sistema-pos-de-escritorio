using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Constants;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;

namespace SistemaCelulares.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context, IPasswordHasher hasher, bool sembrarDatosPrueba = false)
    {
        // Asegurar que la base de datos y esquema existen
        await context.Database.EnsureCreatedAsync();

        // Aplicar migraciones automáticas de columnas/tablas para SQLite si la base de datos ya existía
        await AplicarMigracionesSqliteAsync(context);

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

        var rolTecnico = await context.Roles.Include(r => r.RolPermisos)
            .FirstOrDefaultAsync(r => r.Nombre == Rol.Tecnico);
        if (rolTecnico == null)
        {
            rolTecnico = new Rol
            {
                Nombre = Rol.Tecnico,
                Descripcion = "Técnico de reparaciones: crea, diagnostica y repara equipos",
                EsFijo = true,
                Activo = true
            };
            context.Roles.Add(rolTecnico);
            await context.SaveChangesAsync();
        }

        var rolAlmacenista = await context.Roles.Include(r => r.RolPermisos)
            .FirstOrDefaultAsync(r => r.Nombre == Rol.Almacenista);
        if (rolAlmacenista == null)
        {
            rolAlmacenista = new Rol
            {
                Nombre = Rol.Almacenista,
                Descripcion = "Encargado de almacén: inventario, stock con IMEI, compras y recepción",
                EsFijo = true,
                Activo = true
            };
            context.Roles.Add(rolAlmacenista);
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
            Permisos.ClientesVer, Permisos.ClientesCrear, Permisos.ClientesEditar, Permisos.ClientesEliminar,
            Permisos.TurnosAbrir, Permisos.TurnosCerrar, Permisos.TurnosHistorial,
            Permisos.ProductosVer, Permisos.ProductosCrear, Permisos.ProductosEditar, Permisos.ProductosDesactivar, Permisos.CategoriasGestionar, Permisos.AlertasStockVer,
            Permisos.ProveedoresGestionar, Permisos.ComprasRegistrar, Permisos.ComprasHistorial,
            Permisos.VentasRegistrar, Permisos.VentasAnular, Permisos.VentasHistorial,
            Permisos.ReparacionesVer, Permisos.ReparacionesCrear, Permisos.ReparacionesEditar, Permisos.ReparacionesCobrar,
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

        // Cajero tiene permisos limitados a ventas, clientes y caja (más cobrar reparaciones ya listas)
        var codigosPermisosCajero = new HashSet<string>
        {
            Permisos.TurnosAbrir, Permisos.TurnosCerrar,
            Permisos.ClientesVer, Permisos.ClientesCrear,
            Permisos.ProductosVer,
            Permisos.VentasRegistrar, Permisos.VentasHistorial,
            Permisos.ReparacionesVer, Permisos.ReparacionesCobrar
        };

        foreach (var codigo in codigosPermisosCajero)
        {
            if (permisosPorCodigo.TryGetValue(codigo, out var permiso) &&
                !rolCajero.RolPermisos.Any(rp => rp.PermisoId == permiso.Id))
            {
                context.RolPermisos.Add(new RolPermiso { RolId = rolCajero.Id, PermisoId = permiso.Id });
            }
        }

        // Técnico tiene permisos para crear, editar y diagnosticar reparaciones, ver clientes y consultar productos
        var codigosPermisosTecnico = new HashSet<string>
        {
            Permisos.ReparacionesVer,
            Permisos.ReparacionesCrear,
            Permisos.ReparacionesEditar,
            Permisos.ClientesVer,
            Permisos.ClientesCrear,
            Permisos.ProductosVer
        };

        foreach (var codigo in codigosPermisosTecnico)
        {
            if (permisosPorCodigo.TryGetValue(codigo, out var permiso) &&
                !rolTecnico.RolPermisos.Any(rp => rp.PermisoId == permiso.Id))
            {
                context.RolPermisos.Add(new RolPermiso { RolId = rolTecnico.Id, PermisoId = permiso.Id });
            }
        }

        // Almacenista tiene permisos para inventario, productos, proveedores y compras
        var codigosPermisosAlmacenista = new HashSet<string>
        {
            Permisos.ProductosVer,
            Permisos.ProductosCrear,
            Permisos.ProductosEditar,
            Permisos.CategoriasGestionar,
            Permisos.AlertasStockVer,
            Permisos.ProveedoresGestionar,
            Permisos.ComprasRegistrar,
            Permisos.ComprasHistorial
        };

        foreach (var codigo in codigosPermisosAlmacenista)
        {
            if (permisosPorCodigo.TryGetValue(codigo, out var permiso) &&
                !rolAlmacenista.RolPermisos.Any(rp => rp.PermisoId == permiso.Id))
            {
                context.RolPermisos.Add(new RolPermiso { RolId = rolAlmacenista.Id, PermisoId = permiso.Id });
            }
        }

        await context.SaveChangesAsync();

        // 4. Sembrar Usuario Único SuperAdmin de Soporte Técnico (josebdo / Jo1991ga)
        var userSoporte = await context.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == "josebdo");
        if (userSoporte == null)
        {
            context.Usuarios.Add(new Usuario
            {
                NombreCompleto = "Jose BDO (Soporte Técnico)",
                NombreUsuario = "josebdo",
                PasswordHash = hasher.HashPassword("Jo1991ga"),
                RolId = rolSuperAdmin.Id,
                Activo = true,
                DebeCambiarPassword = false, // Acceso directo para soporte
                FechaCreacion = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        // 5. Sembrar Configuración Inicial del Negocio
        if (!await context.ConfiguracionesNegocio.AnyAsync())
        {
            context.ConfiguracionesNegocio.Add(new ConfiguracionNegocio
            {
                NombreEmpresa = "Mi Tienda de Celulares",
                RncCedula = "000-0000000-0",
                Telefono = "809-000-0000",
                WhatsApp = "809-000-0000",
                Email = "contacto@mitienda.com",
                Direccion = "Calle Principal #1",
                Ciudad = "Santo Domingo, RD",
                MensajePieFactura = "¡Gracias por su compra! Garantía de 30 días con su factura original.",
                MensajeGarantiaReparacion = "Equipos no retirados en un plazo de 30 días pasarán a disposición del taller.",
                MonedaSimbolo = "RD$",
                ItbisPorcentaje = 18.00m,
                UltimaModificacion = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        // 6. Sembrar Categorías Financieras Base (Gastos e Ingresos Operativos)
        if (!await context.CategoriasFinancieras.AnyAsync())
        {
            context.CategoriasFinancieras.AddRange(
                // Gastos Operativos
                new CategoriaFinanciera { Nombre = "Alquiler de Local", Tipo = TipoMovimientoFinanciero.Gasto, Descripcion = "Pago mensual del local comercial", Activo = true },
                new CategoriaFinanciera { Nombre = "Electricidad / Luz", Tipo = TipoMovimientoFinanciero.Gasto, Descripcion = "Factura de energía eléctrica (EDESUR/EDEESTE/EDENORTE)", Activo = true },
                new CategoriaFinanciera { Nombre = "Internet y Telefonía", Tipo = TipoMovimientoFinanciero.Gasto, Descripcion = "Servicios de telecomunicaciones del negocio", Activo = true },
                new CategoriaFinanciera { Nombre = "Nómina y Salarios", Tipo = TipoMovimientoFinanciero.Gasto, Descripcion = "Pago a empleados, comisiones de personal", Activo = true },
                new CategoriaFinanciera { Nombre = "Mantenimiento y Reparaciones", Tipo = TipoMovimientoFinanciero.Gasto, Descripcion = "Mantenimiento del local, aire acondicionado, herramientas", Activo = true },
                new CategoriaFinanciera { Nombre = "Comisiones y Servicios Bancarios", Tipo = TipoMovimientoFinanciero.Gasto, Descripcion = "Comisiones de verifone, transferencias, mantenimiento de cuenta", Activo = true },
                new CategoriaFinanciera { Nombre = "Transporte y Envíos", Tipo = TipoMovimientoFinanciero.Gasto, Descripcion = "Fletes de mercancía, mensajería y combustible", Activo = true },
                new CategoriaFinanciera { Nombre = "Otros Gastos Operativos", Tipo = TipoMovimientoFinanciero.Gasto, Descripcion = "Gastos misceláneos y menores del día a día", Activo = true },

                // Ingresos Adicionales
                new CategoriaFinanciera { Nombre = "Servicio Técnico y Reparaciones", Tipo = TipoMovimientoFinanciero.Ingreso, Descripcion = "Mano de obra por reparación y cambio de pantallas/piezas", Activo = true },
                new CategoriaFinanciera { Nombre = "Desbloqueos y Flasheo", Tipo = TipoMovimientoFinanciero.Ingreso, Descripcion = "Servicios de software, desbloqueo de red y cuentas", Activo = true },
                new CategoriaFinanciera { Nombre = "Otros Ingresos", Tipo = TipoMovimientoFinanciero.Ingreso, Descripcion = "Ingresos varios no provenientes de venta de inventario", Activo = true }
            );

            await context.SaveChangesAsync();
        }

        // 7. Sembrar Secuencias de Comprobantes Fiscales (NCF) Iniciales de DGII
        if (!await context.ComprobanteFiscalSecuencias.AnyAsync())
        {
            context.ComprobanteFiscalSecuencias.AddRange(
                new ComprobanteFiscalSecuencia
                {
                    Tipo = TipoComprobanteFiscal.Consumo_B02,
                    Serie = "B",
                    CodigoTipo = "02",
                    SecuenciaActual = 1,
                    SecuenciaHasta = 100000,
                    FechaVencimiento = DateTime.UtcNow.AddYears(2),
                    Activo = true,
                    Descripcion = "Facturas de Consumo (Consumidor Final)"
                },
                new ComprobanteFiscalSecuencia
                {
                    Tipo = TipoComprobanteFiscal.CreditoFiscal_B01,
                    Serie = "B",
                    CodigoTipo = "01",
                    SecuenciaActual = 1,
                    SecuenciaHasta = 50000,
                    FechaVencimiento = DateTime.UtcNow.AddYears(2),
                    Activo = true,
                    Descripcion = "Facturas con Valor de Crédito Fiscal (Requiere RNC)"
                },
                new ComprobanteFiscalSecuencia
                {
                    Tipo = TipoComprobanteFiscal.RegimenEspecial_B14,
                    Serie = "B",
                    CodigoTipo = "14",
                    SecuenciaActual = 1,
                    SecuenciaHasta = 10000,
                    FechaVencimiento = DateTime.UtcNow.AddYears(2),
                    Activo = true,
                    Descripcion = "Comprobantes para Regímenes Especiales de Tributación"
                },
                new ComprobanteFiscalSecuencia
                {
                    Tipo = TipoComprobanteFiscal.Gubernamental_B15,
                    Serie = "B",
                    CodigoTipo = "15",
                    SecuenciaActual = 1,
                    SecuenciaHasta = 10000,
                    FechaVencimiento = DateTime.UtcNow.AddYears(2),
                    Activo = true,
                    Descripcion = "Comprobantes Gubernamentales"
                }
            );

            await context.SaveChangesAsync();
        }

        // 8. Sembrar Cliente Consumidor Final por defecto
        if (!await context.Clientes.AnyAsync())
        {
            context.Clientes.Add(new Cliente
            {
                NombreCompleto = "Consumidor Final (Cliente Genérico)",
                RncOCedula = "000-0000000-0",
                Telefono = "809-000-0000",
                Email = "cliente@tienda.do",
                Direccion = "Santo Domingo, RD",
                Activo = true
            });

            await context.SaveChangesAsync();
        }

        // 9. Si se solicita modo de pruebas o si se está ejecutando en base de datos InMemory de pruebas unitarias
        bool esPruebaUnitaria = sembrarDatosPrueba || (context.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true);
        if (esPruebaUnitaria)
        {
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
                    NombreCompleto = "Carlos Martínez",
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
                    NombreCompleto = "Ramón Pérez",
                    NombreUsuario = "cajero",
                    PasswordHash = hasher.HashPassword("Cajero123!"),
                    RolId = rolCajero.Id,
                    Activo = true,
                    DebeCambiarPassword = true,
                    FechaCreacion = DateTime.UtcNow
                });
            }

            if (!await context.Usuarios.AnyAsync(u => u.NombreUsuario == "tecnico"))
            {
                context.Usuarios.Add(new Usuario
                {
                    NombreCompleto = "Junior De León",
                    NombreUsuario = "tecnico",
                    PasswordHash = hasher.HashPassword("Tecnico123!"),
                    RolId = rolTecnico.Id,
                    Activo = true,
                    DebeCambiarPassword = true,
                    FechaCreacion = DateTime.UtcNow
                });
            }

            if (!await context.Categorias.AnyAsync())
            {
                var catCel = new Categoria { Nombre = "Celulares y Smartphones", PrefijoSku = "CEL", Descripcion = "Teléfonos móviles de todas las marcas y gamas", Activo = true };
                var catAcc = new Categoria { Nombre = "Accesorios Generales", PrefijoSku = "ACC", Descripcion = "Audífonos, soportes, adaptadores", Activo = true };
                var catCar = new Categoria { Nombre = "Cargadores y Cables", PrefijoSku = "CAR", Descripcion = "Cargadores rápidos, cables Tipo C, Lightning", Activo = true };
                var catPro = new Categoria { Nombre = "Protectores y Fundas", PrefijoSku = "PRO", Descripcion = "Fundas de silicona, carcasas antigolpes, vidrios templados", Activo = true };
                var catRep = new Categoria { Nombre = "Repuestos y Pantallas", PrefijoSku = "REP", Descripcion = "Pantallas OLED/LCD, baterías de reemplazo", Activo = true };

                context.Categorias.AddRange(catCel, catAcc, catCar, catPro, catRep);
                await context.SaveChangesAsync();

                var celSamsung = new Producto
                {
                    Nombre = "Samsung Galaxy A54 5G 128GB",
                    CategoriaId = catCel.Id,
                    Sku = "CEL-0001",
                    PrecioCosto = 14500.00m,
                    PrecioVenta = 19500.00m,
                    StockActual = 2,
                    CantidadMinima = 1,
                    CodigoBarras = "7421001234567",
                    Descripcion = "Pantalla 6.4 FHD+ 120Hz, 8GB RAM, Cámara 50MP",
                    RequiereSerie = true,
                    Activo = true
                };

                var celIphone = new Producto
                {
                    Nombre = "iPhone 13 128GB Midnight",
                    CategoriaId = catCel.Id,
                    Sku = "CEL-0002",
                    PrecioCosto = 28000.00m,
                    PrecioVenta = 35000.00m,
                    StockActual = 2,
                    CantidadMinima = 1,
                    CodigoBarras = "194252707203",
                    Descripcion = "Chip A15 Bionic, pantalla Super Retina XDR",
                    RequiereSerie = true,
                    Activo = true
                };

                var cargador = new Producto
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
                    RequiereSerie = false,
                    Activo = true
                };

                context.Productos.AddRange(celSamsung, celIphone, cargador);
                await context.SaveChangesAsync();

                context.UnidadesProducto.AddRange(
                    new UnidadProducto { ProductoId = celSamsung.Id, Imei = "356789123456781", Estado = EstadoUnidadProducto.EnStock, FechaIngreso = DateTime.UtcNow },
                    new UnidadProducto { ProductoId = celSamsung.Id, Imei = "356789123456782", Estado = EstadoUnidadProducto.EnStock, FechaIngreso = DateTime.UtcNow },
                    new UnidadProducto { ProductoId = celIphone.Id, Imei = "359876543210981", Estado = EstadoUnidadProducto.EnStock, FechaIngreso = DateTime.UtcNow },
                    new UnidadProducto { ProductoId = celIphone.Id, Imei = "359876543210982", Estado = EstadoUnidadProducto.EnStock, FechaIngreso = DateTime.UtcNow }
                );
                await context.SaveChangesAsync();
            }

            if (!await context.Proveedores.AnyAsync())
            {
                context.Proveedores.AddRange(
                    new Proveedor
                    {
                        Nombre = "Distribuidora Celular Dominicana SRL",
                        Rnc = "101-84920-1",
                        Telefono = "809-555-0199",
                        Email = "ventas@districelular.do",
                        Direccion = "Av. 27 de Febrero #240, Santo Domingo, D.N.",
                        Contacto = "Lic. Carlos Méndez",
                        Activo = true
                    },
                    new Proveedor
                    {
                        Nombre = "Tech Import RD SRL",
                        Rnc = "131-72948-2",
                        Telefono = "809-555-0245",
                        Email = "pedidos@techimportrd.com",
                        Direccion = "Av. John F. Kennedy #88, Santo Domingo",
                        Contacto = "Ing. Roberto Almonte",
                        Activo = true
                    }
                );
                await context.SaveChangesAsync();
            }
        }
    }

    private static async Task AplicarMigracionesSqliteAsync(AppDbContext context)
    {
        if (!context.Database.IsSqlite()) return;

        // 1. Columnas nuevas en Clientes
        await EjecutarSqlSeguroAsync(context, "ALTER TABLE Clientes ADD COLUMN EsFrecuente INTEGER NOT NULL DEFAULT 0;");
        await EjecutarSqlSeguroAsync(context, "ALTER TABLE Clientes ADD COLUMN PorcentajeDescuento TEXT NOT NULL DEFAULT '0';");

        // 2. Columnas nuevas en Productos
        await EjecutarSqlSeguroAsync(context, "ALTER TABLE Productos ADD COLUMN RequiereSerie INTEGER NOT NULL DEFAULT 0;");

        // 3. Columnas nuevas en DetalleVentas
        await EjecutarSqlSeguroAsync(context, "ALTER TABLE DetalleVentas ADD COLUMN UnidadProductoId INTEGER NULL;");
        await EjecutarSqlSeguroAsync(context, "ALTER TABLE DetalleVentas ADD COLUMN Imei TEXT NULL;");
        await EjecutarSqlSeguroAsync(context, "ALTER TABLE DetallesVenta ADD COLUMN UnidadProductoId INTEGER NULL;");
        await EjecutarSqlSeguroAsync(context, "ALTER TABLE DetallesVenta ADD COLUMN Imei TEXT NULL;");

        // 4. Tabla UnidadesProducto
        await EjecutarSqlSeguroAsync(context, @"
            CREATE TABLE IF NOT EXISTS UnidadesProducto (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ProductoId INTEGER NOT NULL,
                Imei TEXT NOT NULL,
                Estado INTEGER NOT NULL DEFAULT 1,
                FechaIngreso TEXT NOT NULL,
                FechaVenta TEXT NULL,
                VentaId INTEGER NULL,
                Notas TEXT NULL,
                FOREIGN KEY (ProductoId) REFERENCES Productos (Id) ON DELETE RESTRICT,
                FOREIGN KEY (VentaId) REFERENCES Ventas (Id) ON DELETE SET NULL
            );
        ");
        await EjecutarSqlSeguroAsync(context, "CREATE UNIQUE INDEX IF NOT EXISTS IX_UnidadesProducto_Imei ON UnidadesProducto (Imei);");
        await EjecutarSqlSeguroAsync(context, "CREATE INDEX IF NOT EXISTS IX_UnidadesProducto_ProductoId ON UnidadesProducto (ProductoId);");
        await EjecutarSqlSeguroAsync(context, "CREATE INDEX IF NOT EXISTS IX_UnidadesProducto_VentaId ON UnidadesProducto (VentaId);");

        // 5. Tabla OrdenesReparacion
        await EjecutarSqlSeguroAsync(context, @"
            CREATE TABLE IF NOT EXISTS OrdenesReparacion (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                NumeroOrden TEXT NOT NULL,
                ClienteId INTEGER NOT NULL,
                Marca TEXT NOT NULL,
                Modelo TEXT NOT NULL,
                ImeiOSerie TEXT NULL,
                DescripcionProblema TEXT NOT NULL,
                NotasDiagnostico TEXT NULL,
                PrecioEstimado TEXT NOT NULL DEFAULT '0',
                PrecioFinal TEXT NOT NULL DEFAULT '0',
                Estado INTEGER NOT NULL DEFAULT 1,
                FechaRecepcion TEXT NOT NULL,
                FechaListaParaEntrega TEXT NULL,
                FechaEntrega TEXT NULL,
                UsuarioRecepcionId INTEGER NOT NULL,
                UsuarioEntregaId INTEGER NULL,
                PagoId INTEGER NULL,
                FOREIGN KEY (ClienteId) REFERENCES Clientes (Id) ON DELETE RESTRICT,
                FOREIGN KEY (UsuarioRecepcionId) REFERENCES Usuarios (Id) ON DELETE RESTRICT,
                FOREIGN KEY (UsuarioEntregaId) REFERENCES Usuarios (Id) ON DELETE RESTRICT,
                FOREIGN KEY (PagoId) REFERENCES Pagos (Id) ON DELETE RESTRICT
            );
        ");
        await EjecutarSqlSeguroAsync(context, "CREATE UNIQUE INDEX IF NOT EXISTS IX_OrdenesReparacion_NumeroOrden ON OrdenesReparacion (NumeroOrden);");
        await EjecutarSqlSeguroAsync(context, "CREATE INDEX IF NOT EXISTS IX_OrdenesReparacion_ClienteId ON OrdenesReparacion (ClienteId);");
        await EjecutarSqlSeguroAsync(context, "CREATE INDEX IF NOT EXISTS IX_OrdenesReparacion_PagoId ON OrdenesReparacion (PagoId);");
        await EjecutarSqlSeguroAsync(context, "CREATE INDEX IF NOT EXISTS IX_OrdenesReparacion_UsuarioRecepcionId ON OrdenesReparacion (UsuarioRecepcionId);");
        await EjecutarSqlSeguroAsync(context, "CREATE INDEX IF NOT EXISTS IX_OrdenesReparacion_UsuarioEntregaId ON OrdenesReparacion (UsuarioEntregaId);");

        // 6. Tabla ConfiguracionesNegocio
        await EjecutarSqlSeguroAsync(context, @"
            CREATE TABLE IF NOT EXISTS ConfiguracionesNegocio (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                NombreEmpresa TEXT NOT NULL,
                RncCedula TEXT NOT NULL,
                Telefono TEXT NULL,
                WhatsApp TEXT NULL,
                Email TEXT NULL,
                Direccion TEXT NULL,
                Ciudad TEXT NULL,
                MensajePieFactura TEXT NULL,
                MensajeGarantiaReparacion TEXT NULL,
                MonedaSimbolo TEXT NULL,
                ItbisPorcentaje TEXT NOT NULL DEFAULT '18.00',
                UltimaModificacion TEXT NOT NULL
            );
        ");

        // 7. Columnas en Ventas
        await EjecutarSqlSeguroAsync(context, "ALTER TABLE Ventas ADD COLUMN Descuento TEXT NOT NULL DEFAULT '0';");
        await EjecutarSqlSeguroAsync(context, "ALTER TABLE Ventas ADD COLUMN Subtotal TEXT NOT NULL DEFAULT '0';");
        await EjecutarSqlSeguroAsync(context, "ALTER TABLE Ventas ADD COLUMN Itbis TEXT NOT NULL DEFAULT '0';");
    }

    private static async Task EjecutarSqlSeguroAsync(AppDbContext context, string sql)
    {
        try
        {
            await context.Database.ExecuteSqlRawAsync(sql);
        }
        catch
        {
            // Ignora errores si la columna o índice ya existe
        }
    }
}
