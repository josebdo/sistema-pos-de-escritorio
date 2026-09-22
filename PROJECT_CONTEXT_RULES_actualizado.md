PERSISTENCIA DEL CONTEXTO Y CONTINUIDAD DEL PROYECTO

Este proyecto se desarrollará durante múltiples sesiones. Debes asumir que puedo cerrar Antigravity, apagar la computadora y volver a abrir el proyecto otro día.

Por esta razón, el proyecto debe mantener su propio estado y contexto dentro del repositorio. El repositorio (código + archivos de estado + historial de git) es la única fuente de verdad. La memoria de la conversación NUNCA debe considerarse fuente de verdad.

---

## ARCHIVO DE ESTADO DEL PROYECTO

Crea y mantén un archivo:

`PROJECT_STATE.md`

Este archivo es obligatorio y debe mantenerse actualizado al final de cada sesión. Debe ser BREVE — es un snapshot del presente, no un historial. El historial detallado vive en `docs/CHANGELOG.md` y en git; no lo dupliques aquí.

Debe contener como mínimo:

```markdown
# PROJECT STATE

## Proyecto
Nombre:
Descripción:

## Stack
- C#
- .NET
- WinForms
- Entity Framework Core
- SQLite
- demás dependencias aprobadas (ver sección Dependencias)

## Estado actual
Fase:
Estado: COMPLETADA / EN PROGRESO / BLOQUEADA
Último commit verificado: <hash corto>
Última actividad:
Fecha:

## Fases completadas
- Fase 0: ... (commit: <hash>)
- Fase 1: ... (commit: <hash>)

## Fase actual
Descripción:
Objetivo:
Definition of Ready (qué debía estar resuelto antes de empezar esta fase):
Tareas completadas:
Tareas pendientes:

## Próximo paso
Qué debe hacerse exactamente después. Debe ser lo suficientemente concreto para empezar sin releer todo el código.

## Verificación de la última sesión
Build: OK / FALLÓ (detalle)
Tests ejecutados: <comando exacto>
Resultado: X/Y pasaron
Errores pendientes:

## Decisiones arquitectónicas
Ver docs/DECISIONS.md — no dupliques el contenido aquí, solo referencia el último DEC-XXX relevante.

## Base de datos
Entidades creadas:
Última migración: <nombre> (commit: <hash>)

## Problemas conocidos
Lista de problemas pendientes, con fecha en que se detectaron y cuántas sesiones llevan abiertos.

## Dependencias
Librerías instaladas:
Versión:
Motivo:
Licencia:

## Cambios recientes
Resumen de únicamente el último cambio significativo (1–2 líneas). El resto está en CHANGELOG.md.

## Notas importantes
Información que no debe olvidarse (convenciones de nombres, cosas contraintuitivas del dominio, etc.)
```

---

## COMANDOS CANÓNICOS DEL PROYECTO

Crea también:

`docs/COMMANDS.md`

Con los comandos exactos y verificados que deben usarse siempre — nunca improvisados. Como mínimo:

```markdown
# COMMANDS

## Build
dotnet build <ruta al .sln>

## Tests
dotnet test <ruta al .sln>

## Migraciones EF Core
dotnet ef migrations add <Nombre> --project <ruta>
dotnet ef database update --project <ruta>

## Ejecutar la aplicación
dotnet run --project <ruta>
```

Si algún comando cambia (por ejemplo se reestructuran carpetas), actualiza este archivo en la misma sesión.

---

## REGLA AL INICIAR CADA SESIÓN

Cada vez que empieces a trabajar en este proyecto:

1. Lee `PROJECT_STATE.md`.
2. Compara el "Último commit verificado" con el estado actual de git (`git log`, `git status`, `git diff` si hace falta). Si hay commits posteriores no reflejados en el estado, adviértelo antes de continuar.
3. Inspecciona la estructura actual del proyecto.
4. Revisa el código relacionado con la fase actual.
5. Revisa los tests existentes.
6. Ejecuta build y tests usando los comandos de `docs/COMMANDS.md` para verificar el estado real, no el declarado.
7. Determina exactamente dónde quedó el proyecto.
8. NO asumas que una fase está terminada solamente porque `PROJECT_STATE.md` diga que lo está — la palabra final la tiene el código compilando y los tests pasando.
9. Si existe una diferencia entre `PROJECT_STATE.md` y el código real, informa la diferencia explícitamente y corrígela en el archivo antes de continuar.

Antes de escribir código debes mostrarme brevemente, con evidencia y no solo afirmaciones:

```
Fase actual:
Último trabajo realizado (commit):
Verificación realizada: (ej. "ejecuté dotnet test, pasaron 14/14")
Qué está terminado:
Qué falta:
Qué voy a hacer ahora:
```

Después espera mi confirmación cuando corresponda según las reglas de fases.

---

## REGLA AL TERMINAR CADA SESIÓN

Antes de considerar terminada una sesión de trabajo:

