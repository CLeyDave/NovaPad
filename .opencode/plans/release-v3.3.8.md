# Plan v3.3.8 — Release + AutoStart + Fixes

## Cambios a implementar

### 1. Version bump → 3.3.8
**Archivos:**
- `src/Directory.Build.props` — `3.3.7` → `3.3.8`
- `NovaPad.iss` — `3.3.7` → `3.3.8`
- `src/NovaPad.WPF/Services/UpdateService.cs` — fallbacks v3.3.7 → v3.3.8

### 2. Reloj 12h sin AM/PM
**Archivo:** `src/NovaPad.WPF/Overlay/Partes/PanelExtendido.cs`
```csharp
// Línea 102: ANTES
_relojTexto.Text = DateTime.Now.ToString("h:mm:ss tt", new System.Globalization.CultureInfo("es-MX"));
// DESPUÉS
_relojTexto.Text = DateTime.Now.ToString("h:mm:ss");
```

### 3. Fix Media Controls — WinRT directo
**3a. Cambiar TFM en `src/NovaPad.WPF/NovaPad.WPF.csproj`:**
```xml
<TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
<SupportedOSPlatformVersion>7.0</SupportedOSPlatformVersion>
```

**3b. Reescribir `src/NovaPad.WPF/Overlay/Partes/PanelControlMultimedia.cs`**
Usar `using Windows.Media.Control;` directamente:
- `GlobalSystemMediaTransportControlsSessionManager.RequestAsync()`
- Eventos: `MediaPropertiesChanged`, `PlaybackInfoChanged`, `TimelinePropertiesChanged`
- `props.Thumbnail.OpenReadAsync()` + `.AsStreamForRead()` para miniatura
- Todo el resto de la UI (botones, barra de progreso) se mantiene igual

### 4. Guide button → abre overlay
**Archivo:** `src/NovaPad.WPF/ViewModels/AdminOverlayVm.cs`
Agregar:
```csharp
private readonly Dictionary<string, bool> _prevGuideStates = new();
```
En constructor, después de eventos existentes:
```csharp
_controllers.InputReceived += (_, state) =>
{
    if (state.Guide && !_prevGuideStates.GetValueOrDefault(state.ControllerId))
        Dispatcher.CurrentDispatcher.Invoke(() => _overlay.TogglePanel());
    _prevGuideStates[state.ControllerId] = state.Guide;
};
```
Agregar timer de InputReceived para HUD en tiempo real:
```csharp
private readonly DispatcherTimer _hudTimer = new();
private DateTime _ultimoPushHud = DateTime.MinValue;
```
```csharp
_controllers.InputReceived += (_, _) =>
{
    var now = DateTime.UtcNow;
    if ((now - _ultimoPushHud).TotalMilliseconds >= 33)
    {
        _ultimoPushHud = now;
        PushHudIfActive();
    }
};
```

### 5. Batería PC en PanelExtendido
**Archivo:** `src/NovaPad.WPF/Overlay/Partes/PanelExtendido.cs`
Agregar P/Invoke `GetSystemPowerStatus`, `TextBlock` para mostrar batería, `DispatcherTimer` cada 5s.
Insertar después del reloj en el StackPanel.

### 6. Fix Input Visualizer
**Archivo:** `src/NovaPad.WPF/Overlay/Partes/TarjetaMando.cs` — líneas 688-691
```csharp
// ANTES (Y invertido + clamp parcial):
Canvas.SetTop(_dotIzq!, Math.Max(0, cy - datos.Ly * radio - 4 * _escala));

// DESPUÉS (Y corregido + clamp completo):
var halfDot = _dotIzq!.ActualWidth > 0 ? _dotIzq.ActualWidth / 2 : 4 * _escala;
Canvas.SetTop(_dotIzq!, Math.Clamp(cy + datos.Ly * radio - halfDot, 0, sw - _dotIzq!.ActualWidth));
```

### 7. [NUEVO] AutoStart overlay toggle
**7a. Modelo — `src/NovaPad.Core/Models/AjustesOverlay.cs`**
Agregar propiedad:
```csharp
public bool AutoStart { get; set; } = true;
```

