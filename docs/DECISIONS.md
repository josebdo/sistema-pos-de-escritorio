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

## DEC-005
- **Estado**: VIGENTE
- **Decisión**: Generación automática de SKU basada en prefijo de categoría (ej. `CEL-0001`, `ACC-0001`) con soporte para personalización manual e índice único en base de datos.
- **Motivo**: Agiliza la creación de productos evitando errores humanos de codificación, a la vez que garantiza la unicidad e integridad del inventario.
- **Fecha**: 2026-09-20

## DEC-006
- **Estado**: VIGENTE
- **Decisión**: Método de costeo de inventario por "último costo". Al registrar una compra a proveedor, el precio de costo del producto existente se actualiza con el costo unitario de la compra más reciente, incrementando el stock sin alterar el precio de venta automáticamente.
- **Motivo**: Método simple, directo e idóneo para tiendas comerciales de celulares y retail en caja única, preservando los márgenes sin modificaciones sorpresivas de precios al público.
- **Fecha**: 2026-09-20

## DEC-007
- **Estado**: VIGENTE
- **Decisión**: Estándar de código de barras **EAN-13** (GS1) para productos del inventario. Para productos propios o sin código de fábrica, se utiliza el rango de uso interno/restringido reservado universalmente por GS1 (prefijo `20` a `29`), calculando el 13.° dígito verificador mediante el algoritmo canónico **Modulo 10** (ponderación 1 y 3 alternada).
- **Motivo**: Garantiza compatibilidad universal con lectores ópticos USB estándar y terminales de punto de venta en República Dominicana, evitando colisiones con códigos de barras comerciales emitidos por fabricantes externos.
- **Fecha**: 2026-09-20

## DEC-008
- **Estado**: VIGENTE
- **Decisión**: Modelo integral de métodos de pago (`Efectivo`, `Transferencia`, `TarjetaDebito`, `TarjetaCredito`, `PagoMixto`) con reglas de negocio específicas:
  1. **Efectivo**: Cálculo en tiempo real de devuelta/vuelto en pesos dominicanos (RD$) (`MontoEntregado - MontoTotal`), validando montos suficientes.
  2. **Transferencia Bancaria dominicana**: Verificación manual por el cajero (`EsVerificado`). Si la transferencia no ha sido constatada en cuenta bancaria (Banreservas, Banco Popular, BHD, etc.), el pago queda en estado `PendienteVerificacion` y no computa en el arqueo de caja líquida hasta su aprobación.
  3. **Tarjeta (Débito/Crédito)**: Registro de modalidad y captura opcional de número de autorización/referencia del datáfono externo (Cardnet, Azul, Visanet) sin integración directa de hardware en primera fase.
  4. **Pagos Mixtos**: Validación estricta de que los importes parciales cubran el total exacto de la operación.
- **Motivo**: Flexibilidad operativa en punto de venta y control riguroso de cuadre de caja conforme a las prácticas comerciales de República Dominicana.
- **Fecha**: 2026-09-20

## DEC-009
- **Estado**: VIGENTE
- **Decisión**: Arquitectura de Conmutación de Modo Caja Única / Multi-Caja y Migración de Datos.
  1. **Topología de red**:
     - `CajaUnicaLocal`: Instancia autónoma usando SQLite local (`sistema_celulares.db`) con Caja ID = 1.
     - `ServidorCentral`: PC principal que aloja la base de datos centralizada en la red local (LAN) y asigna turnos y ventas a cada caja.
     - `CajaClienteLan`: Terminal punto de venta en red local conectada a la IP/Host del Servidor Central con identificación única (`CajaId`, `NombreCaja`).
  2. **Control exclusivo**:
     - Únicamente el **Super Admin** puede conmutar el modo de operación y configurar los parámetros de red.
  3. **Migración de datos**:
     - Implementación de `IDataMigrationService` y `DataMigrationService` para generar instantáneas integrales (`SnapshotTiendaDto`) de la base de datos en JSON criptográficamente validado (SHA-256) e importarlas en la nueva base centralizada preservando llaves, historial de turnos, inventario, finanzas y compras.
  4. **Tolerancia y Diagnóstico**:
     - Verificación activa de conectividad (`ProbarConexionAsync`) con reintentos controlados para evitar caídas abruptas del aplicativo ante fallas de red local.
- **Motivo**: Permitir la expansión de negocios desde 1 sola PC a múltiples cajas simultáneas sin perder datos ni requerir reinstalación manual.
- **Fecha**: 2026-09-20

