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
Fase: Fase 10: Métodos de pago
Estado: LISTO PARA INICIAR (Esperando confirmación)
Último commit verificado: 37c8038
Última actividad: Culminación de la Fase 9 (Código de barras — generación) con build y 60 tests unitarios pasando al 100%.
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

## Próxima fase
Fase: Fase 10: Métodos de pago
Descripción: Implementación de pasarela de cobro multi-método para puntos de venta: Efectivo con cálculo en tiempo real de devuelta/vuelto en RD$, Transferencia Bancaria dominicana (Banreservas, Banco Popular, BHD, etc.) con verificación manual por parte del vendedor y número de referencia obligatorio, y Tarjeta Débito/Crédito con registro de número de autorización del POS / terminal.
Objetivo:
- Registrar decisión arquitectónica DEC-008 para soporte de cobro mixto/fraccionado y validaciones por tipo de método.
- Definir enumeración `MetodoPago` (`Efectivo`, `Transferencia`, `Tarjeta`, `Mixto`) y entidades/DTOs de transacción de pago.
- Crear componente/modal interactivo de cobro (`CobroModalForm`) con desglose en RD$, cálculo automático de vuelto, selección de banco de destino y captura de referencia/autorización.
- Pruebas unitarias para cálculo de vuelto, validaciones de montos y reglas de cobro.

## Verificación de la última sesión
Build: OK (0 advertencias, 0 errores en `SistemaCelulares.sln`)
Tests ejecutados: dotnet test SistemaCelulares.sln
Resultado: 60/60 pasaron (100% de éxito)
Errores pendientes: Ninguno

## Decisiones arquitectónicas
Ver `docs/DECISIONS.md` — DEC-001 a DEC-007.

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
Implementación completa de la Fase 8 (Lectura de códigos de barras USB con supresión de beep, búsqueda unificada por código o SKU, verificador de precios/stock y 47 pruebas unitarias al 100%).

## Notas importantes
- Moneda por defecto: Peso Dominicano (RD$ / DOP) con formato regional `es-DO`.
- Los lectores USB de códigos de barras operan en modo emulación de teclado enviando la secuencia de dígitos seguida de Enter.
