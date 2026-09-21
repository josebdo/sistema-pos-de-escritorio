# PROJECT STATE

## Proyecto
Nombre: Sistema de Escritorio para Tienda de Celulares
Descripción: Aplicación de escritorio en WinForms .NET 8 / C# con SQLite y Entity Framework Core para la gestión de inventario, ventas, compras, caja y facturación con soporte para República Dominicana.

## Stack
- C# 12
- .NET 8.0 (WindowsDesktop WinForms)
- Entity Framework Core 8.0 (SQLite)
- BCrypt.Net-Next 4.0.3 (Hashing de contraseñas)
- xUnit 2.5 (Pruebas unitarias)

## Estado actual
Fase: Fase 4: Proveedores
Estado: EN PROGRESO
Último commit verificado: 5095c48
Última actividad: Culminación de la Fase 3 (Productos, Categorías y SKU) con build y 25 tests unitarios pasando al 100%.
Fecha: 2026-09-20

## Fases completadas
- Fase 0: Inicialización del repositorio, reglas canónicas y arquitectura base (commit: c116627)
- Fase 1: Sistema de roles y permisos (RBAC), seguridad BCrypt y formularios WinForms (commit: c116627)
- Fase 2: Apertura y cierre de caja (turnos), arqueo y cálculo de diferencias (commit: 5013c71)
- Fase 3: Productos, categorías y SKU con generación automática e índice único (commit: 5095c48)

## Fase actual
Descripción: Gestión de proveedores del negocio (crear, editar, desactivar) con soporte para identificación fiscal dominicana (RNC).
Objetivo: CRUD de proveedores (Nombre, RNC fiscal dominicano, Teléfono, Email, Dirección, Contacto), validación de RNC y desactivación en lugar de eliminación física para trazabilidad de compras.
Definition of Ready (qué debía estar resuelto antes de empezar esta fase):
- [x] Fase 1 (Roles y permisos) y Fase 3 (Productos y catálogo) COMPLETADAS.
- [x] Catálogo de permisos `Proveedores.Gestionar` disponible en RBAC.
Tareas completadas:
- [x] Permiso `Proveedores.Gestionar` activo en catálogo canónico.
Tareas pendientes:
- [ ] Modelado de entidad `Proveedor` en `Core`.
- [ ] Configuración en `AppDbContext` (índices, campos, longitud de RNC).
- [ ] Implementación de `IProveedorService` y `ProveedorService`.
- [ ] Formularios WinForms: `ProveedoresForm` y `ProveedorModalForm`.
- [ ] Pruebas unitarias para validaciones de proveedor, formato de RNC y desactivación lógica.

## Próximo paso
Confirmar con el usuario el inicio del desarrollo de la Fase 4 (Proveedores), modelar la entidad `Proveedor` y construir la interfaz de gestión.

## Verificación de la última sesión
Build: OK (0 advertencias, 0 errores en `SistemaCelulares.sln`)
Tests ejecutados: dotnet test SistemaCelulares.sln
Resultado: 25/25 pasaron (100% de éxito)
Errores pendientes: Ninguno

## Decisiones arquitectónicas
Ver `docs/DECISIONS.md` — DEC-001 (SQLite), DEC-002 (EF Core), DEC-003 (Localización RD), DEC-004 (RBAC y BCrypt), DEC-005 (Generación de SKU).

## Base de datos
Entidades creadas: `Usuario`, `Rol`, `Permiso`, `RolPermiso`, `Turno`, `Categoria`, `Producto`
Última migración: Esquema con catálogo completo de productos y categorías

## Problemas conocidos
Ninguno.

## Dependencias
Librerías instaladas:
- Microsoft.EntityFrameworkCore.Sqlite (8.0.13)
- Microsoft.EntityFrameworkCore.Design (8.0.13)
- BCrypt.Net-Next (4.0.3)
- Microsoft.Extensions.DependencyInjection (8.0.1)
- Microsoft.EntityFrameworkCore.InMemory (8.0.13)

## Cambios recientes
Implementación completa de la Fase 3 (Categorías con prefijos, Productos con SKU automático correlativo, precios en RD$, alertas de stock mínimo y 25 pruebas unitarias al 100%).

## Notas importantes
- Moneda por defecto: Peso Dominicano (RD$ / DOP) con formato regional `es-DO`.
- No se pueden eliminar físicamente productos ni categorías con registros activos; se desactivan para preservar trazabilidad.
- Los SKUs son generados automáticamente en base al prefijo de la categoría con formato `XXX-0001` y se validan como únicos.
