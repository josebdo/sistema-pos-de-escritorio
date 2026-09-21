# CHANGELOG

## [0.1.0] - 2026-09-20
- Creación de solución modular en .NET 8 con WinForms, EF Core y SQLite.
- Implementación de la Fase 1: Sistema de roles y permisos (RBAC).
- Implementación de entidades `Usuario`, `Rol`, `Permiso` y `RolPermiso`.
- Implementación de catálogo cerrado de permisos de seguridad y de negocio.
- Implementación de sembrado inicial con roles fijos (Super Admin, Admin, Cajero) y usuarios base.
- Implementación de hashing seguro de contraseñas con BCrypt (`BCrypt.Net-Next`).
- Implementación del flujo de cambio de contraseña obligatorio para usuarios nuevos o reseteados.
- Implementación de reseteo de contraseñas por Super Admin / Admin con emisión de contraseña temporal.
- Implementación de pantallas WinForms con tema profesional:
  - `LoginForm`: inicio de sesión y validación de estado de cuenta.
  - `CambiarPasswordObligatorioForm`: diálogo modal de seguridad no omitible.
  - `MainForm`: panel de control con navegación contextual y menú dinámico RBAC.
  - `UsuariosForm` y `UsuarioModalForm`: administración de empleados, credenciales y activación/desactivación.
  - `RolesPermisosForm` y `RolModalForm`: administración de roles personalizados y asignación granular de permisos.
- Implementación de suite de 11 pruebas unitarias automatizadas en xUnit (todas pasando al 100%).
- Configuración de localización para República Dominicana (Moneda RD$ / DOP, formato es-DO).
