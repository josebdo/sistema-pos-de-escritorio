# DECISIONS

## DEC-001
- **Estado**: VIGENTE
- **Decisión**: Utilizar SQLite como base de datos inicial para modo caja única offline.
- **Motivo**: La primera instalación será para una sola PC de escritorio y debe ser 100% autónoma y funcional sin conexión a internet.
- **Fecha**: 2026-09-20

## DEC-002
- **Estado**: VIGENTE
- **Decisión**: Utilizar Entity Framework Core como ORM con arquitectura en capas (Core, Infrastructure, App, Tests).
- **Motivo**: Desacoplar la lógica de negocio y las entidades del motor de base de datos específico para posibilitar una futura migración transparente a motores cliente-servidor (Multi-caja).
- **Fecha**: 2026-09-20

## DEC-003
- **Estado**: VIGENTE
- **Decisión**: Localización para República Dominicana (Moneda DOP / RD$, ITBIS 18% configurable, formato regional es-DO).
- **Motivo**: Cumplimiento del contexto comercial y fiscal especificado en las reglas del proyecto.
- **Fecha**: 2026-09-20

## DEC-004
- **Estado**: VIGENTE
- **Decisión**: Catálogo de permisos cerrado para el sistema RBAC y almacenamiento seguro de contraseñas con BCrypt.
- **Motivo**: Seguridad robusta, evitando contraseñas en texto plano y asegurando que los roles personalizados solo puedan recibir permisos válidos del sistema.
- **Fecha**: 2026-09-20