1. Ejecuta el build (comando de `docs/COMMANDS.md`).
2. Ejecuta los tests correspondientes.
3. Comprueba errores.
4. Actualiza `PROJECT_STATE.md`, incluyendo el hash del último commit y el resultado real de build/tests.
5. Indica qué archivos fueron modificados.
6. Indica qué quedó pendiente.
7. Indica exactamente cuál debe ser el siguiente paso (concreto, accionable).
8. Poda `PROJECT_STATE.md`: elimina de "Cambios recientes" todo lo que no sea el último cambio; mueve detalles históricos a `docs/CHANGELOG.md` si aún no están ahí.

Nunca dejes `PROJECT_STATE.md` desactualizado ni dejes que crezca indefinidamente — debe seguir siendo legible en menos de un minuto.

---

## REGISTRO DE DECISIONES

Crea también:

`docs/DECISIONS.md`

Aquí debes registrar decisiones arquitectónicas importantes.

```markdown
# DECISIONS

## DEC-001
Estado: VIGENTE
Decisión: Utilizar SQLite como base de datos inicial.
Motivo: La primera instalación será para una sola PC y debe funcionar offline.
Fecha: ...

## DEC-002
Estado: VIGENTE
Decisión: Utilizar EF Core.
Motivo: Separar la aplicación del motor de base de datos y facilitar una futura migración.
Fecha: ...
```

Reglas:
- No elimines decisiones anteriores.
- Si una decisión cambia, registra una nueva decisión (DEC-00N) explicando qué cambió, por qué, y qué partes del proyecto afecta.
- Marca la decisión anterior como `Estado: REEMPLAZADA por DEC-00N` — no la borres, no la dejes como si siguiera vigente.

---

## REGISTRO DE PROGRESO

Crea también:

`docs/CHANGELOG.md`

Utilízalo para registrar cambios relevantes por versión. Este es el único lugar con historial detallado; `PROJECT_STATE.md` no debe repetirlo.

```markdown
# CHANGELOG

## [0.2.0]
- Agregado módulo de productos.
- Agregadas categorías.
- Agregadas validaciones.
- Agregados tests de productos.

## [0.1.0]
- Proyecto inicial.
- Configuración de SQLite.
- Configuración de EF Core.
```

---

## MANEJO DE CONFIGURACIÓN Y SECRETOS

- Nunca commitear cadenas de conexión, credenciales ni rutas absolutas locales.
- Usar `appsettings.Development.json` (o equivalente) excluido de git vía `.gitignore` para configuración local.
- Si se necesita un valor de ejemplo, documentarlo en `appsettings.Example.json` versionado, sin datos reales.

---

## REGLA DE NO PERDER CONTEXTO

No dependas únicamente del historial de conversación de Antigravity.

El código y los archivos del proyecto deben contener suficiente información para poder continuar el desarrollo incluso después de:

- cerrar Antigravity
- reiniciar Windows
- comenzar una nueva sesión
- perder el historial de conversación

El repositorio debe ser la fuente principal de verdad del proyecto.

---

## REGLA DE RECUPERACIÓN

Si al abrir el proyecto no existe `PROJECT_STATE.md`, créalo después de inspeccionar cuidadosamente el proyecto actual.

Si el estado es ambiguo:

NO inventes lo que se hizo anteriormente.

Inspecciona:

- estructura de carpetas
- código
- migraciones
- tests
- configuración
- historial de git disponible (`git log`)

y explícame qué puedes determinar con certeza y qué no.

---

## REGLA ESPECIAL PARA LAS FASES

Una fase solamente puede marcarse como:

**"COMPLETADA"**

cuando:

- el código correspondiente existe
- compila (build verificado, no asumido)
- los tests relevantes pasan (ejecutados, no asumidos)
- se verificaron los requisitos de la fase
- no existen errores conocidos que impidan considerar terminada la fase

Si algo está parcialmente implementado, utilizar:

**"EN PROGRESO"**

Si existe un problema pendiente:

**"BLOQUEADA"**

### Escalamiento de bloqueos

Si una fase permanece en estado `BLOQUEADA` durante más de 2 sesiones consecutivas:

1. Documenta el bloqueo en detalle en "Problemas conocidos" (qué se intentó, por qué falló cada intento).
2. No sigas intentando el mismo enfoque sin variación.
3. Pregúntame explícitamente cómo proceder: cambiar de enfoque, replantear el alcance de la fase, o aceptarlo como deuda técnica documentada.

Nunca marques una fase como completada simplemente porque escribiste el código.

---

## DEFINITION OF READY (antes de iniciar una fase)

Antes de empezar una fase nueva, confirma que:

- Las decisiones arquitectónicas de las que depende ya están registradas en `docs/DECISIONS.md`.
- Las fases previas de las que depende están en estado `COMPLETADA` (no `EN PROGRESO`).
- El objetivo y alcance de la fase están claros; si no lo están, pregúntame antes de escribir código.

---

## LOCALIZACIÓN: REPÚBLICA DOMINICANA

La aplicación se usará principalmente en República Dominicana. Todo lo relacionado con moneda, formato numérico, impuestos y estándares debe tomar RD como referencia por defecto, salvo que se indique lo contrario:

