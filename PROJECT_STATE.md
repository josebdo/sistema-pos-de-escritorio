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
Fase: Fase 5: Compras a proveedor (actualiza inventario)
Estado: EN PROGRESO
Último commit verificado: d2227ad
Última actividad: Culminación de la Fase 4 (Proveedores) con build y 30 tests unitarios pasando al 100%.
Fecha: 2026-09-20

## Fases completadas
- Fase 0: Inicialización del repositorio, reglas canónicas y arquitectura base (commit: c116627)
- Fase 1: Sistema de roles y permisos (RBAC), seguridad BCrypt y formularios WinForms (commit: c116627)
- Fase 2: Apertura y cierre de caja (turnos), arqueo y cálculo de diferencias (commit: 5013c71)
- Fase 3: Productos, categorías y SKU con generación automática e índice único (commit: 5095c48)
- Fase 4: Proveedores comerciales con soporte para RNC dominicano y desactivación lógica (commit: d2227ad)

## Fase actual
Descripción: Registro de compras a proveedores con incremento automático de inventario, método de costeo "último costo" y consulta de historial por proveedor y producto.
Objetivo: Entidades `Compra` y `DetalleCompra`, registro transaccional que incrementa stock de productos, actualiza precio de costo si varió sin alterar precio de venta automáticamente, y pantallas de registro y consulta de compras.
Definition of Ready (qué debía estar resuelto antes de empezar esta fase):
- [x] Fase 3 (Productos) y Fase 4 (Proveedores) COMPLETADAS.
- [x] Método de costeo definido ("último costo") registrado en `docs/DECISIONS.md` (DEC-006).
- [x] Catálogo de permisos `Compras.Registrar` y `Compras.Historial` disponibles en RBAC.
Tareas completadas:
- [x] Permisos de compras configurados en el catálogo.
Tareas pendientes:
- [ ] Registro de decisión arquitectónica DEC-006 (Método de costeo: Último costo).
- [ ] Modelado de entidades: `Compra`, `DetalleCompra`.
- [ ] Configuración en `AppDbContext` (tablas, relaciones, precisión decimal en RD$).
- [ ] Implementación de `ICompraService` y `CompraService`.
- [ ] Formularios WinForms: `RegistrarCompraForm` e `HistorialComprasForm`.
- [ ] Pruebas unitarias para incremento de stock, actualización de precio de costo y consultas transaccionales.

## Próximo paso
Registrar la decisión arquitectónica DEC-006 (Método de costeo: último costo), confirmar con el usuario e iniciar la Fase 5: Compras a proveedor.

## Verificación de la última sesión
Build: OK (0 advertencias, 0 errores en `SistemaCelulares.sln`)
Tests ejecutados: dotnet test SistemaCelulares.sln
Resultado: 30/30 pasaron (100% de éxito)
Errores pendientes: Ninguno

## Decisiones arquitectónicas
Ver `docs/DECISIONS.md` — DEC-001 (SQLite), DEC-002 (EF Core), DEC-003 (Localización RD), DEC-004 (RBAC y BCrypt), DEC-005 (Generación de SKU).

## Base de datos
Entidades creadas: `Usuario`, `Rol`, `Permiso`, `RolPermiso`, `Turno`, `Categoria`, `Producto`, `Proveedor`
Última migración: Esquema con directorio de proveedores y RNC dominicano

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
Implementación completa de la Fase 4 (Proveedores comerciales con RNC dominicano, interfaz WinForms y 30 pruebas unitarias al 100%).

## Notas importantes
- Moneda por defecto: Peso Dominicano (RD$ / DOP) con formato regional `es-DO`.
- Al confirmar una compra a proveedor, el precio de costo del producto se actualiza con el de la compra más reciente ("último costo") y el precio de venta permanece intacto para no afectar márgenes sin aprobación del Admin.
