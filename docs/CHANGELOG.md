# CHANGELOG

## [0.12.0] - 2026-09-20
- Implementación completa de la Fase 12: Ventas, Facturación comercial y Comprobantes Fiscales (NCF DGII).
- Registro de decisión arquitectónica DEC-010: Arquitectura de Ventas, Facturación y Control de Comprobantes Fiscales (NCF) DGII.
- Creación de entidades `Venta`, `DetalleVenta`, `Cliente`, `ComprobanteFiscalSecuencia` y enums `TipoComprobanteFiscal` (`B01`, `B02`, `B14`, `B15`) y `EstadoVenta`.
- Implementación de `IVentaService` y `VentaService`:
  - Emisión de NCF correlativo autorizado con validación de rangos y fechas de vencimiento.
  - Validación de RNC/Cédula obligatorio en Facturas de Crédito Fiscal (B01).
  - Control de inventario atómico en tiempo real (decremento inmediato de stock).
  - Cálculo de ITBIS del 18% para República Dominicana con desglose neto.
  - Vinculación obligatoria a turno de caja activo e integración con `IPagoService`.
  - Mecanismo de anulación de ventas con reposición automática de stock y cancelación de pagos.
- Creación del helper `TicketRenderer` para renderizado e impresión de tickets térmicos comerciales con desglose fiscal completo.
- Implementación del formulario de Punto de Venta `PuntoVentaForm` con escáner de códigos de barras / SKU, carrito interactivo, cálculo de impuestos, liquidación con `CobroModalForm` e impresión directa (`F12`).
- Implementación de `HistorialVentasForm` para auditoría, consulta y reimpresión de tickets.
- Sembrado inicial en `DbInitializer` de secuencias oficiales de NCF DGII (B02, B01, B14, B15) y cliente genérico Consumidor Final.
- Suite de 7 nuevas pruebas unitarias para ventas, NCF, control de stock y cálculo de ITBIS (total de 85 pruebas pasando al 100%).

## [0.11.0] - 2026-09-20
- Implementación completa de la Fase 11: Modo caja única / Multi-caja.
- Registro de decisión arquitectónica DEC-009: Arquitectura de Conmutación de Modo Caja Única / Multi-Caja y Migración de Datos.
- Creación de modelos de topología de red `ConfiguracionRedDto`, enum `ModoOperacionCaja` (`CajaUnicaLocal`, `ServidorCentral`, `CajaClienteLan`) y servicio `ConfiguracionRedService` con diagnóstico de conectividad LAN vía sockets TCP.
- Creación de DTOs de migración `SnapshotTiendaDto` y servicio `DataMigrationService` con cálculo criptográfico de hash SHA-256 para serializar, exportar e importar datos íntegros entre bases de datos locales y el servidor central sin pérdidas de inventario, finanzas, turnos ni usuarios.
- Creación de la pantalla de configuración de red y topología para Super Admin `ConfiguracionRedMultiCajaForm`, accesible desde `MainForm`.
- Suite de 7 nuevas pruebas unitarias para conmutación de red, validación de hash de integridad y migración de esquemas (total de 78 pruebas pasando al 100%).

## [0.10.0] - 2026-09-20
- Implementación completa de la Fase 10: Métodos de pago.
- Registro de decisión arquitectónica DEC-008: Modelo integral de cobro (`Efectivo`, `Transferencia`, `TarjetaDebito`, `TarjetaCredito`, `PagoMixto`).
- Creación de entidades `Pago` y `DetallePago`, con relación hacia `Usuario` y `Turno`.
- Implementación de `IPagoService` y `PagoService`:
  - Cálculo de devuelta/vuelto en tiempo real en pesos dominicanos (RD$).
  - Validación de pagos en efectivo impidiendo importes insuficientes y actualizando automáticamente el acumulado de efectivo en el turno activo de caja.
  - Flujo de transferencia bancaria dominicana (Banreservas, Banco Popular, BHD, etc.) con comprobante, verificación manual de cajero y estado `PendienteVerificacion`.
  - Distinción de tarjetas Débito / Crédito con número de autorización/referencia de datáfono POS.
  - Soporte de pagos mixtos/fraccionados con validación de cuadre exacto del total.
