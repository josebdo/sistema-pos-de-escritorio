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
Fase: Fase 6: Alertas de stock mínimo
Estado: EN PROGRESO
Último commit verificado: 83b13ba
Última actividad: Culminación de la Fase 5 (Compras a proveedor) con build y 34 tests unitarios pasando al 100%.
Fecha: 2026-09-20

## Fases completadas
- Fase 0: Inicialización del repositorio, reglas canónicas y arquitectura base (commit: c116627)
- Fase 1: Sistema de roles y permisos (RBAC), seguridad BCrypt y formularios WinForms (commit: c116627)
- Fase 2: Apertura y cierre de caja (turnos), arqueo y cálculo de diferencias (commit: 5013c71)
- Fase 3: Productos, categorías y SKU con generación automática e índice único (commit: 5095c48)
- Fase 4: Proveedores comerciales con soporte para RNC dominicano y desactivación lógica (commit: d2227ad)
- Fase 5: Compras a proveedor con incremento de stock y costeo "último costo" (commit: 83b13ba)

## Fase actual
Descripción: Sistema dedicado de alertas y visualización de productos en o por debajo de su cantidad mínima de stock para reabastecimiento oportuno.
Objetivo: Pantalla centralizada de alertas de stock bajo con cálculo de unidades faltantes para nivel óptimo, indicador visual en el panel principal (MainForm) para roles con permiso (`AlertasStock.Ver`), y acceso directo a generar orden de compra a proveedor.
Definition of Ready (qué debía estar resuelto antes de empezar esta fase):
- [x] Fase 3 (Productos con `StockActual` y `CantidadMinima`) y Fase 5 (Compras) COMPLETADAS.
- [x] Permiso `AlertasStock.Ver` registrado en catálogo canónico de RBAC.
Tareas completadas:
- [x] Consulta base `ObtenerProductosBajoStockAsync` implementada en `IProductoService`.
Tareas pendientes:
- [ ] Implementación de `IAlertaStockService` / métodos de análisis de reabastecimiento (unidades sugeridas a pedir).
- [ ] Formulario WinForms `AlertasStockForm` con vista de productos críticos y botón de compra directa a proveedor.
- [ ] Notificador / Badge de alerta visual en el panel principal de `MainForm.cs`.
- [ ] Pruebas unitarias para detección y cálculo de requerimiento de stock.

## Próximo paso
Confirmar con el usuario el inicio del desarrollo de la Fase 6 (Alertas de stock mínimo), implementar la pantalla de alertas y el enlace directo con compras.

## Verificación de la última sesión
Build: OK (0 advertencias, 0 errores en `SistemaCelulares.sln`)
Tests ejecutados: dotnet test SistemaCelulares.sln
Resultado: 34/34 pasaron (100% de éxito)
Errores pendientes: Ninguno

## Decisiones arquitectónicas
Ver `docs/DECISIONS.md` — DEC-001 (SQLite), DEC-002 (EF Core), DEC-003 (Localización RD), DEC-004 (RBAC y BCrypt), DEC-005 (Generación de SKU), DEC-006 (Método de costeo: último costo).

## Base de datos
Entidades creadas: `Usuario`, `Rol`, `Permiso`, `RolPermiso`, `Turno`, `Categoria`, `Producto`, `Proveedor`, `Compra`, `DetalleCompra`
Última migración: Esquema con compras a proveedor y detalle de transacciones

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
Implementación completa de la Fase 5 (Compras a proveedores con actualización automática de stock, costeo "último costo", historial de compras y 34 pruebas unitarias al 100%).

## Notas importantes
- Moneda por defecto: Peso Dominicano (RD$ / DOP) con formato regional `es-DO`.
- Las compras incrementan el inventario y actualizan el precio de costo del producto ("último costo"), sin modificar el precio de venta al público.
