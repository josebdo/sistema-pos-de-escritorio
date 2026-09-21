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
Fase: Fase 3: Productos, categorías y SKU
Estado: EN PROGRESO
Último commit verificado: 5013c71
Última actividad: Culminación de la Fase 2 (Turnos y Caja) con build y 18 tests unitarios pasando al 100%.
Fecha: 2026-09-20

## Fases completadas
- Fase 0: Inicialización del repositorio, reglas canónicas y arquitectura base (commit: c116627)
- Fase 1: Sistema de roles y permisos (RBAC), seguridad BCrypt y formularios WinForms (commit: c116627)
- Fase 2: Apertura y cierre de caja (turnos), arqueo y cálculo de diferencias (commit: 5013c71)

## Fase actual
Descripción: Gestión completa del catálogo de celulares y accesorios organizados por categoría, cada uno con SKU único, precios (costo y venta), stock actual, stock mínimo para alertas y código de barras.
Objetivo: CRUD de categorías y productos, validación de unicidad de SKU, desactivación lógica para trazabilidad y preparación de campos para alertas y código de barras.
Definition of Ready (qué debía estar resuelto antes de empezar esta fase):
- [x] Fase 1 (Roles y permisos) y Fase 2 (Caja y turnos) COMPLETADAS.
- [x] Formato de moneda dominicana configurado (RD$ / DOP).
- [ ] Definición de generación de SKU (confirmar si es automática por prefijo de categoría o manual con validación de unicidad, y registrar en `docs/DECISIONS.md`).
Tareas completadas:
- [x] Permisos `Productos.Ver`, `Productos.Crear`, `Productos.Editar`, `Productos.Desactivar`, `Categorias.Gestionar`, `AlertasStock.Ver` ya registrados en catálogo RBAC.
Tareas pendientes:
- [ ] Registro de decisión arquitectónica sobre SKU en `docs/DECISIONS.md`.
- [ ] Modelado de entidades: `Categoria`, `Producto`.
- [ ] Configuración en `AppDbContext` (índice único en `Sku` y `CodigoBarras`, FKs, precisión decimal).
- [ ] Implementación de `IProductoService` / `ProductoService` e `ICategoriaService` / `CategoriaService`.
- [ ] Formularios WinForms: `CategoriasForm`, `ProductosForm`, `ProductoModalForm`.
- [ ] Pruebas unitarias para validaciones de SKU, precios, stock y desactivación lógica.

## Próximo paso
Confirmar con el usuario el inicio de la Fase 3 (Productos, categorías y SKU) y definir la estrategia de generación de SKU (ej. SKU automático `CAT-0001` con opción de personalización).

## Verificación de la última sesión
Build: OK (0 advertencias, 0 errores en `SistemaCelulares.sln`)
Tests ejecutados: dotnet test SistemaCelulares.sln
Resultado: 18/18 pasaron (100% de éxito)
Errores pendientes: Ninguno

## Decisiones arquitectónicas
Ver `docs/DECISIONS.md` — DEC-001 (SQLite), DEC-002 (EF Core), DEC-003 (Localización RD), DEC-004 (RBAC y BCrypt).

## Base de datos
Entidades creadas: `Usuario`, `Rol`, `Permiso`, `RolPermiso`, `Turno`
Última migración: Esquema con Seed Data y módulo de turnos

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
Implementación completa de la Fase 2 (Apertura y cierre de turnos de caja, arqueo con cálculo de diferencias y 18 tests unitarios pasando al 100%).

## Notas importantes
- Moneda por defecto: Peso Dominicano (RD$ / DOP) con formato regional `es-DO`.
- No se pueden registrar ventas sin tener un turno de caja abierto.
- La diferencia de arqueo se calcula como: `MontoCierre - (MontoApertura + TotalVentasEfectivo)`.
