# Contexto BenignoGL WebGL

Fecha de referencia: 2026-05-23  
Proyecto Unity: `E:\Documentos\GitHub\650`  
Build WebGL usada para pruebas/publicacion: `E:\Documentos\Trabajos\GlobalSnopek\Build`

## Objetivo

El proyecto es una web app de visualizacion arquitectonica en Unity WebGL. La app puede estar en estados de menu o en modo first person. Se necesitaba que, en desktop, cuando el usuario no tenga capturado el puntero del juego, aparezca un mensaje centrado sobre el canvas:

`haz click aqui para jugar`

El mensaje debe comportarse como una pausa/foco de juego: bloquear clicks, hovers, movimiento y camara hasta que el usuario haga click sobre el area de Unity/canvas. No debe aparecer en moviles ni durante la pantalla de carga.

## Template WebGL

Se creo y asigno el template:

`Assets/WebGLTemplates/BenignoGL`

Unity quedo apuntando a:

`ProjectSettings/ProjectSettings.asset`

con:

`webGLTemplate: PROJECT:BenignoGL`

El template se origino desde el `index.html` actual de la build y se adapto para usar variables de template de Unity:

- `{{{ LOADER_FILENAME }}}`
- `{{{ DATA_FILENAME }}}`
- `{{{ FRAMEWORK_FILENAME }}}`
- `{{{ CODE_FILENAME }}}`
- `{{{ JSON.stringify(COMPANY_NAME) }}}`
- `{{{ JSON.stringify(PRODUCT_NAME) }}}`
- `{{{ JSON.stringify(PRODUCT_VERSION) }}}`

## Overlay de foco

Archivo principal:

`Assets/WebGLTemplates/BenignoGL/index.html`

Elementos relevantes:

- `#unity-play-prompt`
- `window.BenignoGLSetGameplayPointerMode(isEnabled)`
- `window.BenignoGLSetGameplaySceneReady(isReady)`
- `window.BenignoGLIsGameplayFocused()`

El overlay:

- se dimensiona exactamente al rectangulo de `#unity-canvas`;
- aparece solo si:
  - Unity ya cargo;
  - es desktop;
  - Unity esta en modo gameplay first person;
  - la escena principal y subescenas estan listas;
  - el navegador no tiene `pointerLockElement === canvas`;
- no aparece en moviles;
- no aparece en loading;
- bloquea interaccion con el canvas mientras esta visible;
- solo permite recuperar pointer lock desde el click autorizado sobre el prompt;
- si el navegador intenta recapturar pointer lock por un click fuera del canvas/prompt, el template lo libera inmediatamente.

## Cache Busting

Se agrego versionado a las URLs criticas de Unity para evitar mezclar `Build.wasm` nuevo con `Build.framework.js` viejo:

- `Build.loader.js`
- `Build.data`
- `Build.framework.js`
- `Build.wasm`

La version se calcula en el template con:

```js
var buildVersion = [
  {{{ JSON.stringify(PRODUCT_VERSION) }}},
  "benignogl",
  encodeURIComponent(document.lastModified || "runtime")
].join("-");
var buildVersionQuery = "?v=" + buildVersion;
```

Esto resolvio el error:

```text
WebAssembly.instantiate(): Import "env" "BenignoGL_IsGameplayFocused": function import requires a callable
```

Ese error se debia a cache/desfase entre archivos WebGL.

## Bridge JS / C#

Archivo:

`Assets/Plugins/WebGL/DeviceSampler.jslib`

Se agregaron funciones JS para comunicacion con WebGL:

- `BenignoGL_SetGameplayPointerMode`
- `BenignoGL_SetGameplaySceneReady`
- `BenignoGL_IsGameplayFocused`

Archivo:

`Assets/_Interaction/Scripts/WebGL/BenignoGLWebBridge.cs`

Expone desde C#:

- `SetGameplayPointerMode(bool isEnabled)`
- `SetGameplaySceneReady(bool isReady)`
- `IsGameplayFocused()`

En editor/no WebGL, `IsGameplayFocused()` devuelve `true` para no interferir con pruebas locales.

## Estado de escenas

Archivo:

`Assets/_Interaction/Scripts/WebGL/BenignoGLSceneState.cs`

Valida que el overlay solo pueda funcionar cuando esten cargadas:

- `650-Interaccion`
- `650-PB`
- `650-Geo`

Tambien valida que `LoadingScreen` haya terminado:

```csharp
Object.FindFirstObjectByType<LoadingScreen>() == null || LoadingScreen.IsSceneReady
```