- Moneda: peso dominicano (RD$ / DOP), formato con separador de miles con punto y decimales con coma (ej. `RD$1.250,00`) — confirmar el formato exacto conmigo antes de codificarlo si hay dudas.
- Impuesto: ITBIS (18%) como impuesto a considerar si en algún momento se calculan totales con impuesto — no asumir que aplica automáticamente a todos los productos sin confirmarlo conmigo, ya que hay bienes exentos.
- Estándar de código de barras: EAN-13 / GS1 (ver fase correspondiente más abajo).
- Cualquier otro dato "regional" (formato de fecha, número de teléfono, etc.) debe asumirse en formato dominicano salvo indicación contraria.

Esta sección debe consultarse cada vez que una fase implique formato de moneda, impuestos, o cualquier dato sensible a la región.

---

## DESCRIPCIÓN DEL NEGOCIO

Esta sección describe el negocio real para el que se construye la aplicación. Debe consultarse antes de diseñar cualquier fase, porque varias decisiones de datos y flujo dependen directamente de esto — no es un negocio de un solo rubro.

**El negocio es una tienda de celulares que combina tres líneas distintas bajo un mismo local y un mismo sistema:**

1. **Venta de celulares** — cada unidad se rastrea individualmente por **IMEI/número de serie**, no por cantidad genérica. Cada celular vendido tiene su propia garantía.
2. **Venta de accesorios de celulares** — covers, auriculares, proyectores de pantalla, etc. Estos SÍ se manejan como inventario normal por cantidad (como cualquier producto de la fase de Productos), sin necesidad de número de serie individual.
3. **Reparación de celulares** — un servicio, no un producto con stock. Se recibe el equipo del cliente, se diagnostica/repara, y se cobra al entregarlo. Flujo definido como **simple**: no se requiere un seguimiento de múltiples estados intermedios (diagnosticando, esperando pieza, etc.) — basta con registrar el equipo, el problema, y marcarlo como listo para cobrar y entregar.
4. **Papelería** — venta de artículos de oficina/escolares. Se maneja igual que los accesorios: inventario normal por cantidad, sin particularidades adicionales. Entra dentro del mismo catálogo de Productos, simplemente con sus propias categorías.

**Clientes**: el negocio SÍ necesita registrar datos del cliente (nombre, teléfono) — tanto para reparaciones como para ventas — porque se planea dar descuentos a clientes frecuentes más adelante. Esto implica una fase propia de gestión de clientes, transversal a ventas y reparaciones (ver roadmap).

Esta naturaleza mixta del negocio significa que "Producto" en el sistema no es una sola cosa uniforme: algunos productos requieren rastreo por unidad individual (celulares) y otros no (accesorios, papelería). Ver la fase de Productos actualizada más abajo para el detalle de cómo se modela esto.

---

## ROADMAP DE FUNCIONALIDADES SOLICITADAS

Estas funcionalidades deben desarrollarse como fases independientes, siguiendo estrictamente la "REGLA ESPECIAL PARA LAS FASES" y la "REGLA AL TERMINAR CADA SESIÓN" de este documento. Antes de pasar de una fase a la siguiente, debes preguntarme y esperar mi confirmación explícita — sin excepción, aunque el trabajo parezca trivial.

### Fase: Clientes

Objetivo: registrar los clientes del negocio, ya que tanto ventas como reparaciones necesitan asociarse a un cliente — pensando en el futuro sistema de descuentos para clientes frecuentes.

Alcance mínimo:
- CRUD de clientes: nombre, teléfono (obligatorio, es el dato principal de contacto), email (opcional), fecha de registro.
- Búsqueda rápida de cliente por nombre o teléfono al momento de una venta o una reparación — con opción de "cliente ocasional" (registro rápido con solo el teléfono) para no forzar un formulario largo en medio de una venta.
- Historial consultable: ver todas las compras y reparaciones asociadas a un cliente.
- Campo `EsFrecuente` o similar, y un campo de descuento asociado (ej. `PorcentajeDescuento`), que el Admin pueda asignar manualmente a un cliente — la lógica de "frecuente automático por número de compras" queda fuera del alcance mínimo; por ahora es una marca manual del Admin. Si más adelante quieres que se calcule automático según historial de compras, es una fase aparte a definir conmigo.
- No eliminar clientes con ventas o reparaciones asociadas — desactivar en su lugar.

Definition of Ready: ninguna dependencia estricta, pero debe estar completada antes de las fases de Ventas y Reparaciones, ya que ambas dependen de tener un cliente asociado.

Definition of Done: CRUD probado, búsqueda rápida probada, historial de un cliente de prueba verificado con al menos una compra y una reparación asociadas; build y tests pasando.

### Fase: Productos, categorías y SKU

Objetivo: gestión completa del catálogo de productos — celulares, accesorios y papelería — organizados por categoría, cada uno con un SKU único. **El negocio mezcla dos tipos de inventario y hay que modelarlos distinto desde el inicio:**

- **Productos con serie individual (celulares)**: cada unidad física se rastrea por separado (IMEI/número de serie), con su propia garantía y su propio estado (en stock, vendido, en garantía, etc.). El "stock" de estos productos no es un número — es la cuenta de unidades individuales que están en estado "en stock".
- **Productos sin serie (accesorios, papelería)**: se manejan como inventario normal por cantidad, igual que en cualquier tienda — un solo número de stock por producto.

