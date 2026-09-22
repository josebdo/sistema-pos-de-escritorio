# PROJECT STATE

## Proyecto
Nombre: Sistema de Escritorio para Tienda de Celulares
Descripción: Aplicación de escritorio en WinForms .NET 8 / C# con SQLite y Entity Framework Core para la gestión de inventario mixto (IMEI / series vs artículos por cantidad), ventas POS, compras a proveedores, apertura y cierre de turnos de caja, finanzas, código de barras EAN-13, comprobantes fiscales DGII (NCF), fidelización de clientes y taller de reparaciones con localización para República Dominicana.

## Stack
- C# 12
- .NET 8.0 (WindowsDesktop WinForms)
- Entity Framework Core 8.0 (SQLite)
- BCrypt.Net-Next 4.0.3 (Hashing de contraseñas)
- xUnit 2.5 (Pruebas unitarias)

## Estado actual
Fase: Fase 17: Separación Modular de Productos (Catálogo) e Inventario (Stock y Compras)
Estado: COMPLETADA
Último commit verificado: 1116067
Última actividad: Separación total de las opciones de menú y formularios entre '🏷️ Productos' (Catálogo, precios, categorías, EAN-13) y '📦 Inventario' (Entradas por compra a suplidor, existencias físicas, trazabilidad IMEI, alertas de stock mínimo y valorización en RD$) con 96/96 tests pasando al 100%.
Fecha: 2026-09-21

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
- Fase 13: Clientes (CRUD, cliente frecuente, historial), Inventario Mixto por IMEI (`UnidadProducto`, trazabilidad seriada) y Taller de Reparaciones de Celulares (flujo simple de recepción a cobranza/entrega).
- Fase 14: Rediseño visual WinForms según mockups HTML canónicos (paleta limpia, header de ventana, sidebar con avatares circulares y paneles de inicio personalizados para Cajero, Técnico, Admin Dueño y Super Admin) y migración automática de SQLite.
- Fase 15: Entradas de Inventario desde Suplidores / Proveedores con aumento de stock principal, actualización automática de costo y precio de venta configurable en el formulario de compra.
- Fase 16: Perfil de usuario ('Mi Perfil') para cambio de nombre y contraseña voluntario para cualquier rol, autocompletado en vivo en POS y corrección de compilación.
- Fase 17: Separación modular de Productos (catálogo) e Inventario (control de stock, entradas a suplidor, alertas y valorización).

## Próximo paso
El sistema cubre el 100% de los requerimientos y reglas canónicas actualizadas en `PROJECT_CONTEXT_RULES.md`.

## Verificación de la última sesión
Build: OK (0 advertencias, 0 errores en `SistemaCelulares.sln`)
Tests ejecutados: dotnet test SistemaCelulares.sln
Resultado: 96/96 pasaron (100% de éxito)
Errores pendientes: Ninguno

## Decisiones arquitectónicas
Ver `docs/DECISIONS.md` — DEC-001 a DEC-013.

## Base de datos
Entidades creadas: `Usuario`, `Rol`, `Permiso`, `RolPermiso`, `Turno`, `Categoria`, `Producto`, `UnidadProducto`, `Proveedor`, `Compra`, `DetalleCompra`, `CategoriaFinanciera`, `MovimientoFinanciero`, `Pago`, `DetallePago`, `Cliente`, `ComprobanteFiscalSecuencia`, `Venta`, `DetalleVenta`, `OrdenReparacion`
Última migración: Esquema completo con taller de reparaciones, unidades IMEI, clientes, facturación POS, NCF DGII, finanzas, inventario, turnos, compras y proveedores.

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
Implementación de 'Mi Perfil y Seguridad' (cambio de nombre y contraseña voluntario para todo usuario), autocompletado predictivo en vivo en POS y corrección de compilación con 96 pruebas unitarias al 100%.

## Notas importantes
- Cualquier usuario puede cambiar su propio nombre completo y su contraseña (requiriendo ingresar la actual).
- El correo electrónico y el nombre de usuario (login) solo pueden ser modificados por el Admin y Super Admin en la gestión de usuarios.
- En POS, al escribir el nombre o SKU de un producto se despliega una lista desplegable flotante con las coincidencias en tiempo real, permitiendo seleccionar con el ratón o flechas arriba/abajo + Enter para agregar al carrito inmediatamente.
- Para productos con `RequiereSerie = true` (celulares), el stock se calcula dinámicamente según la cantidad de unidades en estado `EnStock`.
- Al registrar compras de celulares, se ingresan los IMEI correspondientes creando registros `UnidadProducto` disponibles.
- Al registrar compras a proveedores, el precio de costo se actualiza automáticamente al último costo de factura y el usuario puede opcionalmente ingresar un nuevo precio de venta para actualizar el catálogo de forma inmediata.
