# Contexto del proyecto 650

Este proyecto es una experiencia Unity ubicada en `E:\Documentos\GitHub\650`. La zona principal de trabajo es `Assets/650`, donde viven las escenas, prefabs, scripts y recursos propios del proyecto. Las carpetas `Library`, `Temp`, `Logs` y caches de Unity deben tratarse como ruido salvo casos puntuales de diagnóstico. Para entender o modificar funcionalidad, conviene empezar por `Assets/650`; para dependencias, revisar `Packages/manifest.json`.

## Lo que sabemos del proyecto

El proyecto usa Unity con URP (`com.unity.render-pipelines.universal`), Input System, TextMesh Pro/UGUI, Cinemachine, Localization, Visual Scripting y varios paquetes de soporte. Hay escenas principales como `650-Geo`, `650-PB`, `650-Interaccion` y `650-MenuInicial`. También existen prefabs importantes como `Panel_Calculadora`, `MainCamara`, mobiliario, lámparas y elementos VFX.

El foco reciente del trabajo ha estado en una calculadora de presupuesto asociada a cambios de materiales dentro de la experiencia. El módulo vive en:

`Assets/650/Scripts/BudgetCalculator`

Los datos editables viven en:

`Assets/650/Resources/Budget`

## Calculadora de presupuesto

Se implementó/organizó un flujo runtime para calcular precios según materiales seleccionados. El arranque lo hace `BudgetCalculatorBootstrap`, que crea o asegura un `BudgetCalculatorManager` después de cargar la escena. El manager carga el catálogo desde `Resources/Budget/BudgetCalculatorCatalog`, inicializa opciones por defecto de `0 EUR` por categoría y mantiene las selecciones activas.

La interfaz se conecta mediante `BudgetCalculatorUI`, que se agrega automáticamente al prefab o panel `Panel_Calculadora` / `Panel Calculadora` cuando existe. La UI genera filas de materiales, muestra categoría, nombre visible, descripción, precio y total. Los precios en cero aparecen como `Incluido`; los demás se muestran con formato `+N EUR`.

El puente entre interacción y presupuesto se resolvió de dos maneras:

- `BudgetInteractionMaterialListener` escucha `InteractObject.OnAnyInteract` y relaciona el nombre del material transferido con una opción del catálogo.
- Para configuración manual precisa, `MaterialCostOption` puede agregarse al mismo objeto que tiene `MaterialTransferHandler`; ahí se asigna un `BudgetMaterialOptionSO`, y el componente reporta la selección cuando termina la transferencia.

Las opciones existentes incluyen categorías/materiales para sofá, piso, muros y encimera. Los precios y nombres visibles se editan en `Assets/650/Resources/Budget/Options`, mientras que la lista total disponible se controla desde `BudgetCalculatorCatalog.asset`.

## Aprendizajes importantes

El proyecto es grande y mezcla assets activos con prototipos, muestras y archivos generados. La mejor estrategia es evitar exploraciones exhaustivas: hacer búsquedas dirigidas, leer pocos archivos clave y pedir contexto si una ruta no aparece rápido.

En Unity, muchas modificaciones reales deben verificarse dentro del editor. Para cambios visuales conviene capturar escena; para scripts o flujo lógico conviene revisar consola y compilación. Ya hay varios logs de compilación y WebGL en la raíz, lo que sugiere que se ha trabajado en builds WebGL, ajustes de preview, puente directo de materiales, preservación de prefab, transferencia de eventos y ajustes de UI del presupuesto.

No se deben editar paquetes cacheados en `Library/PackageCache`. Si alguna dependencia necesita cambios, primero debe convertirse en paquete local/embebido. Los cambios propios deben quedar en `Assets`, `Packages` locales o `ProjectSettings` si realmente son ajustes globales.

## Estado actual y precauciones

Al crear este contexto, `git status` mostraba una modificación existente en `ProjectSettings/ProjectSettings.asset`. No se tocó ni se revirtió. Cualquier trabajo futuro debe respetar cambios previos del usuario y evitar comandos destructivos.

Para continuar el desarrollo, el camino más seguro es:

## antes de cualquier paso consultar directamente la documentación de unity y html:
https://docs.unity3d.com/6000.3/Documentation/Manual/index.html
https://docs.unity3d.com/6000.3/Documentation/ScriptReference/index.html
https://html.spec.whatwg.org/multipage/

1. Buscar primero en `Assets/650`.
2. Modificar scripts o assets del módulo exacto.
3. Verificar compilación/consola.
4. Verificar visualmente si el cambio afecta escena, prefabs o UI.
5. Documentar cualquier conexión manual necesaria dentro del editor.
6. Al momento de ejecutar una build no necesito una descripción de cada paso si generas un schedule build check a menos que aparezca un error.