Modelo de datos sugerido:
- `Producto`: nombre, descripción, categoría (FK), SKU (único, indexado), precio de costo, precio de venta, código de barras, **`RequiereSerie`** (booleano: true para celulares, false para accesorios/papelería), cantidad mínima (para alerta de stock bajo — solo aplica de forma directa a productos sin serie; para productos con serie, la alerta se calcula contando unidades en stock).
- Si `RequiereSerie = true`: **`UnidadProducto`** (o `ProductoSerie`) — una fila por celular físico: `ProductoId` (FK), `IMEI` (único, indexado), `Estado` (En stock / Vendido / En garantía / Devuelto), fecha de ingreso, fecha de venta (nula hasta que se venda), venta asociada (FK, nula hasta que se venda).
- Si `RequiereSerie = false`: el producto usa directamente el campo de stock por cantidad en `Producto`, como ya estaba definido.

Alcance mínimo:
- CRUD de categorías (nombre, descripción) — deben poder diferenciar visualmente celulares de accesorios/papelería, aunque compartan la misma tabla de categorías.
- CRUD de productos, con el campo `RequiereSerie` definido al crear el producto (no editable después de tener unidades registradas, para evitar inconsistencias).
- Si `RequiereSerie = true`: pantalla para agregar unidades individuales (IMEI) a ese producto, ya sea manual o al registrar una compra a proveedor (ver fase de Compras).
- Generación de SKU: definir si es automática (ej. correlativo por categoría) o manual con validación de unicidad — confirmar conmigo y registrar en `docs/DECISIONS.md` antes de codificar.
- No eliminar productos con ventas o compras asociadas — desactivar en su lugar (mismo criterio que empleados: se conserva trazabilidad). Para celulares, tampoco se elimina un `IMEI` ya vendido — queda con estado `Vendido` para trazabilidad de garantía.

Definition of Ready: si la fase de Roles ya está completada, las pantallas deben respetar permisos como `Productos.Editar`; si no, puede construirse igual y conectarse a permisos después.

Definition of Done: CRUD probado tanto para un producto con serie (celular, con al menos 2 IMEI distintos registrados) como para uno sin serie (accesorio); unicidad de SKU e IMEI validada por test; build y tests pasando.

### Fase: Proveedores

Objetivo: gestión de proveedores del negocio — crear, editar y eliminar (desactivar).

Alcance mínimo:
- CRUD de proveedores: nombre, RNC (identificación fiscal dominicana, si aplica), teléfono, email, dirección, contacto.
- No eliminar directamente si el proveedor tiene compras registradas — desactivar en su lugar.

Definition of Ready: ninguna dependencia estricta, pero conviene hacerla junto con o justo después de Productos, ya que la fase de Compras relaciona Proveedor + Producto.

Definition of Done: CRUD probado, build y tests pasando.

### Fase: Compras a proveedor (actualiza inventario)

Objetivo: registrar una compra a un proveedor que incrementa automáticamente el inventario de los productos comprados y actualiza su precio de costo si cambió.

Alcance mínimo:

- Registrar una compra: proveedor, fecha, lista de productos comprados con cantidad y costo unitario.
- Si el producto comprado tiene `RequiereSerie = true` (celulares): en vez de solo indicar una cantidad, se debe capturar el IMEI de cada unidad comprada (una fila por celular), y cada una se agrega como una `UnidadProducto` nueva en estado "En stock".
- Si el producto no requiere serie (accesorios, papelería): funciona como cantidad simple, tal como estaba definido.
- Al confirmar la compra:
  - El stock del producto se incrementa (por cantidad, o por las unidades con IMEI agregadas, según el tipo).
  - Si el costo unitario de esta compra es distinto al que el producto tenía registrado, **se actualiza el precio de costo del producto existente** — es una sola entidad de producto, no se crea un duplicado; el precio refleja la compra más reciente.
  - **Método de costeo definido: "último costo"** (se sustituye el costo anterior por el de la compra más reciente) — es el más simple para un primer sistema. Si en el futuro se necesita costo promedio ponderado o FIFO, eso es una fase separada; esta decisión debe registrarse en `docs/DECISIONS.md`.
  - El **precio de venta NO se actualiza automáticamente** solo porque cambió el costo — eso podría afectar el margen del negocio sin que el Admin se entere. El ajuste del precio de venta queda como una acción manual del Admin en la pantalla de Productos, salvo que me confirmes que prefieres que se recalcule automáticamente con un margen fijo (en cuyo caso hay que definir ese margen).
- Historial de compras consultable por proveedor y por producto.

Definition of Ready: fases de Productos y Proveedores completadas.

Definition of Done: probado registrando una compra de un producto sin serie (verificando cambio de stock y costo) y una compra de un producto con serie (verificando que se agregan las unidades con su IMEI correctamente); build y tests pasando.

### Fase: Alertas de stock mínimo

Objetivo: avisar cuando el inventario de un producto llega o cae por debajo de su cantidad mínima definida.

Alcance mínimo:
- Cuando el stock de un producto llega o cae por debajo de su cantidad mínima (por una venta, o por cualquier ajuste), la app debe mostrar una alerta visible: un indicador en el listado de productos y/o una pantalla de "productos con stock bajo".
- Esta alerta debe ser visible para roles con permiso de gestión de inventario (Admin, Super Admin, y roles personalizados a los que se les dé ese permiso).