## DEC-010
- **Estado**: VIGENTE
- **Decisión**: Arquitectura de Ventas, Facturación y Control de Comprobantes Fiscales (NCF) DGII.
  1. **Comprobantes Fiscales Dominicanos**:
     - Soporte para series oficiales tipo B: `B01` (Crédito Fiscal), `B02` (Consumo), `B14` (Regímenes Especiales), `B15` (Gubernamental) y Sin NCF (Ticket de Venta Directo).
     - Formato estándar de 11 caracteres: `B` + 2 dígitos de tipo + 8 dígitos correlativos con ceros a la izquierda (ej: `B0200000001`).
     - Control de rangos autorizados por la DGII (`SecuenciaActual`, `SecuenciaHasta`, `FechaVencimiento`).
     - Validación obligatoria de RNC/Cédula y Razón Social del cliente en facturas B01.
  2. **Control Atómico de Inventario**:
     - Descuento inmediato de `StockActual` al confirmar la venta dentro de una transacción EF Core. Se bloquea la venta ante insuficiencia de stock.
  3. **Cálculo de Impuestos (ITBIS)**:
     - Tasa de ITBIS del 18% para República Dominicana con desglose transparente en tickets: Subtotal Neto, ITBIS (18%) y Total en RD$.
  4. **Trazabilidad de Turno y Cobro**:
     - Requiere turno de caja activo (`TurnoId`), registra la sesión del cajero y vincula el cobro a través del servicio `IPagoService`.
- **Motivo**: Cumplimiento de la normativa fiscal de la DGII de República Dominicana y control riguroso de inventario en punto de venta.
- **Fecha**: 2026-09-20

## DEC-011
- **Estado**: VIGENTE
- **Decisión**: Módulo de Clientes, Búsqueda Ágil y Descuentos para Clientes Frecuentes.
  1. **Gestión Integral de Clientes**:
     - Entidad `Cliente` con nombre completo, teléfono, email, cédula/RNC, dirección y notas.
     - Posibilidad de venta a "Cliente Ocasional / Contado" sin registro forzoso.
  2. **Búsqueda Ágil y Creación Rápida**:
     - Búsqueda en punto de venta y taller por número de teléfono o nombre.
     - Botón de creación rápida en modal para no interrumpir el flujo de venta o recepción.
  3. **Fidelización y Descuentos**:
     - Flag `EsFrecuente` con `PorcentajeDescuento` definido por cliente, calculando automáticamente el descuento sugerido en punto de venta.
  4. **Historial Centralizado**:
     - Consulta unificada de historial de compras y órdenes de reparación por cliente.
- **Motivo**: Requisito clave del negocio de celulares para fidelización y trazabilidad de clientes en ventas y servicios técnicos.
- **Fecha**: 2026-09-21

## DEC-012
- **Estado**: VIGENTE
- **Decisión**: Modelo de Inventario Mixto (Artículos Seriados con IMEI vs Artículos por Cantidad).
  1. **Trazabilidad Unitaria por IMEI**:
     - Los teléfonos móviles (`RequiereSerie = true`) se rastrean individualmente mediante la entidad `UnidadProducto` con índice único para `Imei`.
     - Ciclo de vida de cada unidad: `EnStock` $\rightarrow$ `Vendido` $\rightarrow$ `EnGarantia` / `Devuelto`.
  2. **Artículos por Cantidad**:
     - Accesorios, papelería y repuestos se administran mediante control de stock numérico estándar (`RequiereSerie = false`).
  3. **Compras de Celulares**:
     - Al comprar celulares a proveedores, se ingresan los IMEIs individuales correspondientes a las unidades recibidas, creándolas automáticamente en estado `EnStock`.
  4. **Ventas y Garantías**:
     - Al vender un celular, se exige la selección o escaneo del IMEI disponible, vinculando `UnidadProductoId` y `Imei` en `DetalleVenta` y actualizando el estado de la unidad a `Vendido`.
     - En caso de anulación de venta, la unidad revierte a estado `EnStock`.
- **Motivo**: Control indispensable para la venta de celulares, garantías y prevención de pérdidas de equipos de alto valor.
- **Fecha**: 2026-09-21

## DEC-013
- **Estado**: VIGENTE
- **Decisión**: Módulo de Taller y Reparaciones de Celulares con Flujo Simple e Integración de Caja.
  1. **Flujo de Estados Simplificado**:
     - Estados de la orden: `EnReparacion` $\rightarrow$ `ListaParaEntrega` $\rightarrow$ `Entregada` (o `Cancelada`).
  2. **Recepción de Equipo**:
     - Registro de cliente, marca, modelo, IMEI/serie, falla reportada, precio estimado y notas iniciales.
     - Generación correlativa de código de orden (`REP-YYYY-XXXX`) y emisión de comprobante de recepción para el cliente.
  3. **Cobro y Entrega Integrada**:
     - Al entregar el equipo listo, se procesa el cobro a través de `IPagoService` registrando el ingreso en el turno de caja abierto (Efectivo, Tarjeta, Transferencia o Mixto).
     - Bloqueo de entrega si el pago no es completado o no coincide con el precio final acordado.
- **Motivo**: Servicio técnico organizado con cobranza controlada dentro de los turnos de caja de la tienda.
- **Fecha**: 2026-09-21

