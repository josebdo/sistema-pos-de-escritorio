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
Fase: Fase 9: Código de barras — generación
Estado: LISTO PARA INICIAR (Esperando confirmación)
Último commit verificado: c29e063
Última actividad: Culminación de la Fase 8 (Código de barras — lectura) con build y 47 tests unitarios pasando al 100%.
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

## Próxima fase
Fase: Fase 9: Código de barras — generación
Descripción: Generación automática de códigos de barras estándar EAN-13 para productos propios o sin código de fábrica, utilizando el rango de uso interno restringido GS1 (prefijo 20-29), cálculo de checksum y renderizado gráfico / impresión de etiquetas.
Objetivo:
- Registrar decisión arquitectónica DEC-007 (EAN-13, rango GS1 20-29, algoritmo Modulo 10).
- Servicio `IEan13GeneratorService` para cálculo de dígito verificador y generación de secuencias sin colisión.
- Generador de imagen/renderizado de código de barras (System.Drawing Bitmap) para vista previa e impresión de etiquetas con nombre de producto y precio RD$.
- Integración en `ProductoModalForm` y botón de impresión en `ProductosForm`.

## Verificación de la última sesión
Build: OK (0 advertencias, 0 errores en `SistemaCelulares.sln`)
Tests ejecutados: dotnet test SistemaCelulares.sln
Resultado: 47/47 pasaron (100% de éxito)
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
Implementación completa de la Fase 8 (Lectura de códigos de barras USB con supresión de beep, búsqueda unificada por código o SKU, verificador de precios/stock y 47 pruebas unitarias al 100%).

## Notas importantes
- Moneda por defecto: Peso Dominicano (RD$ / DOP) con formato regional `es-DO`.
- Los lectores USB de códigos de barras operan en modo emulación de teclado enviando la secuencia de dígitos seguida de Enter.
