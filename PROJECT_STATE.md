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
Fase: Fase 12: Ventas, Facturación y Comprobantes Fiscales (NCF)
Estado: COMPLETADA
Último commit verificado: 1116067
Última actividad: Corrección de docking/z-order de MainForm y verificación integral de UI (85 tests pasando al 100%).
Fecha: 2026-09-20

## Fases completadas
- Fase 0: Inicialización del repositorio, reglas canónicas y arquitectura base (commit: c116627)
- Fase 1: Sistema de roles y permisos (RBAC), seguridad BCrypt y formularios WinForms (commit: c116627)
- Fase 2: Apertura y cierre de caja (turnos), arqueo y cálculo de diferencias (commit: 5013c71)
- Fase 3: Productos, categorías y SKU con generación automática e índice único (commit: 5095c48)
- Fase 4: Proveedores comerciales con soporte para RNC dominicano y desactivación lógica (commit: d2227ad)
- Fase 5: Compras a proveedor con incremento de stock y costeo "último costo" (commit: 83b13ba)
- Fase 6: Alertas de stock mínimo, criticidad y análisis de reposición (commit: 57daa88)
- Fase 7: Gastos e ingresos del negocio, balance neto consolidado en RD$ (commit: 325f361)
- Fase 8: Código de barras — lectura, helper de escaneo USB y verificador de precios (commit: c29e063)
- Fase 9: Código de barras — generación EAN-13 GS1 (prefijos 20-29) e impresión de etiquetas térmicas (commit: 37c8038)
- Fase 10: Métodos de pago (Efectivo con devuelta en RD$, Transferencia verificada, Tarjeta POS y Pagos Mixtos) (commit: 4aae986)
- Fase 11: Modo caja única / Multi-caja (conmutación LAN, snapshots criptográficos SHA-256 y gestión para Super Admin) (commit: 1c441be)
- Fase 12: Ventas, Facturación comercial y Comprobantes Fiscales NCF DGII (B01, B02, B14, B15), emisión de tickets térmicos e integración de inventario (commit: 990f9a8)

## Próximo paso
El sistema de escritorio cuenta con todas sus fases troncales y requisitos de localización de República Dominicana (NCF, ITBIS 18%, DOP/RD$, RBAC, Turnos, Inventario, Código de Barras EAN-13, Métodos de Pago, Multi-Caja y Facturación POS) completamente implementadas, compilando con 0 advertencias, 0 errores y 85 pruebas unitarias automatizadas aprobadas al 100%.
Próximos pasos opcionales a solicitud del usuario:
- Empaquetado o publicación de instalador de escritorio autónomo (Single File Publish / Inno Setup / MSIX).
- Adición de módulos complementarios (Control de Garantías / Taller de Reparaciones de Celulares con Recepción de Equipos).

## Verificación de la última sesión
Build: OK (0 advertencias, 0 errores en `SistemaCelulares.sln`)
Tests ejecutados: dotnet test SistemaCelulares.sln
Resultado: 85/85 pasaron (100% de éxito)
Errores pendientes: Ninguno

## Decisiones arquitectónicas
Ver `docs/DECISIONS.md` — DEC-001 a DEC-010.

## Base de datos
Entidades creadas: `Usuario`, `Rol`, `Permiso`, `RolPermiso`, `Turno`, `Categoria`, `Producto`, `Proveedor`, `Compra`, `DetalleCompra`, `CategoriaFinanciera`, `MovimientoFinanciero`, `Pago`, `DetallePago`, `Cliente`, `ComprobanteFiscalSecuencia`, `Venta`, `DetalleVenta`
Última migración: Esquema completo con facturación POS, NCF DGII, finanzas, inventario, turnos, compras y proveedores
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
Implementación completa de la Fase 8 (Lectura de códigos de barras USB con supresión de beep, búsqueda unificada por código o SKU, verificador de precios/stock y 47 pruebas unitarias al 100%).

## Notas importantes
- Moneda por defecto: Peso Dominicano (RD$ / DOP) con formato regional `es-DO`.
- Los lectores USB de códigos de barras operan en modo emulación de teclado enviando la secuencia de dígitos seguida de Enter.
