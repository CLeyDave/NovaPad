# Plan v3.3.8 — Release Completa

## Resumen de cambios

### 1. Version bump (todos los archivos)
| Archivo | Línea | Cambio |
|---------|-------|--------|
| `src/Directory.Build.props` | 3 | `3.3.7` → `3.3.8` |
| `NovaPad.iss` | 5 | `3.3.7` → `3.3.8` |
| `src/NovaPad.WPF/Services/UpdateService.cs` | 111 | `v3.3.7` → `v3.3.8` |
| `src/NovaPad.WPF/Services/UpdateService.cs` | 115 | `v3.3.7` → `v3.3.8` |

---

### 2. Reloj 12h sin AM/PM
**Archivo:** `src/NovaPad.WPF/Overlay/Partes/PanelExtendido.cs`, línea 114
```csharp
// ANTES:
_relojTexto.Text = DateTime.Now.ToString("h:mm:ss tt", new System.Globalization.CultureInfo("es-MX"));
// DESPUÉS:
_relojTexto.Text = DateTime.Now.ToString("h:mm:ss");
```

---

### 3. Fix Media Controls — WinRT directo
**3a. TFM change en `src/NovaPad.WPF/NovaPad.WPF.csproj`:**
```xml
<!-- línea 4: ANTES -->
<TargetFramework>net8.0-windows</TargetFramework>
<!-- línea 4: DESPUÉS -->
<TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
```
Agregar después de `<ApplicationIcon>icon.ico</ApplicationIcon>` (línea 11):
```xml
<SupportedOSPlatformVersion>7.0</SupportedOSPlatformVersion>
```

**3b. Reescribir `PanelControlMultimedia.cs`** — eliminar todo el código con reflexión y usar `using Windows.Media.Control;` directamente:
- Namespace `Windows.Media.Control` disponible por el TFM nuevo
- Reemplazar `Type.GetType(...)`, `GetMethod(...)`, `GetProperty(...)`, `dynamic` con llamadas directas
- `GlobalSystemMediaTransportControlsSessionManager.RequestAsync()` → `await` directo
- `session.MediaPropertiesChanged += handler` directo
- `session.TryGetMediaPropertiesAsync()` → `await` directo
- `thumbnail.OpenReadAsync()` → `await` directo, usar `IRandomAccessStreamReference`
- Mantener IDÉNTICA la UI: `_tituloTexto`, `_artistaTexto`, `_miniatura`, `_barraProgresoNivel`, `_tiempoTexto`, `_btnAnterior`, `_btnPlayPause`, `_btnSiguiente`
- Mantener `CambiarAcento(hexAcento)` y `Detener()` sin cambios

---

### 4. Guide button → toggle overlay panel
**Archivo:** `src/NovaPad.WPF/ViewModels/AdminOverlayVm.cs`

**4a. Agregar campo** después de línea 27 (`private bool _starting;`):
```csharp
private readonly Dictionary<string, bool> _prevGuideStates = new();
```

**4b. Suscribir input** después de línea 125 (`_controllers.ControllerUpdated`):
```csharp
_controllers.InputReceived += (_, state) =>
{
    if (state.Guide && !_prevGuideStates.GetValueOrDefault(state.ControllerId))
        System.Windows.Application.Current.Dispatcher.Invoke(() => _overlay.TogglePanel());
    _prevGuideStates[state.ControllerId] = state.Guide;
};
```

---

