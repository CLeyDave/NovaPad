# Plan de cambios — NovaPad v3.3.7

## 1. Reloj 12h sin AM/PM
**Archivo:** `src/NovaPad.WPF/Overlay/Partes/PanelExtendido.cs`
**Línea 102:**
```csharp
// ANTES:
_relojTexto.Text = DateTime.Now.ToString("h:mm:ss tt", new System.Globalization.CultureInfo("es-MX"));
// DESPUÉS:
_relojTexto.Text = DateTime.Now.ToString("h:mm:ss");
```

---

## 2. Fix Media Controls (SMTC directo)
### 2a. Cambiar TFM en csproj
**Archivo:** `src/NovaPad.WPF/NovaPad.WPF.csproj`
```xml
<!-- Línea 4: ANTES -->
<TargetFramework>net8.0-windows</TargetFramework>
<!-- Línea 4: DESPUÉS -->
<TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
```
Agregar después de `<ApplicationIcon>` (línea 11):
```xml
<SupportedOSPlatformVersion>7.0</SupportedOSPlatformVersion>
```

### 2b. Reescribir PanelControlMultimedia.cs
**Archivo:** `src/NovaPad.WPF/Overlay/Partes/PanelControlMultimedia.cs`
**Reemplazar TODO el contenido** con la versión que usa `using Windows.Media.Control;` directamente (sin reflexión). El código completo está en el mensaje adjunto o se puede generar con:
- Usar `GlobalSystemMediaTransportControlsSessionManager.RequestAsync()` directo
- Eventos: `MediaPropertiesChanged`, `PlaybackInfoChanged`, `TimelinePropertiesChanged`
- `props.Thumbnail.OpenReadAsync()` + `stream.AsStreamForRead()` para miniatura
- Mantener toda la UI igual (botones, barra de progreso, etc.)

---

## 3. Guide button abre overlay
**Archivo:** `src/NovaPad.WPF/ViewModels/AdminOverlayVm.cs`

### 3a. Agregar campo `_prevGuideStates` y timer/throttle
Después de `private bool _starting;` (línea 27):
```csharp
private readonly Dictionary<string, bool> _prevGuideStates = new();
private readonly DispatcherTimer _inputTimer = new();
```

### 3b. En el constructor, después de `_controllers.ControllerUpdated += ...` (línea 125):
```csharp
_controllers.InputReceived += (_, state) =>
{
    if (state.Guide && !_prevGuideStates.GetValueOrDefault(state.ControllerId))
        Dispatcher.CurrentDispatcher.Invoke(() => _overlay.TogglePanel());
    _prevGuideStates[state.ControllerId] = state.Guide;
};
```

---

## 4. Batería PC en PanelExtendido
**Archivo:** `src/NovaPad.WPF/Overlay/Partes/PanelExtendido.cs`

### 4a. Agregar P/Invoke y campos
Después de `public class PanelExtendido` agregar:
```csharp
[DllImport("kernel32.dll")]
private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS sps);

[StructLayout(LayoutKind.Sequential)]
private struct SYSTEM_POWER_STATUS
{
    public byte ACLineStatus;
    public byte BatteryFlag;
    public byte BatteryLifePercent;
    public byte Reserved1;
    public int BatteryLifeTime;
    public int BatteryFullLifeTime;
}
```

Agregar campos:
```csharp
private readonly TextBlock _pcBatteryTexto;
private readonly DispatcherTimer _batteryTimer;
```

### 4b. En el constructor, después de `_relojTexto` crear:
```csharp
_pcBatteryTexto = new TextBlock
{
    FontSize = 11,
    Foreground = DisenadorColores.CrearTextoSecundario(),
    HorizontalAlignment = HorizontalAlignment.Center,
    Margin = new Thickness(0, 2, 0, 8)
};
_batteryTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
_batteryTimer.Tick += (_, _) => RefrescarBateriaPC();
_batteryTimer.Start();
RefrescarBateriaPC();
```
Agregar `_pcBatteryTexto` al pila después de `_relojTexto`:
```csharp
pila.Children.Add(_relojTexto);
pila.Children.Add(_pcBatteryTexto);  // <-- NUEVO
```

