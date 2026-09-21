# CHANGELOG

## [0.4.0] - 2026-09-20
- Implementación completa de la Fase 4: Proveedores.
- Creación de la entidad `Proveedor` con campos para razón social, identificación fiscal de República Dominicana (RNC), teléfono, email, dirección, persona de contacto y estado activo.
- Implementación de `IProveedorService` y `ProveedorService` con validación de RNC único e indexación.
- Implementación de formularios WinForms para la gestión de proveedores:
  - `ProveedoresForm`: Listado interactivo con búsqueda en tiempo real (por nombre, RNC, contacto, teléfono), filtro de activos y acciones.
  - `ProveedorModalForm`: Diálogo de creación y modificación de proveedores.
- Desactivación lógica de proveedores para garantizar la trazabilidad en compras existentes.
- Sembrado inicial con 3 distribuidores e importadores clave de celulares y repuestos de República Dominicana.
- Suite de 5 nuevas pruebas unitarias para proveedores (total de 30 pruebas pasando al 100%).

## [0.3.0] - 2026-09-20
- Implementación completa de la Fase 3: Productos, categorías y SKU.
- Creación de entidades `Categoria` y `Producto` con llaves foráneas, índices únicos en `Sku` y `Nombre` de categoría, e índice en `CodigoBarras`.
- Implementación de `ICategoriaService`, `CategoriaService`, `IProductoService` y `ProductoService`.
- Implementación de algoritmo de generación automática de SKU basado en el prefijo de categoría (ej. `CEL-0001`, `ACC-0001`) con soporte para personalización manual y garantía de unicidad.
- Control de margen de ganancia en tiempo real y alertas visuales automáticas de stock mínimo.
- Desactivación lógica de productos para preservar la trazabilidad histórica de transacciones futuras.
- Implementación de formularios WinForms para el catálogo:
  - `CategoriasForm` y `CategoriaModalForm`: Administración de categorías y configuración de prefijos de SKU.
  - `ProductosForm` y `ProductoModalForm`: Catálogo visual con búsqueda rápida, filtros de categoría, filtro de alerta de bajo stock, botón de generación de SKU automático y formateo de precios en pesos dominicanos (RD$).
- Sembrado inicial de categorías comunes de tienda de celulares y productos demo.
- Suite de 7 nuevas pruebas unitarias para el catálogo (total de 25 pruebas pasando al 100%).

## [0.2.0] - 2026-09-20
- Implementación completa de la Fase 2: Apertura y cierre de caja (turnos de trabajo).
- Creación de la entidad `Turno` con soporte para monto de apertura, ventas acumuladas en efectivo, monto esperado, dinero físico contado (arqueo), cálculo de diferencia y observaciones.
- Implementación de `ITurnoService` y `TurnoService` con validación de turno único activo por usuario/caja y cálculo en vivo del efectivo esperado.
- Implementación de formularios WinForms para gestión de caja:
  - `AbrirTurnoModalForm`: Apertura de turno con monto inicial en efectivo en pesos dominicanos (RD$).
  - `CerrarTurnoModalForm`: Resumen automático del turno (inicial + ventas), captura de dinero físico contado y cálculo en tiempo real de faltante/sobrante/cuadre con alertas visuales.
  - `HistorialTurnosForm`: Panel de estado de turno del cajero conectado, listado filtrable por fechas y usuarios, y detalle de diferencias.
- Suite de 7 nuevas pruebas unitarias para turnos (total de 18 pruebas pasando al 100%).

## [0.1.0] - 2026-09-20
- Creación de solución modular en .NET 8 con WinForms, EF Core y SQLite.
- Implementación de la Fase 1: Sistema de roles y permisos (RBAC).
- Implementación de entidades `Usuario`, `Rol`, `Permiso` y `RolPermiso`.
- Implementación de catálogo cerrado de permisos de seguridad y de negocio.
- Implementación de sembrado inicial con roles fijos (Super Admin, Admin, Cajero) y usuarios base.
- Implementación de hashing seguro de contraseñas con BCrypt (`BCrypt.Net-Next`).
- Implementación del flujo de cambio de contraseña obligatorio para usuarios nuevos o reseteados.
- Implementación de reseteo de contraseñas por Super Admin / Admin con emisión de contraseña temporal.
- Implementación de pantallas WinForms con tema profesional.
- Implementación de suite de 11 pruebas unitarias automatizadas en xUnit (todas pasando al 100%).
- Configuración de localización para República Dominicana (Moneda RD$ / DOP, formato es-DO).