### 5. Input Visualizer — tiempo real + fix Y
**5a. Fix Y invertido + clamp** en `TarjetaMando.cs`, líneas 717-723:
```csharp
// ANTES:
var halfDot = 4 * _escala;
Canvas.SetLeft(_dotIzq!, Math.Max(0, cx + datos.Lx * radio - halfDot));
Canvas.SetTop(_dotIzq!, Math.Max(0, cy - datos.Ly * radio - halfDot));
Canvas.SetLeft(_dotDer!, Math.Max(0, cx + datos.Rx * radio - halfDot));
Canvas.SetTop(_dotDer!, Math.Max(0, cy - datos.Ry * radio - halfDot));

// DESPUÉS:
var halfDot = _dotIzq!.ActualWidth > 0 ? _dotIzq.ActualWidth / 2 : 4 * _escala;
var clampX = sw - _dotIzq!.ActualWidth;
var clampY = sh - _dotIzq!.ActualHeight;
Canvas.SetLeft(_dotIzq!, Math.Clamp(cx + datos.Lx * radio - halfDot, 0, clampX));
Canvas.SetTop(_dotIzq!, Math.Clamp(cy + datos.Ly * radio - halfDot, 0, clampY));
Canvas.SetLeft(_dotDer!, Math.Clamp(cx + datos.Rx * radio - halfDot, 0, clampX));
Canvas.SetTop(_dotDer!, Math.Clamp(cy + datos.Ry * radio - halfDot, 0, clampY));
```

> **⚠️ Cambio clave:** `cy - datos.Ly * radio` → `cy + datos.Ly * radio` (corrige Y invertido)

**5b. Throttle InputReceived** en `AdminOverlayVm.cs` al final del constructor (~línea 129):
```csharp
var _lastPush = DateTime.MinValue;
_controllers.InputReceived += (_, _) =>
{
    var now = DateTime.UtcNow;
    if ((now - _lastPush).TotalMilliseconds >= 33)
    {
        _lastPush = now;
        PushHudIfActive();
    }
};
```

---

### 6. Batería PC en PanelExtendido
**Archivo:** `src/NovaPad.WPF/Overlay/Partes/PanelExtendido.cs`

**6a. P/Invoke** después de `using NovaPad.WPF.Overlay.Dibujo;`:
```csharp
using System.Runtime.InteropServices;
```

**6b. Agregar struct y método** dentro de `class PanelExtendido`:
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

**6c. Campo `_pcBatteryTexto` y timer** después de línea 36:
```csharp
private readonly TextBlock _pcBatteryTexto;
private readonly DispatcherTimer _batteryTimer;
```

**6d. En el constructor**, después de `_temporizador.Start();`:
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

**6e. Insertar en `pila`** después de `_relojTexto` (línea 81):
```csharp
pila.Children.Add(_pcBatteryTexto);
```

**6f. Método `RefrescarBateriaPC()`:**
```csharp
private void RefrescarBateriaPC()
{
    if (GetSystemPowerStatus(out var sps))
    {
        var nivel = sps.BatteryLifePercent;
        if (nivel > 100) nivel = 100;
        var cargando = sps.ACLineStatus == 1;
        var icono = cargando ? "⚡" : (nivel <= 20 ? "🪫" : "🔋");
        _pcBatteryTexto.Text = $"{icono} PC: {nivel}%{(cargando ? " (cargando)" : "")}";
    }
}
```

**6g. En `Detener()`, agregar:**
```csharp
_batteryTimer.Stop();
```

---

### 7. AutoStart toggle en PaginaOverlay (siempre visible, modo global)
**7a. Modelo** `src/NovaPad.Core/Models/AjustesOverlay.cs`, agregar después de `Activado` (línea 8):
```csharp
public bool AutoStart { get; set; } = true;
```

**7b. ViewModel** `AdminOverlayVm.cs`, agregar propiedad después de línea 27:
```csharp
[ObservableProperty] private bool _autoStart = true;
```

**7c. En `RefreshSettings()`** (línea 221):
```csharp
AutoStart = s.Overlay.AutoStart;
```

**7d. Agregar `OnAutoStartChanged`:**
```csharp
partial void OnAutoStartChanged(bool value) { if (_loading) return; SaveGlobal(); }
```

**7e. En `SaveGlobal()`**, agregar:
```csharp
s.Overlay.AutoStart = AutoStart;
```

**7f. `App.xaml.cs`** línea 189:
```csharp
// ANTES:
if (settings.Settings.Overlay.Activado)
// DESPUÉS:
if (settings.Settings.Overlay.AutoStart)
```

