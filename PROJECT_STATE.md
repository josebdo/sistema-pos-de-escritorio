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
Fase: Fase 8: Código de barras — lectura
Estado: LISTO PARA INICIAR (Esperando confirmación)
Último commit verificado: Fase 7 completada
Última actividad: Culminación de la Fase 7 (Gastos e ingresos del negocio) con build y 42 tests unitarios pasando al 100%.
Fecha: 2026-09-20

## Fases completadas
- Fase 0: Inicialización del repositorio, reglas canónicas y arquitectura base (commit: c116627)
- Fase 1: Sistema de roles y permisos (RBAC), seguridad BCrypt y formularios WinForms (commit: c116627)
- Fase 2: Apertura y cierre de caja (turnos), arqueo y cálculo de diferencias (commit: 5013c71)
- Fase 3: Productos, categorías y SKU con generación automática e índice único (commit: 5095c48)
- Fase 4: Proveedores comerciales con soporte para RNC dominicano y desactivación lógica (commit: d2227ad)
- Fase 5: Compras a proveedor con incremento de stock y costeo "último costo" (commit: 83b13ba)
- Fase 6: Alertas de stock mínimo, criticidad y análisis de reposición (commit: 57daa88)
- Fase 7: Gastos e ingresos del negocio, balance neto consolidado en RD$ (commit pendiente)

## Próxima fase
Fase: Fase 8: Código de barras — lectura
Descripción: Lectura de códigos de barras mediante lectores USB (modo emulación de teclado con sufijo Enter) para identificar productos instantáneamente en ventas, compras e inventario.
Objetivo:
- Servicio o helper de captura de escaneo (`BarcodeScannerListener` / `IBusquedaProductoService`).
- Detección de producto por código de barras o SKU en milisegundos.
- Integración en formularios de búsqueda rápida y selección de producto.
- Manejo de productos no encontrados con opción de registro rápido.

## Verificación de la última sesión
Build: OK (0 advertencias, 0 errores en `SistemaCelulares.sln`)
Tests ejecutados: dotnet test SistemaCelulares.sln
Resultado: 42/42 pasaron (100% de éxito)
Errores pendientes: Ninguno

## Decisiones arquitectónicas
Ver `docs/DECISIONS.md` — DEC-001 a DEC-006.

## Base de datos
Entidades creadas: `Usuario`, `Rol`, `Permiso`, `RolPermiso`, `Turno`, `Categoria`, `Producto`, `Proveedor`, `Compra`, `DetalleCompra`, `CategoriaFinanciera`, `MovimientoFinanciero`
Última migración: Esquema completo con finanzas, inventario, turnos, compras y proveedores

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
Implementación completa de la Fase 7 (Control financiero con registro de gastos operativos e ingresos adicionales, consolidación de compras a proveedores, cálculo de balance neto y 42 pruebas unitarias al 100%).

## Notas importantes
- Moneda por defecto: Peso Dominicano (RD$ / DOP) con formato regional `es-DO`.
- Solo los roles autorizados (Admin, Super Admin o con permiso explícito `Finanzas.ReportesVer`) pueden visualizar la información financiera sensible.