Definition of Ready: fase de Productos completada (necesita el campo de cantidad mínima y el mecanismo de control de stock).

Definition of Done: probado bajando el stock de un producto por debajo del mínimo y verificando que la alerta aparece; build y tests pasando.

### Fase: Reparaciones de celulares

Objetivo: registrar el servicio de reparación de celulares como un flujo propio, distinto de la venta de productos — con flujo **simple**, confirmado contigo: sin seguimiento de múltiples estados intermedios.

Alcance mínimo:
- Registrar una orden de reparación: cliente (FK a la fase de Clientes — obligatorio, ya que confirmaste que siempre se registra), marca y modelo del equipo, IMEI del equipo (texto libre — es el equipo del cliente, no inventario de la tienda, así que no se valida contra `UnidadProducto`), descripción del problema reportado, precio acordado (puede ajustarse al momento de entregar si el precio final difiere del estimado inicial), fecha de recepción.
- Estado de la orden, simplificado a lo esencial para poder cobrar y entregar: **En reparación** → **Lista para entrega** → **Entregada**. No se contemplan sub-estados (diagnosticando, esperando pieza, etc.) en esta fase — si más adelante quieres agregarlos, es una extensión simple de este mismo modelo.
- Al marcar una orden como "Entregada", se registra el cobro (usando los métodos de pago ya definidos: efectivo, transferencia, tarjeta) y la fecha de entrega.
- Listado de órdenes filtrable por estado (para ver rápido qué reparaciones están pendientes de entregar) y por cliente.
- **Fuera de alcance de esta fase, a propósito** (para no romper el flujo simple que pediste): descuento automático de piezas de inventario usadas en la reparación, y sub-estados intermedios de diagnóstico. Si en el futuro los necesitas, se agregan como una fase separada sin rehacer lo ya construido.

Definition of Ready: fases de Clientes y Roles y permisos completadas.

Definition of Done: probado con al menos una orden completa (recibida → lista → entregada con cobro registrado); build y tests pasando.

### Fase: Gastos e ingresos del negocio

Objetivo: que el Admin pueda registrar movimientos financieros del negocio que no son ventas — gastos fijos (luz, internet, alquiler, etc.) y otros ingresos no relacionados a una venta — para tener una visión completa de las finanzas, no solo lo que entra por caja.

Alcance mínimo:
- Registrar un "Movimiento financiero" con: tipo (Gasto / Ingreso), categoría (ej. Luz, Internet, Alquiler, Salarios, Otro — lista editable por el Admin), monto, fecha, descripción, y quién lo registró.
- Listado filtrable por rango de fechas y por tipo.
- Reporte simple del neto del negocio en un periodo: ventas + otros ingresos − gastos (complementa, no reemplaza, los reportes de ventas de otras fases).
- Solo el Admin (y Super Admin) deben poder ver y registrar esta sección, salvo que se le dé el permiso explícito a un rol personalizado — es información financiera sensible.

Definition of Ready: fase de Roles y permisos completada.

Definition of Done: probado registrando al menos un gasto y un ingreso, verificando el cálculo del neto del periodo; build y tests pasando.

### Fase: Código de barras — lectura

Objetivo: permitir escanear el código de barras de un producto (con lector físico USB tipo teclado, o cámara si aplica) y que la aplicación identifique automáticamente el producto correspondiente.

Alcance mínimo:
- Campo de entrada que capture el código escaneado (los lectores USB comunes funcionan como teclado, emiten los dígitos + Enter).
- Búsqueda del producto por código de barras en la base de datos.
- Si el código no corresponde a ningún producto, mostrar mensaje claro y permitir registrar uno nuevo o cancelar.
- Manejo de error si el lector no está conectado o el código es inválido.

Definition of Ready: fase de Productos, categorías y SKU completada (el modelo de `Producto` debe tener el campo `CodigoBarras` único e indexado, ya definido en esa fase).

Definition of Done: probado con al menos un código real escaneado y con un código inexistente (caso de error), tests unitarios de la búsqueda por código, build y tests pasando.

### Fase: Código de barras — generación

Objetivo: generar un código de barras único para productos propios que no vengan con uno de fábrica (ej. productos a granel, marca blanca, etc.).

Estándar definido: **EAN-13** (estándar GS1, el mismo usado por GS1 República Dominicana y por el retail dominicano en general — es el que leerá cualquier lector de código de barras estándar).

Para productos propios sin código de fábrica, usar el rango de **uso interno/restringido reservado por GS1** (prefijo `20`–`29`), que es la convención universal para códigos generados internamente por un comercio sin necesidad de registrarse ante GS1 como empresa emisora. Esto evita colisión con códigos reales de productos de fábrica.

Alcance mínimo:
- Registrar como decisión arquitectónica en `docs/DECISIONS.md`: uso de EAN-13, rango interno `20`–`29`, y el algoritmo de cálculo del dígito verificador.
- Generación automática de un código único al crear el producto, con validación de que no colisione con uno existente.
- Renderizado del código de barras como imagen para poder imprimirlo (etiqueta).
- Opción de imprimir o exportar la etiqueta (definir formato: PDF, imagen, impresora térmica, etc. — preguntar antes de asumir).