- Creación de formulario modal interactivo `CobroModalForm` con accesos directos de billetes dominicanos, display de devuelta, validaciones y atajos de teclado (`F10`, `Esc`).
- Suite de 11 nuevas pruebas unitarias para métodos de pago y transferencias (total de 71 pruebas pasando al 100%).

## [0.9.0] - 2026-09-20
- Implementación completa de la Fase 9: Código de barras — generación.
- Registro de decisión arquitectónica DEC-007: Estándar EAN-13, rango de uso interno restringido GS1 (prefijos 20-29), algoritmo Modulo 10 con ponderación 1 y 3 alternada.
- Creación de interfaz `IEan13GeneratorService` y servicio `Ean13GeneratorService` para cálculo de dígito verificador y generación de secuencias numéricas sin colisión.
- Creación de `BarcodeRenderer` nativo (System.Drawing) para renderizado de patrones binarios EAN-13 (Left Guard, Center Guard, Right Guard y tablas A/B/C) sin librerías externas de terceros.
- Creación de formulario `ImprimirEtiquetaModalForm` con vista previa gráfica de código de barras, precio en RD$, selector de tamaño de etiqueta (pequeña 30x20mm, estándar 50x25mm, grande 70x35mm) y selector de copias.
- Integración de generación automática y validación de código de barras en `ProductoModalForm` y botón de impresión en `ProductosForm`.
- Suite de 13 nuevas pruebas unitarias para generación EAN-13 (total de 60 pruebas pasando al 100%).

## [0.8.0] - 2026-09-20
- Implementación completa de la Fase 8: Código de barras — lectura.
- Implementación de método de búsqueda optimizada `BuscarPorCodigoBarrasOSkuAsync` en `IProductoService` y `ProductoService`.
- Creación de `BarcodeScannerHelper` para manejo de lectores físicos USB en modo emulación de teclado con supresión de alerta auditiva de Windows y despacho automático en Enter.
- Creación del formulario `VerificadorPrecioModalForm` (Lector de Códigos de Barras y Verificador de Precios y Stock al Público), con display de precio en RD$, disponibilidad de inventario y opción de registro directo de productos no encontrados con código pre-cargado.
- Integración del escáner en la barra de herramientas de `ProductosForm` y soporte para código de barras inicial en `ProductoModalForm`.
- Suite de 5 nuevas pruebas unitarias para lectura y resolución de código de barras / SKU (total de 47 pruebas pasando al 100%).

## [0.7.0] - 2026-09-20
- Implementación completa de la Fase 7: Gastos e ingresos del negocio.
- Creación de entidades `CategoriaFinanciera` (Gastos e Ingresos) y `MovimientoFinanciero` con soporte para comprobantes y trazabilidad de usuario.
- Implementación de `IFinanzasService` y `FinanzasService` con cálculo de balance neto, consolidación de gastos operativos y compras de inventario a proveedores en pesos dominicanos (RD$).
- Sembrado inicial en `DbInitializer` con categorías financieras dominicanas (Alquiler, Electricidad EDES, Nómina, Telecomunicaciones, Servicio Técnico, Flasheo, etc.).
- Formularios WinForms `FinanzasForm` (panel con KPIs de ingresos, gastos, compras de mercancía y utilidad neta), `RegistrarMovimientoModalForm` y `CategoriasFinancierasModalForm`.
- Integración en la barra de navegación principal `MainForm`.
- Suite de 6 nuevas pruebas unitarias para finanzas (total de 42 pruebas pasando al 100%).

