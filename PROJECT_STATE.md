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
Fase: Fase 2: Apertura y cierre de caja (turno)
Estado: EN PROGRESO
Último commit verificado: c116627
Última actividad: Culminación de la Fase 1 (Roles y permisos) con build y 11 tests unitarios pasando.
Fecha: 2026-09-20

## Fases completadas
- Fase 0: Inicialización del repositorio, reglas canónicas y arquitectura base (commit: c116627)
- Fase 1: Sistema de roles y permisos (RBAC), seguridad BCrypt y formularios WinForms (commit: c116627)

## Fase actual
Descripción: Apertura y cierre de caja (turno) para control de flujo de efectivo por cajero y arqueo de diferencias.
Objetivo: Obligar la apertura de turno antes de operar ventas, registrar monto inicial, calcular efectivo esperado y registrar arqueo al cierre con cálculo de diferencias.
Definition of Ready (qué debía estar resuelto antes de empezar esta fase):
- [x] Fase 1 (Roles y permisos) COMPLETADA y verificada.
- [x] Usuario autenticado disponible en la sesión (`SesionUsuario`).
- [x] Permisos `Turnos.Abrir`, `Turnos.Cerrar` y `Turnos.Historial` registrados en el catálogo.
Tareas completadas:
- [x] Catálogo de permisos de turnos listo en `PermisosConstantes`.
Tareas pendientes:
- [ ] Modelado de entidad `Turno` (o `CajaTurno`) en `Core`.
- [ ] Configuración en `AppDbContext` y servicio `ITurnoService` / `TurnoService`.
- [ ] Validación de bloqueo de ventas si no hay turno abierto.
- [ ] Formularios WinForms: `AbrirTurnoForm`, `CerrarTurnoForm` (arqueo y cálculo de diferencia) e `HistorialTurnosForm`.
- [ ] Pruebas unitarias para apertura, cálculo de saldo esperado, cierre y diferencias.

## Próximo paso
Confirmar con el usuario el inicio del desarrollo de la Fase 2 (Apertura y cierre de caja / Turnos), modelar la entidad `Turno` y construir la lógica de apertura, cálculo de efectivo y arqueo de cierre.

## Verificación de la última sesión
Build: OK (0 advertencias, 0 errores en `SistemaCelulares.sln`)
Tests ejecutados: dotnet test SistemaCelulares.sln
Resultado: 11/11 pasaron (100% de éxito)
Errores pendientes: Ninguno

## Decisiones arquitectónicas
Ver `docs/DECISIONS.md` — DEC-001 (SQLite), DEC-002 (EF Core), DEC-003 (Localización RD), DEC-004 (RBAC y BCrypt).

## Base de datos
Entidades creadas: `Usuario`, `Rol`, `Permiso`, `RolPermiso`
Última migración: Esquema inicial con Seed Data (Super Admin, Admin, Cajero)

## Problemas conocidos
Ninguno.

## Dependencias
Librerías instaladas:
- Microsoft.EntityFrameworkCore.Sqlite (8.0.13) - Persistencia en SQLite
- Microsoft.EntityFrameworkCore.Design (8.0.13) - Herramientas de EF Core
- BCrypt.Net-Next (4.0.3) - Hashing seguro de contraseñas
- Microsoft.Extensions.DependencyInjection (8.0.1) - Inyección de dependencias
- Microsoft.EntityFrameworkCore.InMemory (8.0.13) - Aislamiento en tests unitarios

## Cambios recientes
Implementación completa de la Fase 1 (Roles y Permisos, login BCrypt, cambio obligatorio de contraseña temporal, gestión de usuarios/roles y pruebas unitarias al 100%).

## Notas importantes
- Moneda por defecto: Peso Dominicano (RD$ / DOP) con formato regional `es-DO`.
- El Super Admin tiene credenciales por instalación y puede resetear claves de cualquier usuario.
- Toda contraseña inicial o reseteada activa `DebeCambiarPassword = true`.
- Los roles fijos (Super Admin, Admin, Cajero) no pueden eliminarse ni cambiar de nombre.