Definition of Ready: fase de "lectura" completada, ya que generación y lectura comparten el mismo estándar de código.

Definition of Done: código generado, sin colisiones verificadas por test, imagen/etiqueta generada correctamente, build y tests pasando.

### Fase: Métodos de pago

Objetivo: registrar el método de pago de cada venta: efectivo, transferencia o tarjeta.

Alcance mínimo común a los tres métodos:
- Selección del método de pago al cerrar una venta.
- El método pagado queda registrado en la venta (para reportes/consultas futuras).

Reglas específicas por método:

**Efectivo:**
- Capturar el monto total a pagar (ya calculado por la venta).
- Capturar cuánto dinero entrega el cliente.
- Calcular y mostrar el vuelto/cambio a devolver = monto entregado − monto total.
- Validar que el monto entregado sea igual o mayor al total (no permitir un pago insuficiente sin advertencia explícita).

**Transferencia:**
- Registrar que el pago fue por transferencia.
- La verificación es **manual, hecha por el vendedor**: el vendedor confirma en pantalla (checkbox/botón "Transferencia verificada") que revisó su banco/app y el dinero llegó, antes de que la venta se cierre como pagada.
- Mientras no se marque como verificada, la venta debe quedar en un estado distinto (ej. "Pendiente de verificación") y no contarse como cobrada en caja hasta confirmarse.
- Campo opcional para anotar una referencia/nota (ej. "Transferencia BHD, últimos 4 dígitos 1234") por si el vendedor necesita rastrear cuál transferencia corresponde a cuál venta — no es obligatorio, pero ayuda si hay varias transferencias pendientes al mismo tiempo.

**Tarjeta (recomendación):**
- Dado que la app es de escritorio (WinForms, una sola PC) y el cobro con tarjeta ocurre físicamente en un datáfono aparte (ej. Cardnet, Azul, Visanet — los procesadores más comunes en RD), **no se recomienda integrar la app con el datáfono en esta fase**: esas integraciones requieren certificación del procesador y añaden complejidad desproporcionada para una primera versión.
- Lo recomendado: registro manual — el vendedor selecciona "Tarjeta" y confirma que el datáfono aprobó el cobro, igual que hoy haría con una caja registradora normal.
- Sí conviene distinguir **débito / crédito** como subtipo, porque es información útil para reportes y no cuesta nada capturarla (es solo un selector adicional).
- Si en el futuro se desea integrar con un datáfono real, eso debe tratarse como una fase nueva y separada, no como parte de esta.

Definition of Ready: el modelo de `Venta` debe soportar un campo `MetodoPago` y, si aplica, campos adicionales según lo que se defina para transferencia y tarjeta — esto debe decidirse y registrarse en `docs/DECISIONS.md` antes de codificar.

Definition of Done: los tres métodos probados con tests (incluyendo el cálculo de vuelto con varios casos: pago exacto, pago mayor, intento de pago insuficiente), build y tests pasando.

### Fase: Sistema de roles y permisos

Objetivo: control de acceso basado en roles (RBAC), con roles fijos del sistema y roles personalizables que el dueño del negocio puede crear — igual que en un sistema web con permisos granulares.

Jerarquía de roles:

- **Super Admin (fijo)**: tu rol como desarrollador/revendedor. No editable, no eliminable desde la interfaz normal. Confirmado contigo: credenciales **distintas por instalación** (no una contraseña maestra igual en todos los negocios), para que si se compromete una instalación no se comprometan todas.
  - Control total sobre la instalación: puede ver y gestionar todo lo que el Admin puede, más funciones exclusivas de soporte.
  - Puede **resetear la contraseña de cualquier usuario**, incluyendo la del Admin (dueño), para casos donde el dueño olvide su contraseña y necesite tu ayuda para recuperar acceso. El reset debe generar una contraseña temporal que obligue a cambiarla en el siguiente inicio de sesión (ver regla de "cambio de contraseña obligatorio" más abajo).
  - Puede activar/desactivar el modo multi-caja de la instalación (ver fase correspondiente).
- **Admin (dueño del negocio)**: gestiona su propio negocio — empleados, productos, reportes, configuración. Puede crear y editar roles personalizados y asignarles permisos.
- **Cajero / Empleado (rol por defecto)**: permisos limitados a operaciones de venta (registrar ventas, cobrar, ver su propio historial). Sin acceso a configuración, reportes financieros completos, ni gestión de usuarios.
- **Roles personalizados**: el Admin puede crear roles nuevos (ej. "Encargado de inventario", "Supervisor") y asignarles una combinación de permisos de una lista predefinida — no texto libre. Esa lista de permisos disponibles debe quedar cerrada y documentada en `docs/DECISIONS.md` antes de codificar, para que sea la referencia de qué combinaciones son posibles.

