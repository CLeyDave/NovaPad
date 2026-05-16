# Changelog

## 3.2.0 (2026-05-15)

### Optimization
- **Overlay in-process**: Eliminado `NovaPad.Overlay.exe` como proceso separado.
- **Startup paralelo**: Ventana principal se muestra inmediatamente después de construir el host + cargar settings. `ScanForControllersAsync()`, restauración RGB e inicio del overlay se ejecutan en background (`Task.Run`). La ventana aparece ~300-800ms antes que en v3.1.x.
- **Detección event-driven**: Eliminados los dos bucles de polling redundantes (HidService cada 300ms + RealControllerManagerService cada 300ms). Reemplazados por WMI `ManagementEventWatcher` (eventos `Win32_DeviceChangeEvent`) + escaneo de seguridad cada 30s. La detección es ahora instantánea y la huella en idle es prácticamente cero. Nueva dependencia: `System.Management` 8.0.0. `VentanaFondo` ahora vive en el mismo proceso WPF. Las llamadas al overlay son directas (sin named pipe, sin JSON, sin retries de conexión). Startup del overlay pasa de 500-5000ms a ~5ms.
- **HidDiagnostics**: Eliminado `HidDiagnostics.RunAsync()` del startup de `LightingEngine`. Ya no se enumeran todos los dispositivos HID en cada inicio.
- **Fix broken resources**: `PaginaOverlay.xaml` tenía dos `DynamicResource` rotos (`CardBackgroundBrush`, `AccentBrush`). Reemplazados por `SurfaceBrush` y `PrimaryBrush`.
- **ControllerState indexer**: Faltaban 3 entradas `ButtonType` (`Gyro`, `Accelerometer`, `Custom`). Corregido.
- **Localization**: Añadida clave `AppTitle` faltante en diccionario español.
- **Deps limpias**: Eliminados paquetes NuGet no utilizados: `Newtonsoft.Json` (Core+WPF), `ModernWpfUI`, `LiveChartsCore.SkiaSharpView.WPF`.
- **Auto-update system**: Nuevo `UpdateService` conectado a GitHub Releases API. Botones "Buscar actualizaciones", "Descargar" e "Instalar" en Settings. Descarga con barra de progreso, extracción automática y lanzamiento del instalador.
- **Versión bump**: 3.2.0 (minor).

### Breaking Changes (internos)
- Eliminados: `NovaPad.Overlay.exe`, `NovaPad.Overlay.csproj`, `RemitenteRed.cs`, `IniciadorCapa.cs`, `EnvoltorioRed.cs`, `ReceptorPipe.cs`, `Clasificador.cs`, `DepositoEstado.cs`.
- Nueva interfaz `IOverlayService` en NovaPad.Core — `VentanaFondo` la implementa; `AdminOverlayVm`, `MainViewModel` y `SettingsViewModel` la usan.
- Namespaces de partes overlay cambiaron de `NovaPad.Overlay.*` a `NovaPad.WPF.Overlay.*`.

## 3.1.9 (2026-05-15)

### Bug Fix
- **Apilamiento vertical de tarjetas**: Tarjetas en la misma posición ahora se apilan verticalmente una debajo de otra (gap 6px) en lugar de superponerse exactamente.

## 3.1.8 (2026-05-15)

### Feature
- **Posición por mando**: Cada tarjeta ahora tiene posición independiente (anclaje + desvío X/Y) configurable por mando. Se guarda en `OverlayCardConfig.AnclajeHud/DesvioX/DesvioY/Escala`.
- **Escalado InputVisualizer**: Sticks, gatillos, botones y fuentes del estilo InputVisualizer ahora se escalan correctamente con el factor de escala global.
- **Tarjetas individuales en Canvas**: El overlay reemplazó el StackPanel compartido por posicionamiento individual de cada tarjeta directamente en el Canvas, permitiendo que cada mando tenga su propia posición en pantalla.

## 3.1.7 (2026-05-15)

### Feature
- **Persistencia de InputProcessing**: `InputProcessingConfig` ahora se persiste en `settings.json` por mando. `InputProcessingService` usa `IAppSettingsService` para cargar/guardar configuraciones (deadzone, curva, sensibilidad, inversión) en lugar del diccionario volátil anterior.
- **InputProcessing integrado en overlay**: `AdminOverlayVm.PushHud()` aplica `ProcessRawInput()` a cada mando antes de enviar los datos al overlay, por lo que el InputVisualizer refleja sticks/gatillos procesados (deadzone, curvas, etc.).

## 3.1.6 (2026-05-15)

### Feature
- **Nuevo estilo "InputVisualizer"**: Estilo de tarjeta que muestra en tiempo real la posición de los sticks (L/R), nivel de gatillos (L2/R2) con barras de progreso, y estado de los botones frontales (A/B/X/Y) con indicadores visuales.
- `InfoMando` extendido con datos de input: `Lx`, `Ly`, `Rx`, `Ry` (sticks, -1..1), `L2`, `R2` (gatillos, 0..1), y botones `BtnA`, `BtnB`, `BtnX`, `BtnY`, `BtnUp`, `BtnDown`, `BtnLeft`, `BtnRight`, `BtnLB`, `BtnRB`.
- `AdminOverlayVm.PushHud()` ahora obtiene el estado actual (`GetCurrentState`) de cada mando y lo envía al overlay, permitiendo visualización en tiempo real.

## 3.1.5 (2026-05-15)

### Feature
- **Overlay configurable por mando**: Un solo overlay con tarjetas independientes para cada mando, cada una con su propio color de acento, fondo, estilo de tarjeta y visibilidad de elementos. Desde la página de ajustes del overlay se puede seleccionar un mando específico y personalizar su tarjeta sin afectar a los demás.
- Nuevo evento IPC `OverlayCardConfig` para enviar configuración individual de tarjeta desde WPF al overlay.
- `AjustesOverlay.PorMando` — diccionario persistente de configuraciones por `ControllerId`.
- Vista combinada Global/per-controller en la UI: el combo selector "Configurar mando" permite alternar entre modo Global y configuración individual.

## 3.1.4 (2026-05-15)

### Fix
- **RGB en mandos idénticos**: `FindDevicePath()` y `GetOrOpenStream()` ahora filtran por `HidDevicePath` ademas de VID/PID. Esto permite controlar RGB independientemente en dos mandos iguales (ej. dos DualSense) sin que los comandos se escriban al mando equivocado.