### 4c. Método RefrescarBateriaPC():
```csharp
private void RefrescarBateriaPC()
{
    if (GetSystemPowerStatus(out var sps))
    {
        var nivel = sps.BatteryLifePercent;
        var cargando = sps.ACLineStatus == 1;
        if (nivel > 100) nivel = 100;
        var icono = cargando ? "⚡" : (nivel <= 20 ? "🪫" : "🔋");
        _pcBatteryTexto.Text = $"{icono} PC: {nivel}%{(cargando ? " (cargando)" : "")}";
    }
}
```

### 4d. En Detener(), agregar:
```csharp
_batteryTimer.Stop();
```

---

## 5. Fix Input Visualizer
### 5a. Fix Y invertido y clamp en TarjetaMando.cs
**Archivo:** `src/NovaPad.WPF/Overlay/Partes/TarjetaMando.cs`

**Líneas 688-691 — AplicarInputVisualizer:**
```csharp
// ANTES:
var sw = _stickIzq!.ActualWidth > 0 ? _stickIzq.ActualWidth : 50 * _escala;
var sh = _stickIzq.ActualHeight > 0 ? _stickIzq.ActualHeight : 50 * _escala;
var cx = sw / 2; var cy = sh / 2; var radio = (sw / 2 - 7 * _escala);
Canvas.SetLeft(_dotIzq!, Math.Max(0, cx + datos.Lx * radio - 4 * _escala));
Canvas.SetTop(_dotIzq!, Math.Max(0, cy - datos.Ly * radio - 4 * _escala));
Canvas.SetLeft(_dotDer!, Math.Max(0, cx + datos.Rx * radio - 4 * _escala));
Canvas.SetTop(_dotDer!, Math.Max(0, cy - datos.Ry * radio - 4 * _escala));

// DESPUÉS:
var sw = _stickIzq!.ActualWidth > 0 ? _stickIzq.ActualWidth : 50 * _escala;
var sh = _stickIzq.ActualHeight > 0 ? _stickIzq.ActualHeight : 50 * _escala;
var cx = sw / 2; var cy = sh / 2; var radio = (sw / 2 - 7 * _escala);
var halfDot = _dotIzq!.ActualWidth > 0 ? _dotIzq.ActualWidth / 2 : 4 * _escala;
var clampX = sw - _dotIzq.ActualWidth;
var clampY = sh - _dotIzq.ActualHeight;
Canvas.SetLeft(_dotIzq!, Math.Clamp(cx + datos.Lx * radio - halfDot, 0, clampX));
Canvas.SetTop(_dotIzq!, Math.Clamp(cy + datos.Ly * radio - halfDot, 0, clampY));
Canvas.SetLeft(_dotDer!, Math.Clamp(cx + datos.Rx * radio - halfDot, 0, clampX));
Canvas.SetTop(_dotDer!, Math.Clamp(cy + datos.Ry * radio - halfDot, 0, clampY));
```

> **⚠️ Cambio clave:** `cy - datos.Ly * radio` → `cy + datos.Ly * radio` (Y invertido arreglado).  
> `Math.Max(0, ...)` → `Math.Clamp(..., 0, clampX/Y)` (clamp completo).  
> `4 * _escala` → `halfDot` dinámico.

### 5b. InputReceived timer en AdminOverlayVm.cs
**Archivo:** `src/NovaPad.WPF/ViewModels/AdminOverlayVm.cs`

**Agregar campo:**
```csharp
private readonly DispatcherTimer _hudTimer = new();
private DateTime _ultimoPushHud = DateTime.MinValue;
```

**En el constructor, después de los eventos existentes:**
```csharp
_controllers.InputReceived += (_, _) =>
{
    var now = DateTime.UtcNow;
    if ((now - _ultimoPushHud).TotalMilliseconds >= 33) // ~30fps
    {
        _ultimoPushHud = now;
        PushHudIfActive();
    }
};
```

---

## 6. Build y verificación
```powershell
dotnet build src/NovaPad.WPF/NovaPad.WPF.csproj
```