Modelo de datos sugerido:
- `Roles` (con flag `EsFijo` para proteger Super Admin y Cajero contra eliminación/edición de sus permisos base).
- `Permisos` (catálogo cerrado de permisos disponibles, ej. `Ventas.Crear`, `Ventas.Anular`, `Productos.Editar`, `Reportes.Ver`, `Usuarios.Gestionar`, etc.).
- `RolPermiso` (relación N:M entre Roles y Permisos).
- `Usuario` con FK a `Rol`, más un campo `DebeCambiarPassword` (booleano).

Regla de contraseña temporal (obligatoria):
- Cuando el Admin o el Super Admin crea un nuevo empleado, se le asigna una contraseña temporal (generada o definida por quien lo crea).
- Ese usuario queda marcado con `DebeCambiarPassword = true`.
- Al iniciar sesión por primera vez, la app debe forzar el cambio de contraseña antes de permitir cualquier otra acción — no se puede omitir ni cerrar esa pantalla sin cambiarla.
- La misma regla aplica cuando el Super Admin resetea la contraseña de alguien (Admin o empleado): queda marcado para cambiarla en el siguiente inicio de sesión.

Alcance mínimo:
- Login con usuario/contraseña, con contraseñas **hasheadas** (nunca en texto plano — usar BCrypt o equivalente).
- Semilla (seed) automática al instalar: 1 Super Admin fijo + 1 Admin inicial (configurado por el dueño en el primer arranque) + rol Cajero predefinido con sus permisos base.
- Pantalla donde el Admin (o el Super Admin) gestiona empleados: crear, editar, desactivar (no eliminar directamente, para conservar la trazabilidad de qué empleado hizo qué venta).
- Flujo de cambio de contraseña obligatorio en primer inicio de sesión, como se describió arriba.
- Función de "resetear contraseña" disponible para el Super Admin sobre cualquier usuario, incluyendo el Admin.
- Pantalla donde el Admin crea/edita roles personalizados y les asigna permisos de la lista cerrada.
- Cada pantalla y función de la app debe verificar el permiso correspondiente antes de mostrarse o ejecutarse — igual que un middleware de autorización en un sistema web, pero aplicado en el arranque de cada formulario/acción de WinForms.

Definition of Ready: debe existir ya un modelo de datos base (no puede ser la primera pantalla que se construya sin nada detrás).

Definition of Done: probado con al menos 3 roles distintos (Admin, Cajero, uno personalizado) verificando que cada uno ve y hace únicamente lo permitido; flujo de cambio de contraseña obligatorio probado con un usuario nuevo y con un reset del Super Admin; contraseñas verificadas como hasheadas en la base de datos; build y tests pasando.

### Fase: Apertura y cierre de caja (turno)

Objetivo: que cada turno de trabajo empiece con una apertura de caja (registrando con cuánto efectivo arrancó el cajero) y termine con un cierre de caja (con cuánto cerró), para que el Admin pueda cuadrar los números de cada turno y detectar diferencias.

Alcance mínimo:
- Al iniciar sesión un Cajero (o cualquier rol que opere ventas), si no hay un turno abierto para esa caja, la app debe pedir el **monto de apertura** (efectivo inicial en caja) antes de permitir registrar ventas.
- Durante el turno, la app debe poder calcular en cualquier momento cuánto debería haber en caja: monto de apertura + ventas en efectivo − cualquier salida registrada (si en el futuro se agregan salidas/gastos de caja, no es parte del alcance mínimo de esta fase).
- Al cerrar el turno, el cajero (o quien corresponda) ingresa el **monto de cierre** (efectivo contado físicamente), y la app muestra la diferencia entre lo esperado (calculado) y lo contado — sin bloquear el cierre si hay diferencia, pero dejándola registrada.
- El Admin debe poder ver un historial de turnos: quién abrió, con cuánto, quién cerró, con cuánto, diferencia, fecha y hora.
- Si un cajero intenta registrar una venta sin tener un turno abierto, la app debe bloquear la venta y pedir abrir turno primero.

Modelo de datos sugerido: entidad `Turno` (o `AperturaCierre`) con: `UsuarioAperturaId`, `MontoApertura`, `FechaApertura`, `UsuarioCierreId`, `MontoCierre`, `FechaCierre`, `Diferencia`, `CajaId` (relevante cuando exista multi-caja).

Definition of Ready: la fase de roles y permisos debe estar completada, ya que el turno se asocia a un usuario autenticado.

Definition of Done: probado abriendo y cerrando al menos 2 turnos distintos, con y sin diferencia entre lo esperado y lo contado; validado que no se pueden registrar ventas sin turno abierto; build y tests pasando.

### Fase: Modo caja única / multi-caja

Objetivo: que la app funcione tanto en un negocio con una sola caja/PC como en uno con varias cajas trabajando simultáneamente sobre el mismo inventario y las mismas ventas — y que **tú, como Super Admin, puedas activar o desactivar el modo multi-caja de una instalación en cualquier momento**, no solo al instalar. Esto es porque venderás negocios que empiezan en caja única y luego crecen, y otros que necesitan multi-caja desde el día uno.

⚠️ **Esto es una decisión arquitectónica importante, no solo una función más.** Debe registrarse en `docs/DECISIONS.md` y confirmarse conmigo antes de escribir código.

