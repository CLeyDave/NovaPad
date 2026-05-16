# Documentación de Optimización — NovaPad v3.2.0

## Sesión Actual (2026-05-15)

### Objetivo
Optimizar overlay (mover in-process), RGB y startup de la app, manteniendo 100% de funcionalidad y persistencia.

### Decisiones tomadas
- **Overlay: in-process** — Eliminar `NovaPad.Overlay.exe` como proceso separado, mover `VentanaFondo` al proceso WPF. Llamadas directas en vez de named pipe + JSON.
- **HidDiagnostics** — Quitar del startup (era debug-only, enumeraba todos los HID cada vez).
- **Version bump** — `3.2.0` (minor, cambio significativo pero compatible).
- **Orden:** Primero Fase 1 (overlay), Fase 2 y 3 después.

---

## Fase 1 — Overlay In-Process

### Qué cambia

**Antes:**
```
AdminOverlayVm
  → IniciadorCapa.ArrancarAsync()
    → Process.Start("NovaPad.Overlay.exe")
    → RemitenteRed.ConectarAsync() (pipe client, 10 retries × 1s)
      → ReceptorPipe (pipe server)
        → Clasificador (JSON deserialize + route)
          → DepositoEstado (cache state)
            → handler en VentanaFondo
```

**Después:**
```
AdminOverlayVm
  → _ventana.Show() (instantáneo, mismo proceso)
  → _ventana.AplicarConfig(cfg) (llamada directa)
  → _ventana.AplicarCardConfig(cfg) (llamada directa)
  → _ventana.ActualizarHud(info) (llamada directa)
```

### Archivos involucrados

#### Crear en `NovaPad.WPF/Overlay/`

| Archivo | Procedencia |
|---|---|
| `VentanaFondo.cs` | `NovaPad.Overlay/VentanaFondo.cs` modificado |
| `Partes/TarjetaMando.cs` | `NovaPad.Overlay/Partes/TarjetaMando.cs` |
| `Partes/BurbujaInfo.cs` | `NovaPad.Overlay/Partes/BurbujaInfo.cs` |
| `Partes/OrganizadorBurbujas.cs` | `NovaPad.Overlay/Partes/OrganizadorBurbujas.cs` |
| `Partes/PanelExtendido.cs` | `NovaPad.Overlay/Partes/PanelExtendido.cs` |
| `Partes/CuadroDepuracion.cs` | `NovaPad.Overlay/Partes/CuadroDepuracion.cs` |
| `Partes/MarcadorHora.cs` | `NovaPad.Overlay/Partes/MarcadorHora.cs` |
| `Dibujo/DisenadorColores.cs` | `NovaPad.Overlay/Dibujo/DisenadorColores.cs` |

#### Modificar

| Archivo | Cambio |
|---|---|
| `NovaPad.WPF/ViewModels/AdminOverlayVm.cs` | `IniciadorCapa` + `RemitenteRed` → `VentanaFondo` directo |
| `NovaPad.WPF/App.xaml.cs` | DI: sacar `RemitenteRed`, `IniciadorCapa`; agregar `VentanaFondo` |
| `NovaPad.WPF/Services/LightingEngine.cs` | L39: eliminar `_ = HidDiagnostics.RunAsync()` |
| `NovaPad.WPF/NovaPad.WPF.csproj` | Sacar ref a `NovaPad.Overlay`, eliminar targets build |
| `NovaPad.WPF/Views/PaginaOverlay.xaml` | Sin cambios (UI igual) |

#### Eliminar

| Archivo | Razón |
|---|---|
| `NovaPad.Overlay/` (directorio completo) | Proyecto eliminado |
| `NovaPad.WPF/Services/RemitenteRed.cs` | Pipe client eliminado |
| `NovaPad.WPF/Services/IniciadorCapa.cs` | Launcher eliminado |
| `NovaPad.Core/Events/EnvoltorioRed.cs` | Envelope JSON eliminado |
| `NovaPad.Overlay.csproj` | Proyecto eliminado |

### Cambios específicos en `VentanaFondo.cs`

```diff
- using NovaPad.Overlay.Enlace;
- private readonly ReceptorPipe _receptor = new();
- private readonly Clasificador _clasificador = new();
- private readonly DepositoEstado _estado = new();
+ private InformeMandos? _ultimoInforme;
+ private readonly Dictionary<string, OverlayCardConfig> _configPorMando = new();
- _receptor.AlMensaje += msg => Dispatcher.Invoke(() => _clasificador.Procesar(msg));
- _receptor.Arrancar();
- _receptor.Parar();
```

