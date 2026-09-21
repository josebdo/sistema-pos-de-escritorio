# PROJECT STATE

## Proyecto
Nombre: Sistema de Escritorio para Tienda de Celulares
Descripción: Aplicación de escritorio en WinForms .NET 8 / C# con SQLite y Entity Framework Core para la gestión de inventario, ventas, compras, caja y facturación con soporte para República Dominicana.

## Stack
- C# 12
- .NET 8.0 (WindowsDesktop WinForms)
- Entity Framework Core 8 / 9 SQLite
- BCrypt.Net-Next (Seguridad / Hashing)
- xUnit (Pruebas unitarias)

## Estado actual
Fase: Fase 1: Sistema de roles y permisos
Estado: EN PROGRESO
Último commit verificado: <sin commit inicial>
Última actividad: Inicialización de la estructura de estado y preparación del plan de implementación.
Fecha: 2026-09-20

## Fases completadas
- Fase 0: Inicialización del repositorio y reglas de persistencia de contexto (commit: inicial)

## Fase actual
Descripción: Sistema de roles y permisos (RBAC) con Super Admin, Admin, Cajero y roles personalizados, login seguro con BCrypt, cambio obligatorio de contraseña temporal y verificación de permisos en UI/acciones.
Objetivo: Proporcionar la base de seguridad y control de acceso para todas las pantallas del sistema.
Definition of Ready (qué debía estar resuelto antes de empezar esta fase):
- [x] DEC-001 (SQLite) y DEC-002 (EF Core) documentados.
- [x] DEC-004 (Catálogo de permisos y BCrypt) documentado.
- [x] Estructura de solución definida y aprobada.
Tareas completadas:
- [x] Inicialización de Git y reglas canónicas.
- [x] Definición de documentación de soporte (`docs/`).
Tareas pendientes:
- [ ] Creación de solución `SistemaCelulares.sln` y proyectos (`Core`, `Infrastructure`, `App`, `Tests`).
- [ ] Modelado de entidades: `Usuario`, `Rol`, `Permiso`, `RolPermiso`.
- [ ] Implementación de `AppDbContext`, migraciones iniciales y seeder (Super Admin, Admin, Cajero, Permisos base).
- [ ] Servicio de autenticación y autorización (`AuthService`, `PasswordHasher`).
- [ ] Vistas WinForms: Login, Cambio de contraseña obligatorio, Gestión de usuarios y Gestión de roles/permisos.
- [ ] Pruebas unitarias automatizadas para hashing, autenticación y verificación de permisos.

## Próximo paso
Crear la solución .NET 8 con los proyectos modulares (`Core`, `Infrastructure`, `App`, `Tests`), configurar EF Core SQLite, modelar las entidades de seguridad y escribir los primeros tests unitarios.

## Verificación de la última sesión
Build: Pendiente de creación de solución
Tests ejecutados: Ninguno todavía
Resultado: 0/0
Errores pendientes: Ninguno

## Decisiones arquitectónicas
Ver `docs/DECISIONS.md` — DEC-001, DEC-002, DEC-003, DEC-004.

## Base de datos
Entidades creadas: Ninguna
Última migración: Ninguna

## Problemas conocidos
Ninguno.

## Dependencias
Librerías a instalar:
- Microsoft.EntityFrameworkCore.Sqlite (EF Core SQLite)
- Microsoft.EntityFrameworkCore.Design (Herramientas de migración)
- BCrypt.Net-Next (Hashing de contraseñas)
- xunit & Microsoft.NET.Test.Sdk (Testing)

## Cambios recientes
Inicialización del repositorio, reglas de persistencia de contexto y estructura de documentación.

## Notas importantes
- Moneda por defecto: Peso Dominicano (RD$ / DOP) con formato regional.
- El Super Admin tiene credenciales por instalación y puede resetear claves de cualquier usuario.
- Toda contraseña inicial o reseteada activa `DebeCambiarPassword = true`.