El problema: SQLite (ver DEC-001) está pensada para un solo proceso escribiendo a la vez. Funciona perfectamente para "caja única". Para "multi-caja" real — varias PCs vendiendo al mismo tiempo sobre el mismo inventario — SQLite por sí sola no es adecuada; escrituras concurrentes por red pueden corromper la base de datos.

Como además necesitas poder **cambiar de modo sin reinstalar todo**, esto implica dos cosas que hay que diseñar juntas:
1. Qué motor de base de datos se usa en cada modo.
2. Cómo se migran los datos existentes (productos, ventas, turnos, usuarios) de un negocio que empezó en caja única y pasa a multi-caja, sin perder su historial.

Opciones a evaluar conmigo antes de iniciar esta fase:

1. **Motor cliente-servidor en red local** (ej. SQL Server Express, MySQL, PostgreSQL) corriendo en una PC "servidor" dentro del negocio, con las demás cajas conectándose a esa. Coherente con DEC-002 (EF Core separa la app del motor de base de datos, así que cambiar de proveedor es factible sin rehacer la lógica de negocio). Al activar multi-caja en un negocio que ya tenía caja única en SQLite, se migran los datos existentes al nuevo motor (un proceso de exportación/importación que hay que construir como parte de esta fase, no algo manual e improvisado). **Esta es la recomendación inicial.**
2. Mantener SQLite pero con un servicio/API intermedio corriendo en la PC servidor, y las demás cajas hablan con esa API en vez de tocar el archivo directamente. Más trabajo de desarrollo, pero evita instalar un motor de base de datos adicional.
3. Cada caja con su propia base de datos local y sincronización periódica. Más complejo y con riesgo real de conflictos (ej. el mismo producto vendido dos veces si el inventario no sincroniza a tiempo) — no recomendada como primera opción.

Alcance mínimo (una vez decidido el enfoque):
- Un ajuste, visible y controlable solo por el Super Admin, para activar/desactivar el modo multi-caja de esa instalación (y, si aplica, indicar qué PC es el "servidor" y cuáles son "clientes").
- Proceso de migración de datos al activar multi-caja por primera vez en un negocio que ya tenía caja única, sin pérdida de historial (productos, ventas, turnos, usuarios).
- Si se elige la opción 1: connection string configurable, migraciones aplicadas contra el motor elegido, validado con EF Core.
- Manejo de error claro si una caja cliente pierde conexión con el servidor — no debe cerrar la app abruptamente ni corromper una venta en curso.

Definition of Ready: decisión registrada en `docs/DECISIONS.md` sobre qué enfoque se usará para multi-caja y cómo se migran los datos al activarlo, confirmada conmigo.

Definition of Done: probado activando multi-caja sobre una instalación que ya tenía datos en modo caja única (sin pérdida de información), y luego con al menos 2 "cajas" operando simultáneamente sobre el mismo inventario, sin pérdida ni duplicación de datos; build y tests pasando.

### Orden sugerido

1. Fase: Sistema de roles y permisos — primero, ya que login y permisos son la base sobre la que se construyen las demás pantallas.
2. Fase: Clientes — necesaria temprano porque tanto ventas como reparaciones dependen de tener un cliente asociado.
3. Fase: Apertura y cierre de caja (turno) — depende de tener usuarios autenticados.
4. Fase: Productos, categorías y SKU — base del inventario, incluyendo el modelo mixto (celulares con IMEI vs. accesorios/papelería por cantidad).
5. Fase: Proveedores
6. Fase: Compras a proveedor (actualiza inventario)
7. Fase: Alertas de stock mínimo
8. Fase: Reparaciones de celulares — depende de Clientes y Roles; no depende de Productos porque es un servicio, no inventario.
9. Fase: Gastos e ingresos del negocio
10. Fase: Código de barras — lectura
11. Fase: Código de barras — generación
12. Fase: Métodos de pago — usados tanto por ventas como por reparaciones al momento de cobrar.
13. Fase: Modo caja única / multi-caja — al final, porque es la decisión arquitectónica de mayor impacto y afecta a todas las fases anteriores; conviene construir y probar bien el sistema en modo caja única primero, y luego agregar la capacidad de activar multi-caja.

Este orden es una sugerencia inicial, no una obligación — confírmalo o ajústalo conmigo antes de iniciar la primera fase de este roadmap.

---

## IMPORTANTE

Quiero que actúes como si este fuera un proyecto profesional de largo plazo.

La memoria de la conversación NO debe ser la única fuente de contexto.

El propio proyecto debe poder explicar:

- qué estamos construyendo
- cómo está construido
- qué decisiones hemos tomado
- qué hemos terminado
- qué estamos haciendo
- qué falta
- cuáles son los problemas conocidos
- cuál es el siguiente paso

Cuando vuelva a abrir el proyecto después de varios días, quiero que puedas leer esos archivos, inspeccionar el código, verificar contra git, y continuar exactamente desde donde dejamos el trabajo.

NO reinicies el proyecto.
NO vuelvas a crear archivos existentes sin necesidad.
NO cambies la arquitectura sin justificación registrada en `docs/DECISIONS.md`.
NO repitas fases ya completadas.

Continúa desde el estado real del proyecto, verificado — no desde lo que un archivo dice que pasó.