**7b. ViewModel — `AdminOverlayVm.cs`**
Agregar:
```csharp
[ObservableProperty] private bool _autoStart = true;
```

En `RefreshSettings()`:
```csharp
AutoStart = s.Overlay.AutoStart;
```

En `SaveGlobal()`:
```csharp
s.Overlay.AutoStart = AutoStart;
```

En `StartLayerAsync()`, reemplazar:
```csharp
_settings.Settings.Overlay.Activado = true;
```
con:
```csharp
_settings.Settings.Overlay.AutoStart = AutoStart;
_settings.Settings.Overlay.Activado = true;
```

En `StopLayer()`, mantener solo:
```csharp
_settings.Settings.Overlay.Activado = false;
```
(Sin tocar AutoStart)

**7c. View — `PaginaOverlay.xaml`**
Agregar toggle en sección GENERAL:
```xml
<Grid Margin="0,0,0,5" Visibility="{Binding IsPerController, Converter={StaticResource BoolToVisInverted}}">
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

**7d. App.xaml.cs** — cambio en autostart check:
```csharp
// ANTES:
if (settings.Settings.Overlay.Activado)
// DESPUÉS:
if (settings.Settings.Overlay.AutoStart)
```

---

## Release v3.3.8 — Comandos (ejecutar en orden)

```powershell
# 1. Build Release
dotnet publish src/NovaPad.WPF/NovaPad.WPF.csproj -c Release -o publish

# 2. Reorganizar bin layout
powershell -File src/NovaPad.WPF/bin-layout.ps1 -PublishDir ./publish

# 3. Crear instalador (Inno Setup 6)
iscc NovaPad.iss

# 4. Publicar en GitHub
gh release create v3.3.8 NovaPad-Setup-v3.3.8.exe --title "v3.3.8" --notes "## Novedades v3.3.8

### Nuevo
- Boton central del mando (Guide/PS/Xbox) abre el panel de control del overlay
- Panel de control multimedia con Spotify/Chrome integrado
- Bateria del PC visible en el panel de control
- Opcion para activar/desactivar auto-start del overlay

### Mejoras
- Input Visualizer ahora funciona correctamente (sticks, DPad, bumpers)
- Reloj en formato 12h
- Overlay no aparece en Alt+Tab

### Correcciones
- Eje Y del Input Visualizer invertido corregido
- Media Controls ahora accede a SMTC via WinRT directo
- HUD del overlay se actualiza en tiempo real con InputReceived"
```

---

## Resumen de archivos modificados

| Archivo | Cambio |
|---------|--------|
| `src/Directory.Build.props` | Version 3.3.8 |
| `NovaPad.iss` | Version 3.3.8 |
| `src/NovaPad.WPF/Services/UpdateService.cs` | Fallbacks v3.3.8 |
| `src/NovaPad.WPF/NovaPad.WPF.csproj` | TFM → `net8.0-windows10.0.19041.0`, + `SupportedOSPlatformVersion` |
| `src/NovaPad.WPF/Overlay/Partes/PanelExtendido.cs` | Reloj 12h + Batería PC |
| `src/NovaPad.WPF/Overlay/Partes/PanelControlMultimedia.cs` | Reescribir usando `Windows.Media.Control` |
| `src/NovaPad.WPF/Overlay/Partes/TarjetaMando.cs` | Fix Y invertido + clamp completo |
| `src/NovaPad.WPF/Overlay/VentanaFondo.cs` | (ya tiene WS_EX_TOOLWINDOW) |
| `src/NovaPad.WPF/ViewModels/AdminOverlayVm.cs` | Guide button + HUD timer + AutoStart |
| `src/NovaPad.WPF/Views/PaginaOverlay.xaml` | AutoStart toggle |
| `src/NovaPad.Core/Models/AjustesOverlay.cs` | AutoStart property |
| `src/NovaPad.WPF/App.xaml.cs` | AutoStart check |