## [0.6.0] - 2026-09-20
- Implementación completa de la Fase 6: Alertas de stock mínimo.
- Creación de `AlertaStockDto` y enumeración `NivelCriticidadStock` (Agotado, Crítico, Bajo) para categorización visual de urgencia.
- Implementación de `IAlertaStockService` y `AlertaStockService` con cálculo automático de unidades faltantes e inversión requerida para reabastecimiento en pesos dominicanos (RD$).
- Implementación de la pantalla `AlertasStockForm` con tarjetas KPI (Agotados, Críticos, Inversión Total) y botón de compra directa a proveedor pre-cargado.
- Integración de acceso a alertas de stock en el panel lateral de `MainForm` para roles con permiso `AlertasStock.Ver`.
- Suite de 2 nuevas pruebas unitarias para alertas de stock y cálculo de reposición (total de 36 pruebas pasando al 100%).

## [0.5.0] - 2026-09-20
- Implementación completa de la Fase 5: Compras a proveedor (actualiza inventario).
- Registro de decisión arquitectónica DEC-006: Método de costeo "último costo" sin alterar el precio de venta automáticamente.
- Creación de entidades `Compra` y `DetalleCompra` con relaciones hacia `Proveedor`, `Usuario` y `Producto`.
- Implementación de `ICompraService` y `CompraService` con transacción que incrementa el stock de cada producto comprado, actualiza su precio de costo al más reciente y preserva el precio de venta.
- Implementación de formularios WinForms `RegistrarCompraForm` e `HistorialComprasForm`.
- Suite de 4 nuevas pruebas unitarias para compras y actualización de costeo (total de 34 pruebas pasando al 100%).

## [0.4.0] - 2026-09-20
- Implementación completa de la Fase 4: Proveedores.
- Creación de la entidad `Proveedor` con campos para razón social, identificación fiscal de República Dominicana (RNC), teléfono, email, dirección, persona de contacto y estado activo.
- Implementación de `IProveedorService` y `ProveedorService` con validación de RNC único e indexación.
- Implementación de formularios WinForms `ProveedoresForm` y `ProveedorModalForm`.
- Desactivación lógica de proveedores para garantizar la trazabilidad en compras existentes.
- Sembrado inicial con 3 distribuidores e importadores clave de celulares y repuestos de República Dominicana.
- Suite de 5 nuevas pruebas unitarias para proveedores (total de 30 pruebas pasando al 100%).

## [0.3.0] - 2026-09-20
- Implementación completa de la Fase 3: Productos, categorías y SKU.
- Creación de entidades `Categoria` y `Producto` con llaves foráneas, índices únicos en `Sku` y `Nombre` de categoría, e índice en `CodigoBarras`.
- Implementación de `ICategoriaService`, `CategoriaService`, `IProductoService` y `ProductoService`.
- Implementación de algoritmo de generación automática de SKU basado en el prefijo de categoría (ej. `CEL-0001`, `ACC-0001`).
- Formularios WinForms `CategoriasForm`, `CategoriaModalForm`, `ProductosForm` y `ProductoModalForm`.
- Suite de 7 nuevas pruebas unitarias para el catálogo (total de 25 pruebas pasando al 100%).

## [0.2.0] - 2026-09-20
- Implementación completa de la Fase 2: Apertura y cierre de caja (turnos de trabajo).
- Creación de la entidad `Turno` con soporte para monto de apertura, ventas acumuladas en efectivo, monto esperado, dinero físico contado (arqueo), cálculo de diferencia y observaciones.
- Implementación de formularios WinForms `AbrirTurnoModalForm`, `CerrarTurnoModalForm` e `HistorialTurnosForm`.
- Suite de 7 nuevas pruebas unitarias para turnos (total de 18 pruebas pasando al 100%).

## [0.1.0] - 2026-09-20
- Creación de solución modular en .NET 8 con WinForms, EF Core y SQLite.
- Implementación de la Fase 1: Sistema de roles y permisos (RBAC).
- Implementación de entidades `Usuario`, `Rol`, `Permiso` y `RolPermiso`.
- Formularios WinForms de autenticación y gestión de usuarios.
- Suite de 11 pruebas unitarias automatizadas en xUnit (todas pasando al 100%).