Escucha:

- `SceneManager.sceneLoaded`
- `SceneManager.sceneUnloaded`
- `SceneManager.activeSceneChanged`

y actualiza el estado con `BenignoGLWebBridge.SetGameplaySceneReady(...)`.

## Loading Screen

Archivo:

`Assets/_Interaction/Scripts/UI/LoadingScreen.cs`

Se agregaron llamadas a:

`BenignoGLSceneState.UpdateSceneReadyState()`

cuando:

- empieza el loading (`IsSceneReady = false`);
- termina sin subescenas;
- termina con subescenas.

Esto evita que el mensaje aparezca en `Assets/_Interaction/Scenes/LoadingScreen.unity`.

## First Person Movement

Archivo:

`Assets/_Interaction/Scripts/Player/FirstPersonMovement.cs`

Se agrego pausa de movimiento/camara cuando el navegador no tiene pointer lock real:

```csharp
private bool ShouldPauseForWebFocus()
{
    return !isMobile
        && !isInteracting
        && !usePointerLook
        && !BenignoGLWebBridge.IsGameplayFocused();
}
```

Cuando `ShouldPauseForWebFocus()` es true:

- `moveInput = Vector2.zero`;
- `lookInput = Vector2.zero`;
- `currentVelocity = Vector3.zero`;
- se evita `HandleMovement()`;
- se evita `HandleMouseLook()`;
- `OnMove` y `OnLook` ignoran input nuevo.

Esto corrigio el bug donde la camara seguia girando aunque estuviera visible el mensaje.

## UI Manager

Archivo:

`Assets/_Interaction/Scripts/UI/UIManager.cs`

Se informa al template cuando Unity entra/sale de gameplay:

- `showCursor()` llama `BenignoGLWebBridge.SetGameplayPointerMode(false)`;
- `hideCursor()` llama `BenignoGLWebBridge.SetGameplayPointerMode(true)`.

Esto evita que el overlay aparezca sobre menus, donde Unity necesita cursor visible para botones.

## Audio y Persistent Data

En `index.html` se activo:

```js
autoSyncPersistentDataPath: true
```

Esto elimina el warning de Unity sobre `JS_FileSystem_Sync()` deprecado.

Tambien se agrego una mitigacion de audio:

- wrapper de `AudioContext`;
- `unlockAudioContexts()` en click/tecla/prompt.

El warning de Chrome:

```text
The AudioContext was not allowed to start. It must be resumed after a user gesture.
```

puede seguir apareciendo si Unity intenta audio antes del primer gesto. No bloquea la experiencia y se mitiga con el primer click/tecla.

## Build y Pruebas

Unity usado:

`E:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe`

Se generaron builds WebGL en:

`E:\Documentos\Trabajos\GlobalSnopek\Build`

Comandos de batchmode usados durante la conversacion mediante un script temporal `CodexWebGLBuild.cs`, luego eliminado:

```powershell
& "E:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe" `
  -batchmode `
  -quit `
  -projectPath "E:\Documentos\GitHub\650" `
  -buildTarget WebGL `
  -executeMethod CodexWebGLBuild.Build `
  -logFile "E:\Documentos\GitHub\650\Logs\codex-webgl-build-focusfix.log"
```

El build final termino con:

```text
Build Finished, Result: Success.
```

La build local se sirvio en:

`http://127.0.0.1:8123/`

Se verifico:

- carga sin `LinkError`;
- el prompt no aparece en inicio/loading/menu;
- el prompt aparece cuando se fuerza `gameplay + sceneReady` y no hay pointer lock;
- click fuera del canvas no oculta el prompt ni captura pointer lock;
- click sobre prompt captura pointer lock;
- `Escape` libera pointer lock y vuelve a mostrar prompt;
- movil/emulacion movil no muestra prompt;
- la build contiene `_BenignoGL_IsGameplayFocused` en `Build.framework.js`.

## Publicacion

Sitio publico mencionado:

`https://benignostudio.com/650v2/`

Cuando se sube una build nueva, deben subirse juntos:

- `index.html`
- carpeta `Build/`
- `TemplateData/` si cambio

Si aparece un error de import de WebAssembly o comportamiento viejo, purgar cache/CDN o hacer hard reload. El cache-busting del template deberia reducir este problema.

## Estado Actual

`BenignoGL` debe considerarse la plantilla oficial para este proyecto WebGL.

La build local en:

`E:\Documentos\Trabajos\GlobalSnopek\Build`

quedo alineada con esa plantilla despues de las correcciones.