- `_estado.UltimoInforme` → `_ultimoInforme`
- `_estado.ConfigPorMando` → `_configPorMando`
- Los handlers de pipe pasan a ser métodos públicos:
  - `AplicarConfig(DatosConfigOverlay)` — ya existe (línea 297)
  - `ActualizarHud(InformeMandos)` — ya existe (línea 366)
  - `AplicarCardConfig(OverlayCardConfig)` — nuevo, lógica del handler (líneas 277-294)
  - `LanzarAviso(string, string)` — nuevo, de `ComprobacionAviso` + `EstadoConexion`

### Cambios en `AdminOverlayVm.cs`

```diff
+ using NovaPad.WPF.Overlay;
- private readonly IniciadorCapa _launcher;
- private readonly RemitenteRed _net;
+ private readonly VentanaFondo _ventana;
```

- `StatusText` → `_ventana.IsVisible ? "En marcha" : "Detenido"`
- `StartLayer()` → `_ventana.Show(); _ventana.AplicarConfig(...); ...`
- `StopLayer()` → `_ventana.Hide()`
- `TestAlert()` → `_ventana.LanzarAviso(...)`
- `PushCfg()` → `_ventana.AplicarConfig(...)` (sincrónico, no `async Task`)
- `PushCardCfg()` → `_ventana.AplicarCardConfig(...)`
- `PushAllCardCfg()` → `foreach ... _ventana.AplicarCardConfig(kv.Value)`
- `PushHud()` → `_ventana.ActualizarHud(...)`

### Persistencia
No cambia. `IAppSettingsService` sigue siendo la fuente de verdad. `VentanaFondo` recibe config aplicada desde el VM, no guarda nada por sí mismo.

### Verificación post-implementación
1. Build sin errores
2. Overlay aparece al hacer "Start Layer"
3. Config (opacidad, escala, estilo) se aplica correctamente
4. Notificaciones de prueba funcionan
5. Cards de cada mando aparecen con datos correctos
6. Per-controller config se aplica al seleccionar mando individual
7. RGB sigue funcionando (no tocamos)
8. Al cerrar app, todo se limpia sin crashes

---

## Fase 2 — App Startup Paralelo

*Instrucciones para implementación futura.*

### Objetivo
Reducir tiempo hasta ventana visible de ~2-6s a ~200ms.

### Cambios en `App.xaml.cs`

**Hoy (secuencial):**
```
_host.StartAsync() + settings.Load()  → 250ms
controllerService.StartDetectionAsync()  → 5ms
controllerService.ScanForControllersAsync()  → 50-300ms (HID enum)
mainWindow.Show()  → window visible
overlayVm.StartLayer()  → 5-5000ms (pipe connect)
RGB restore loop  → 5ms
```

**Mañana (paralelo + defer):**
```csharp
// 1. Par: host + settings
await Task.WhenAll(
    _host.StartAsync(),                          // 50-200ms
    Task.Run(() => settingsService.Load())       // 5ms, en paralelo con host
);

// 2. Mostrar ventana INMEDIATAMENTE (~0ms)
mainWindow.Show();

// 3. Defer: scan, overlay, RGB en background
_ = Task.Run(async () =>
{
    await controllerService.ScanForControllersAsync();
    Dispatcher.Invoke(() => controllerService.NotifyConnectedControllers());

    // Restore RGB
    foreach (var ctrl in controllerService.ConnectedControllers) { ... }

    // Overlay si auto-start
    if (settings.Settings.AutoStartOverlay)
        Dispatcher.Invoke(() => overlayVm.StartLayer());
});
```

### Detalles importantes

| Paso | Timing | Notas |
|---|---|---|
| `_host.StartAsync()` + `settings.Load()` paralelo | ~50-200ms | Se espera sí o sí antes del Show |
| `mainWindow.Show()` | Inmediato | Ventana visible, puede estar vacía momentáneamente |
| `ScanForControllersAsync()` en background | ~50-300ms | HID enum sin bloquear UI |
| Overlay + RGB después del scan | Fire-and-forget | Usar `Dispatcher.Invoke` para tocar UI |