**7g. View** `PaginaOverlay.xaml`, dentro de la card "GENERAL" (líneas 110-127), **al final del StackPanel**, antes de cerrar `</Border>`:
```xml
<Grid Margin="0,10,0,0">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="Auto"/>
    </Grid.ColumnDefinitions>
    <TextBlock Text="Iniciar automáticamente"
               FontSize="12" Foreground="{DynamicResource TextPrimaryBrush}"/>
    <ToggleButton Grid.Column="1" IsChecked="{Binding AutoStart}"
                  Style="{StaticResource ModernToggleSwitch}"/>
</Grid>
```

---

## Release — Comandos de build

```powershell
# 1. Build Release
dotnet publish src/NovaPad.WPF/NovaPad.WPF.csproj -c Release -o publish

# 2. Reorganizar layout (dlls planos para Inno Setup)
powershell -File src/NovaPad.WPF/bin-layout.ps1 -PublishDir ./publish

# 3. Generar instalador
iscc NovaPad.iss

# 4. Subir a GitHub
gh release create v3.3.8 NovaPad-Setup-v3.3.8.exe ^
  --title "v3.3.8" ^
  --notes "## Novedades v3.3.8

### Nuevo
- Boton central del mando (Guide/PS/Xbox) abre el panel de control del overlay
- Panel de control multimedia con Spotify/Chrome integrado (WinRT SMTC directo)
- Bateria del PC visible en el panel extendido del overlay
- Opcion para activar/desactivar auto-start del overlay

### Mejoras
- Input Visualizer ahora funciona correctamente (sticks, DPad, bumpers)
- Reloj en formato 12h
- Overlay no aparece en Alt+Tab (WS_EX_TOOLWINDOW)

### Correcciones
- Eje Y del Input Visualizer invertido corregido
- Media Controls ahora accede a SMTC via WinRT directo (net8.0-windows10.0.19041.0)
- HUD del overlay se actualiza en tiempo real con InputReceived"

# 5. Verificar
gh release view v3.3.8
```

---

## Archivos modificados (resumen)

| Archivo | Cambios |
|---------|---------|
| `src/Directory.Build.props` | Version → `3.3.8` |
| `NovaPad.iss` | Version → `3.3.8` |
| `src/NovaPad.WPF/Services/UpdateService.cs` | Fallbacks → `v3.3.8` |
| `src/NovaPad.WPF/NovaPad.WPF.csproj` | TFM → `net8.0-windows10.0.19041.0`, + `<SupportedOSPlatformVersion>7.0</SupportedOSPlatformVersion>` |
| `src/NovaPad.Core/Models/AjustesOverlay.cs` | + `AutoStart` property |
| `src/NovaPad.WPF/App.xaml.cs` | `Activado` → `AutoStart` check |
| `src/NovaPad.WPF/ViewModels/AdminOverlayVm.cs` | + `AutoStart` prop, + Guide edge detection, + InputReceived throttle |
| `src/NovaPad.WPF/Views/PaginaOverlay.xaml` | + AutoStart toggle en card GENERAL |
| `src/NovaPad.WPF/Overlay/Partes/PanelExtendido.cs` | Reloj 12h, + Batería PC con P/Invoke |
| `src/NovaPad.WPF/Overlay/Partes/PanelControlMultimedia.cs` | Reescribir con WinRT directo (`Windows.Media.Control`) |
| `src/NovaPad.WPF/Overlay/Partes/TarjetaMando.cs` | Fix Y invertido, clamp completo |
| `src/NovaPad.WPF/Overlay/VentanaFondo.cs` | (sin cambios — ya tiene `WS_EX_TOOLWINDOW`) |

## Prerrequisitos para build
- .NET 8 SDK
- Inno Setup 6+ (en PATH como `iscc`)
- GitHub CLI (`gh`) autenticado
- PowerShell script `bin-layout.ps1` existente
