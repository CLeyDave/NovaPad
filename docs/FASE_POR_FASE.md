# NovaPad — Plan Fase por Fase

## Fase 1: Overlay In-Process ✅
**Objetivo**: Eliminar `NovaPad.Overlay.exe` como proceso separado y ejecutar el overlay dentro del proceso WPF principal.

### Cambios
- Nueva interfaz `IOverlayService` en `NovaPad.Core`
- `VentanaFondo.cs` implementa `IOverlayService` directamente
- Eliminados archivos del proyecto overlay externo (`RemitenteRed.cs`, `IniciadorCapa.cs`, `EnvoltorioRed.cs`, `ReceptorPipe.cs`, `Clasificador.cs`, `DepositoEstado.cs`)
- Namespaces migrados de `NovaPad.Overlay.*` a `NovaPad.WPF.Overlay.*`
- Comunicación por llamadas directas a métodos (sin named pipe, sin JSON, sin retries)

### Impacto
- Startup del overlay: ~500-5000ms → ~5ms
- Menos procesos en memoria
- Código más simple y mantenible

---

## Fase 2: Startup Paralelo ✅
**Objetivo**: Que la ventana principal aparezca lo antes posible, difiriendo tareas pesadas a background.

### Cambios
- Construcción del host DI + resolución de servicios
- `_host.StartAsync()` en fire-and-forget
- Carga de settings
- `controllerService.StartDetectionAsync()` (ahora event-driven, instantáneo)
- `mainWindow.Show()` — ventana visible inmediatamente
- `Task.Run` para: `ScanForControllersAsync()`, restauración RGB, auto-start overlay

### Impacto
- Ventana aparece ~300-800ms antes que en v3.1.x
- Usuario puede interactuar con la UI mientras se completan tareas en segundo plano

---

## Fase 3: Detección Event-Driven ✅
**Objetivo**: Reemplazar los dos bucles de polling redundantes (300ms cada uno) por detección basada en eventos del sistema operativo.

### Cambios
- `HidService`: reemplazado `MonitorDevicesAsync` (polling cada 300ms) por WMI `ManagementEventWatcher` con `Win32_DeviceChangeEvent`
- `DiffScan()`: escaneo completo único al recibir evento WMI (con debounce de 200ms)
- `SafetyScanAsync()`: escaneo de seguridad cada 30s (vs 300ms antes)
- `RealControllerManagerService`: eliminado `ScanLoopAsync` (segundo bucle de 300ms) — ya no necesario porque `HidService` dispara eventos confiables
- Nueva dependencia: `System.Management` 8.0.0

### Impacto
- CPU idle → prácticamente cero (antes: dos hilos haciendo polling cada 300ms)
- Detección instantánea de conexión/desconexión de mandos
- Build time reducido: ~5s → ~1.8s

---

## Fase 4: Fixes y Revival UI ✅
**Objetivo**: Corregir todos los recursos rotos, dependencias muertas, problemas de localización y mejorar la interfaz.

### Cambios
- **Broken resources**: `CardBackgroundBrush` y `AccentBrush` en `PaginaOverlay.xaml` reemplazados por `SurfaceBrush` y `PrimaryBrush`
- **ControllerState indexer**: añadidas entradas faltantes para `ButtonType.Gyro`, `.Accelerometer`, `.Custom`
- **Localization**: añadida clave `AppTitle` faltante en español
- **Deps eliminadas**: `Newtonsoft.Json` (Core + WPF), `ModernWpfUI`, `LiveChartsCore.SkiaSharpView.WPF` — todas sin uso en el código
- **SettingsView**: mejorada la sección "Acerca de" con información de versión

---

## Fase 5: Auto-Update System ✅
**Objetivo**: Sistema de actualización automática conectado a GitHub Releases.

### Cambios
- Nuevo servicio `UpdateService` en `NovaPad.WPF.Services`
- Conexión a API REST de GitHub: `api.github.com/repos/{owner}/{repo}/releases/latest`
- Modelos `GitHubRelease`, `GitHubAsset` para deserializar JSON
- Estados: `Idle → Checking → Available → Downloading → ReadyToInstall → UpToDate/Error`
- Tres comandos en `SettingsViewModel`: `CheckForUpdates`, `DownloadUpdate`, `InstallUpdate`
- UI en `SettingsView.xaml` con: estado, barra de progreso, botones contextuales
- Extracción ZIP y lanzamiento de instalador automático

### Pendiente para versiones futuras
- Crear `NovaPad.Updater.exe` para reemplazar archivos en uso
- Auto-check al inicio con notificación
- Verificación de firma digital
- Delta updates (solo archivos cambiados)

---

## Fase 6: Documentación 📝
**Objetivo**: Documentar el plan de desarrollo y el flujo de trabajo con GitHub.

### Archivos creados
- `docs/FASE_POR_FASE.md` — Este archivo
- `docs/GUIADETRABAJO_GITHUB.md` — Guía paso a paso para publicar en GitHub

---

## Próximas Fases (Ideas)

### Fase 7: Mejora de Estilos
- Refinar tema claro/oscuro
- Agregar variantes de acento (más colores predefinidos)
- Mejorar responsive de las vistas

### Fase 8: Perfiles de Mando
- Guardar/cargar configuraciones completas por perfil
- Asignar perfiles a juegos específicos

### Fase 9: Overlay Mejorado
- Editor visual de HUD (arrastrar y soltar)
- Más estilos de tarjeta y notificación
- Widgets personalizables

### Fase 10: Updater Nativo
- `NovaPad.Updater.exe` como proyecto separado
- Reemplazo de archivos en uso con script de reinicio
- Verificación de integridad (hash SHA-256)