### Riesgos
- `ScanForControllersAsync()` inicia `ReadLoopAsync` por cada controller — si un controller no está conectado aún, no importa, el monitor loop (300ms) lo detectará después
- Los primeros segundos la UI puede mostrar "no controllers" hasta que termine el scan
- `Dispatcher.Invoke` debe usarse para cualquier toque a la UI desde el task en background
- La resolución de `_host.Services` debe hacerse DESPUÉS de `_host.StartAsync()`, pero ANTES de salir del `OnStartup` principal (para que el catch global funcione)

---

## Fase 3 — Micro-optimizaciones

*Instrucciones para implementación futura.*

### 1. BurbujaInfo lazy pool
**Archivo:** `OrganizadorBurbujas.cs`

**Hoy:** Pre-crea 8 instancias de `BurbujaInfo` en el constructor.

**Objetivo:** Crear instancias on-demand.

```csharp
private readonly Stack<BurbujaInfo> _pool = new();

private BurbujaInfo ObtenerBurbuja()
{
    return _pool.Count > 0 ? _pool.Pop() : new BurbujaInfo(_colorAcento);
}

private void ReciclarBurbuja(BurbujaInfo b)
{
    b.Ocultar();
    if (_pool.Count < MAX_POOL)
        _pool.Push(b);
}
```

Beneficio: ~100KB menos de memoria si nunca se usan notificaciones. Mínimo.

### 2. FPS timer apagado por defecto
**Archivo:** `VentanaFondo.cs`

Ya está implementado: `_depu.Vista.Visibility` se controla con `_verFps`. Si `_verFps = false`, el debug panel no se renderiza.

Verificar que `_tickDepu.Start()` también se condiciona a `_verFps`:

```csharp
if (_verFps)
{
    _tickDepu.Start();
    CompositionTarget.Rendering += (_, _) => _frames++;
}
```

### 3. PushCfg + PushCardCfg + PushHud paralelos (ya in-process)
**Archivo:** `AdminOverlayVm.cs`

Después de Fase 1, estos son llamadas directas (no async). Pasan de:

```csharp
await PushCfg();
await PushAllCardCfg();
await PushHud();
```

A:

```csharp
// Ya no es necesario, son sincrónicas. Pero si se necesita, las 3 son casi instantáneas.
```

### 4. LightingEngine — keepalive opcional
**Archivo:** `LightingEngine.cs`

El keepalive (re-escribe color cada 5s) es necesario para controllers que se resetean. Se puede mantener como está. Opcionalmente agregar flag para desactivarlo en settings.

### 5. Simplify DI (opcional, riesgo medio)
**Archivo:** `App.xaml.cs`

Reemplazar `Host.CreateDefaultBuilder()` (trae consigo `IConfiguration`, `ILoggerFactory`, health checks, etc.) por `new ServiceCollection()` + `ServiceProvider` directo.

Beneficio: ~100-200ms menos en startup.
Riesgo: `Serilog.Extensions.Hosting` depende de `IHost`. Habría que cambiar a `Serilog.Sinks.Console` + `Serilog.Sinks.File` directo.

```csharp
var services = new ServiceCollection();
services.AddSingleton<IAppSettingsService, AppSettingsService>();
// ... resto de servicios
var provider = services.BuildServiceProvider();
_host = provider; // o similar
```

Requiere testeo exhaustivo de todos los servicios.

---

## Notas de Arquitectura

### ¿Qué sigue siendo IPC-free después de Fase 1?
- **RGB:** ✅ Directo (LightingEngine → HID via HidSharp)
- **Overlay:** ✅ Directo (VentanaFondo en mismo proceso)
- **HID:** ✅ Directo (HidService → HidSharp)
- **Batería:** ✅ Directo (SdlBatteryService + HidDirectBatteryService)
- **Notificaciones:** ✅ Directo (NotificationService in-process)
- **Named pipes:** ❌ Eliminados completamente

### Dependencias entre fases
- **Fase 1** no depende de Fase 2 ni 3 → se puede hacer primero
- **Fase 2** depende de Fase 1 (overlay start cambió) → hacer después de Fase 1
- **Fase 3** es independiente (excepto punto 3 que depende de Fase 1)

### Versionado
- `3.2.0` = Fase 1 (overlay in-process + HidDiagnostics off)
- `3.2.1` o `3.3.0` = Fase 2 (startup paralelo)
- `3.2.2` o `3.3.1` = Fase 3 (micro-optimizaciones)
