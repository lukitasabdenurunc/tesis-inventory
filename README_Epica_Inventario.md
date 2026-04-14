# Épica: Módulo Inventario

## Objetivo de la Épica
Construir y modernizar de raíz el módulo de Inventario reemplazando el antiguo modelo plano por un sistema de organización estructurado en dos niveles (Rubros y Familias) con la capacidad de definir atributos dinámicos obligatorios adaptables según la Familia del producto.
También incluye un sistema de autogeneración de SKU (Stock Keeping Unit).

## HU (Historias de Usuario) Incluidas
- **HU-INV-01**: ABM de Rubros.
- **HU-INV-02**: ABM de Familias asociadas a Rubros.
- **HU-INV-03**: Mantenimiento de Atributos del Sistema (Diccionario Global).
- **HU-INV-04**: Asociación y configuración de Atributos dentro de Familias de productos (Incluye la desvinculación selectiva con auditoría visual).
- **HU-INV-05**: Gestión Avanzada de Productos (con auto-SKU y captura dinámica de atributos).
- **HU-INV-06**: Adaptación de la relación Stock transaccional entre Sede y Producto.
- **HU-INV-07**: Implementación del Submódulo Stock: Control de Inventario por Sede, registro de Consumo/Egresos, Adición de Stock por Compra/Ajustes, y Transferencias Logísticas Inter-Sedes con Historial.
- **HU-INV-08**: Desarrollo de vista global de Historial de Movimientos de Stock, con panel de filtros avanzados y detalle de saldos restantes.

## Alcance Funcional
- **Rubros y Familias**: Flujo de gestión a doble vista interdependiente. Un Rubro agrupa Familias, estas últimas engloban a su vez el catálogo real de productos. Todo soporta Baja Lógica.
- **Atributos**: Tipos de datos soportados (`STRING`, `NUMBER`, `DECIMAL`, `BOOLEAN`, `LIST`).
- **Productos**: El SKU asume la forma de concatenación del Código de Rubro + Código de Familia + Nro Correlativo. Al crearlo, la UI genera los controles visuales correspondientes basados en las normas estrictas impuestas por la asociación con su familia (FamiliaAtributo). El borrado del producto exige confirmación exacta validando su nombre.
- **Stock**: Visualización detallada del inventario por sede. Permite registrar consumos/egresos (con motivos predefinidos), incrementar stock y solicitar transferencias hacia otras sedes, registrando una trazabilidad (Movimientos y Transferencias) completa.

## Capas Involucradas
- **Domain**: `Rubro.cs`, `Familia.cs`, `Atributo.cs`, `FamiliaAtributo.cs`, `AtributoOpcion.cs`, `Producto.cs`, `ProductoAtributoValor.cs`, `Stock.cs`, refactor de `Sede.cs`. Adición de `Movimiento.cs`, `Transferencia.cs`, `HistorialTransferencia.cs`.
- **Infrastructure**: Implementación de RepositoryPattern (`RubroRepository`, `StockRepository`, `MovimientoRepository`, `TransferenciaRepository`, etc.), inyección por Entity Framework y Migraciones (`InventorySchemaInitialization`, `AddStockModule`).
- **Application**: DTOs completos de Lectura/Escritura y lógica pesada abstraída en `ProductosService.cs`, `AtributosService.cs`, `StockService.cs` y análogos.
- **Presentation**: `*Controller.cs` exponiendo endpoints REST estándar (`StockController`).
- **Frontend (Angular)**: Módulo `inventory` implementando componentes (`rubros-familias`, `atributos`, `productos`, `stock`) y el manejo de modales integrados para transacciones logísticas y consultas del almacén.

## Flujo General
1. Usuario crea Rubro "Bazar".
2. Usuario crea Familia "Tazas" englobada en "Bazar".
3. Se crea el atributo maestro "Capacidad" (NUMBER, ml) y se asinga obligatoriamente a "Tazas".
4. Al ir a agregar un inventario, eligen rubro/familia, el formulario pide la Capacidad obligatoria. Luego, el servidor arroja el SKU auto-armado y persiste el catálogo.

## Decisiones de Implementación y Ajustes
**[2026-04-14]**
- **Descripción del cambio**: Implementación de contexto global (interceptor HTTP y SedeContextService en UI) para el manejo dinámico de la sede seleccionada en componentes dependientes del inventario (Stock / Movimientos).
- **Motivo técnico**: Se optó por una arquitectura reactiva (RxJS BehaviorSubject) en sustitución de inyección manual de parámetros en Sede para asegurar la recarga automática (auto-refresco y refetching) de los grids de datos en toda la aplicación Inventario cuando el Administrador cambia la sede en el layout global.
- **Impacto funcional**: Mejora sustancial en la UX del Administrador al consultar inventarios sedes-afines. Los grids de datos listarán estricta y únicamente el stock y transferencias correspondientes a la sede actualmente dispuesta en la top-bar por el Admin, transparentando la operación de consulta en multi-sede.
