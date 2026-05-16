# NovaPad — Documentación del Proyecto

> Versión actual: **3.1.6**

---

## Índice

1. [Resumen del Proyecto](#1-resumen-del-proyecto)
2. [Arquitectura General](#2-arquitectura-general)
3. [Estructura del Código](#3-estructura-del-código)
4. [Flujo de Comunicación](#4-flujo-de-comunicación)
5. [Funcionalidades Actuales](#5-funcionalidades-actuales)
6. [Análisis de Soporte Multi-Mando](#6-análisis-de-soporte-multi-mando)
7. [Plan de Cambios](#7-plan-de-cambios)
8. [Sistema de Versionado](#8-sistema-de-versionado)
9. [Registro de Cambios (CHANGELOG)](#9-registro-de-cambios-changelog)

---

## 1. Resumen del Proyecto

**NovaPad** es una aplicación de escritorio para Windows (WPF, .NET 8) que permite:

- Detectar y monitorear mandos de videojuegos (DualSense, DualShock 4, Xbox, Switch Pro, Joy-Con, Steam, genéricos HID)
- Controlar iluminación RGB en mandos Sony (DualSense y DS4)
- Mostrar un HUD en tiempo real mediante una superposición transparente (Overlay)
- Visualizar batería, latencia, frecuencia de sondeo y más
- Diagnosticar dispositivos HID
- Probar mandos mediante WebView2

### Stack Tecnológico

| Componente | Tecnología |
|---|---|
| **Lenguaje** | C# 12 |
| **Framework** | .NET 8.0 |
| **UI** | WPF con ModernWpfUI (Fluent Design) |
| **Patrón** | MVVM con CommunityToolkit.Mvvm (source generators) |
| **DI** | Microsoft.Extensions.Hosting |
| **HID** | HidSharp + SDL2 |
| **IPC** | Named Pipes (JSON) |
| **Logging** | Serilog (archivos rotativos) |
| **Gráficas** | LiveChartsCore.SkiaSharpView |
| **Serialización** | Newtonsoft.Json + System.Text.Json |
| **Temas** | ModernWpfUI (Oscuro/Claro + color acento) |
| **Overlay** | Proceso WPF independiente con ventana transparente |

---

## 2. Arquitectura General

```
┌─────────────────────────────────────────────────────────────┐
│                    NovaPad.WPF (Main)                       │
│  ┌───────────┐ ┌──────────┐ ┌──────────┐ ┌─────────────┐  │
│  │ ViewModels │ │ Services │ │ Views    │ │ Converters  │  │
│  │ (MVVM)     │ │ (DI)     │ │ (XAML)   │ │             │  │
│  └─────┬─────┘ └────┬─────┘ └────┬─────┘ └──────┬──────┘  │
│        │             │            │              │          │
│  ┌─────┴─────────────┴────────────┴──────────────┴──────┐  │
│  │              App.xaml.cs (Host, DI, Init)             │  │
│  │         - Serilog init                                │  │
│  │         - Single-instance Mutex                       │  │
│  │         - Register all services                       │  │
│  │         - Start overlay (IniciadorCapa)               │  │
│  │         - Auto-apply saved RGB states                 │  │
│  └───────────────────────┬──────────────────────────────┘  │
│                          │                                  │
│  ┌───────────────────────┴──────────────────────────────┐  │
│  │              RemitenteRed (Named Pipe Client)         │  │
│  │         Envia EnvoltorioRed (JSON) → Overlay         │  │
│  └───────────────────────┬──────────────────────────────┘  │
└──────────────────────────┼──────────────────────────────────┘
                           │ Pipe: "NovaPadOverlay"
┌──────────────────────────┼──────────────────────────────────┐
│  ┌───────────────────────┴──────────────────────────────┐  │
│  │              ReceptorPipe (Named Pipe Server)         │  │
│  │         Recibe EnvoltorioRed → Clasificador          │  │
│  └───────────────────────┬──────────────────────────────┘  │
│                          │                                  │
│  ┌───────────────────────┴──────────────────────────────┐  │
│  │              Clasificador (Message Router)            │  │
│  │  DatosConfigOverlay → AplicarConfig()                │  │
│  │  InformeMandos → ActualizarHud()                     │  │
│  │  EstadoConexion → Notificaciones                     │  │
│  │  ComprobacionAviso → Test notification               │  │
│  │  CambioTema → Theme update                           │  │
│  └───────────────────────┬──────────────────────────────┘  │
│                          │                                  │
│  ┌───────────────────────┴──────────────────────────────┐  │
│  │              VentanaFondo (Overlay Window)            │  │
│  │  ┌──────────┐ ┌──────────┐ ┌─────────┐ ┌─────────┐  │  │
│  │  │Tarjetas  │ │Burbujas  │ │PanelExt │ │Depurac  │  │  │
│  │  │(mandos)  │ │(notifs)  │ │(reloj+) │ │(FPS)    │  │  │
│  │  └──────────┘ └──────────┘ └─────────┘ └─────────┘  │  │
│  └──────────────────────────────────────────────────────┘  │
│                 NovaPad.Overlay                            │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    NovaPad.HID                              │
│  ┌────────────────┐ ┌────────────────┐ ┌────────────────┐  │
│  │ HidService     │ │ SdlBattery     │ │ AggregBattery  │  │
│  │ (HidSharp)     │ │ (SDL2 bindings)│ │ (strategy)     │  │
│  └────────────────┘ └────────────────┘ └────────────────┘  │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    NovaPad.Core (Shared)                    │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐  │
│  │ Enums    │ │ Models   │ │ Events   │ │ Interfaces   │  │
│  └──────────┘ └──────────┘ └──────────┘ └──────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

---

## 3. Estructura del Código

### 3.1 NovaPad.Core (`src/NovaPad.Core/`)

Librería compartida que contiene modelos, enumeraciones, eventos e interfaces.

```
NovaPad.Core/
├── Enums/
│   ├── ControllerType.cs      — DualSense, DS4, Xbox360/One/SeriesX, SwitchPro, JoyCon, Steam, GenericHid, Unknown
│   ├── ConnectionType.cs      — Usb, Bluetooth, WirelessAdapter, Wired
│   ├── ButtonType.cs          — Todos los botones + Gyro/Accelerometer
│   └── LogLevel.cs
├── Events/
│   ├── BatteryChangedEvent.cs — Datos de cambio de batería
│   ├── CambioTema.cs          — Cambio de tema (oscuro/claro)
│   ├── ComprobacionAviso.cs   — Payload para test de notificación
│   ├── DatosConfigOverlay.cs  — Configuración global del overlay
│   ├── EnvoltorioRed.cs       — Envoltorio IPC genérico (Tag + Cuerpo JSON)
│   ├── EstadoConexion.cs      — Estado de conexión de un mando
│   ├── InformeMandos.cs       — Lista de InfoMando para el HUD
│   └── ProfileChangedEvent.cs — Cambio de perfil
├── Interfaces/
│   ├── IAppSettingsService.cs
│   ├── IBatteryService.cs
│   ├── IControllerManagerService.cs
│   ├── IControllerNamingService.cs
│   ├── IInputProcessingService.cs
│   ├── ILightingService.cs
│   ├── INavigationService.cs
│   ├── INotificationService.cs
│   └── IThemeService.cs
├── Models/
│   ├── AjustesOverlay.cs      — Config de overlay persistida en settings.json
│   ├── AppSettings.cs         — Config global + RgbState por mando
│   ├── ControllerInfo.cs      — Info completa de un mando (Id, Type, Connection, Battery, etc.)
│   ├── ControllerState.cs     — Estado de entrada (botones, ejes, giroscopio, touchpad)
│   ├── LightingCommand.cs     — Comando RGB (ControllerId, Type, Effect, Colors, Priority)
│   └── ...
└── NovaPad.Core.csproj
```

### 3.2 NovaPad.HID (`src/NovaPad.HID/`)

Librería de comunicación HID con dispositivos.

```
NovaPad.HID/
├── Bindings/
│   └── SDL2.cs                — P/Invoke bindings para SDL2 (joystick, gamecontroller, haptic, power)
├── Services/
│   ├── HidService.cs          — HidSharp: enumeración, lectura de reports, battery feature reports
│   ├── HidDirectBatteryService.cs  — Batería vía reports HID directos (Sony)
│   ├── SdlBatteryService.cs   — Batería vía SDL2 (fallback genérico)
│   └── AggregateBatteryService.cs  — Estrategia: intenta HID directa, fallback SDL2
└── NovaPad.HID.csproj
```

### 3.3 NovaPad.Overlay (`src/NovaPad.Overlay/`)

Proceso WPF independiente que muestra la superposición transparente.

```
NovaPad.Overlay/
├── App.xaml / App.xaml.cs     — Entry point, Serilog, inicio de ReceptorPipe
├── VentanaFondo.xaml / .cs    — Ventana transparente full-screen, CompositionTarget.Rendering
├── Dibujo/
│   └── DisenadorColores.cs    — Brushes predefinidos (acento, texto, batería, conexión, tipo mando)
├── Enlace/
│   ├── ReceptorPipe.cs        — Named pipe server, loop asíncrono
│   ├── Clasificador.cs        — Router de mensajes por Tag
│   └── DepositoEstado.cs      — Cache de estado (última config + último informe)
└── Partes/
    ├── TarjetaMando.cs        — 8 estilos visuales para tarjetas de mando
    ├── BurbujaInfo.cs         — Burbuja de notificación animada
    ├── OrganizadorBurbujas.cs — Pool de burbujas (máx 4, cola de espera)
    ├── PanelExtendido.cs      — Panel deslizable con reloj + lista de mandos
    ├── CuadroDepuracion.cs    — Overlay de debug (FPS, tiempos)
    └── MarcadorHora.cs        — Widget de reloj analógico+digital
```

### 3.4 NovaPad.WPF (`src/NovaPad.WPF/`)

Aplicación principal de escritorio.

```
NovaPad.WPF/
├── App.xaml / App.xaml.cs     — IHost setup, DI, Serilog, Mutex, inicio overlay
├── MainWindow.xaml / .cs      — Chrome personalizado, sidebar, bandeja, atajos
├── Views/
│   ├── DashboardView.xaml     — Panel principal con gráficas LiveCharts
│   ├── ControllerListView.xaml — Lista de mandos conectados
│   ├── ControllerDetailView.xaml — Detalle + WebView2 (gamepad.e7d.io)
│   ├── BatteryView.xaml       — Batería de todos los mandos
│   ├── RGBView.xaml           — Control RGB por mando
│   ├── SettingsView.xaml      — Config global
│   ├── DeviceInfoView.xaml    — Diagnóstico detallado del dispositivo
│   └── PaginaOverlay.xaml     — Config del overlay
├── ViewModels/
│   ├── MainViewModel.cs       — VM principal (navegación, batería sidebar, notifs)
│   ├── DashboardViewModel.cs
│   ├── ControllerListViewModel.cs
│   ├── ControllerDetailViewModel.cs
│   ├── BatteryViewModel.cs
│   ├── RGBViewModel.cs        — RGB con selector de mando, efectos, colores
│   ├── SettingsViewModel.cs
│   ├── DeviceInfoViewModel.cs
│   └── AdminOverlayVm.cs      — Config overlay (global actualmente)
├── Services/
│   ├── RealControllerManagerService.cs — Manager de mandos con timers
│   ├── NavigationService.cs   — Navegación con stack
│   ├── AppSettingsService.cs  — Persistencia JSON + hot-reload
│   ├── ThemeService.cs        — Tema ModernWpfUI + color acento
│   ├── NotificationService.cs — Cola de notificaciones
│   ├── ControllerNamingService.cs — Nombres personalizados (names.json)
│   ├── InputProcessingService.cs — Dead zones, curvas, inversión por mando
│   ├── LightingService.cs     — Fachada de LightingEngine
│   ├── LightingEngine.cs      — Motor RGB a 30Hz por mando (efectos, lerp, prioridad)
│   ├── IniciadorCapa.cs       — Ciclo de vida del proceso overlay
│   ├── RemitenteRed.cs        — Named pipe client
│   ├── HidDiagnostics.cs      — Logs de diagnóstico HID
│   ├── CrashLogger.cs         — Captura de excepciones no manejadas
│   └── AggregateBatteryService.cs
├── Converters/                — 14 converters (BatteryLevelToColor, BoolToVis, HexToBrush, etc.)
├── Styles/
│   ├── Colors.xaml            — Tokens de diseño
│   ├── Animations.xaml        — Storyboards (pulse, fade, slide, glow)
│   ├── Styles.xaml            — Estilos de todos los controles
│   └── Converters.xaml        — Resource dictionary de converters
└── Helpers/
    ├── ControllerHelper.cs
    ├── FocusHelper.cs
    ├── LocalizationHelper.cs
    └── WindowHelper.cs
```

---

## 4. Flujo de Comunicación

### 4.1 IPC entre WPF y Overlay

```
WPF (RemitenteRed)                          Overlay (ReceptorPipe)
       │                                          │
       │  ── EnvoltorioRed ──────────────────────►│
       │    {                                      │
       │      "Tag": "DatosConfigOverlay",         │
       │      "Cuerpo": "{...}"                    │
       │    }                                      │
       │                                          │ Clasificador.Procesar()
       │                                          │   └── según Tag:
       │                                          │       ├─ DatosConfigOverlay → AplicarConfig()
       │                                          │       ├─ InformeMandos     → ActualizarHud()
       │                                          │       ├─ EstadoConexion    → Notificación
       │                                          │       ├─ ComprobacionAviso → Test
       │                                          │       └─ CambioTema       → Tema
```

### 4.2 Mensajes IPC Soportados

| Tag | Payload | Propósito |
|---|---|---|
| `DatosConfigOverlay` | Opacidad, escala, colores, anclajes, visibilidad | Config global del overlay |
| `InformeMandos` | Lista de `InfoMando` (cada uno con Id, Nombre, Batería, Latencia, Hz, Tipo) | Actualización del HUD |
| `EstadoConexion` | IdMando, NombreMando, Conectado, Batería | Notificación de conexión/desconexión |
| `ComprobacionAviso` | (vacío) | Probar notificación |
| `CambioTema` | Oscuro, Acento | Cambio de tema |

### 4.3 Flujo de Datos de Mandos

```
HidService (HidSharp)
    │
    ├── DeviceList.Changed → RealControllerManagerService
    │       ├── ControllerConnected (event) → MainViewModel, RGBViewModel, etc.
    │       └── ControllerDisconnected (event) → Limpieza
    │
    ├── ReadLoopAsync (por dispositivo)
    │       └── ControllerState (event) → InputProcessingService
    │               └── MainViewModel (batería sidebar)
    │
    └── AggregateBatteryService (timer 30s)
            └── BatteryChangedEvent → Overlay + Notifications
```

---

## 5. Funcionalidades Actuales

### 5.1 Detección de Mandos
- Escaneo HID con HidSharp, reintentos hasta 5 veces (5s entre intentos) para Bluetooth
- Tipos detectados: DualSense, DS4, Xbox 360/One/Series X, Switch Pro, Joy-Con, Steam, Genérico HID
- Conexiones: USB, Bluetooth, Adaptador Inalámbrico, Cableado

### 5.2 Monitoreo de Batería
- **HID Directa** (Sony): Feature reports específicos para DualSense y DS4
- **SDL2 Fallback**: `SDL_JoystickPowerLevel` para otros mandos
- **Estrategia Agregada**: Intenta HID directa primero, fallback SDL2
- Timers: batería cada 30s, escaneo cada 5s, auto-escaneo cada 10s

### 5.3 Control RGB
- **Mandos soportados**: DualSense (VID 0x054C, PID 0x0CE6-0x0E0B) y DS4 (VID 0x054C, PID 0x05C4-0x0BA0)
- **Efectos**: Static, Pulse, Rainbow, BatteryLevel, Strobe, Heartbeat, Fire, Disco, Wave, Meteor, Aurora, Storm
- **Motor**: 30Hz tick, interpolación (lerp) en 6 frames, keep-alive cada 5s, 3 fallos = desconexión
- **Prioridad**: Critical > Notification > Battery > User
- **Per-controller**: Cada `DeviceContext` es independiente por `ControllerId`
- **Persistencia**: `RgbState` dictionary en `settings.json`

### 5.4 Overlay HUD
- Ventana transparente full-screen, siempre al frente
- 8 estilos de tarjeta: Clásico, Moderno, Mini, Terminal, Minimal, SoloBatería, CompactoVertical, Glasmórfico
- Notificaciones animadas (fade-in, scale-in, auto-dismiss)
- Panel extendido (Ctrl+Shift+O) con reloj + lista de mandos
- Debug overlay (FPS, tiempos de render)
- Anclajes: 9 posiciones (TopLeft, TopRight, Center, BottomLeft, etc.)

### 5.5 Dashboard
- Cards LiveCharts: mandos conectados, promedio batería, latencia, frecuencia
- Grid de mandos con estado

### 5.6 Prueba de Mandos
- WebView2 embebido con gamepad.e7d.io
- Layout de botones PlayStation local

### 5.7 Configuración
- Tema (Oscuro/Claro/Sistema) + color acento
- Idioma (Español/Inglés)
- Tamaño/posición de ventana
- Overlay: opacidad, escala, colores, anclajes, widgets
- Notificaciones: duración, umbral batería
- Nombres personalizados de mandos (names.json)

### 5.8 Diagnósticos
- Logs HID en `log/hid-diagnostics-*.log`
- Crash logs en `%APPDATA%\NovaPad\crashes\`
- Serilog: `%APPDATA%\NovaPad\logs\novapad-*.log`, `capa-*.log`
- Página de información detallada del dispositivo

---

## 6. Análisis de Soporte Multi-Mando

### 6.1 Estado Actual

| Aspecto | Estado | Detalle |
|---|---|---|
| **Identificación de mandos** | ✅ Completo | `ControllerInfo.Id` = `VID:PID:Serial`, string único |
| **Tracking de mandos** | ✅ Completo | `ConcurrentDictionary<string, ControllerInfo>` |
| **Eventos por mando** | ✅ Completo | Todos los eventos llevan `ControllerInfo.Id` |
| **RGB por mando** | ⚠️ Bug presente | `FindDevicePath()` usa solo VID/PID, no filtra por `HidDevicePath` |
| **RGB: selector UI** | ✅ Presente | ComboBox en RGBView permite elegir mando |
| **RGB: DeviceContext** | ✅ Independiente | `ConcurrentDictionary<string, DeviceContext>` en LightingEngine |
| **RGB: persistencia** | ✅ Por mando | `Dictionary<string, RgbSavedState>` en AppSettings |
| **InputProcessing por mando** | ✅ Independiente | `Dictionary<string, InputConfig>` en InputProcessingService |
| **InputProcessing: persistencia** | ❌ No persiste | Solo en memoria, se pierde al reiniciar |
| **Overlay: multi-tarjeta** | ✅ Implementado | `List<TarjetaMando>` dinámica en VentanaFondo |
| **Overlay: config por mando** | ❌ Global | `DatosConfigOverlay` sin identificador de mando |
| **Overlay: estilo por mando** | ❌ Global | Todas las tarjetas usan mismo estilo |
| **IPC: mensajes por mando** | ✅ Parcial | `InformeMandos` tiene lista de `InfoMando` con Id |
| **IPC: config por mando** | ❌ No existe | No hay mensaje para config individual |
| **Settings: por mando** | ✅ Parcial | Solo `RgbState` es por mando |

### 6.2 Bug RGB: FindDevicePath

**Archivo:** `NovaPad.WPF/Services/LightningEngine.cs:1001-1026`

```csharp
private static string? FindDevicePath(int vendorId, int productId, string controllerId)
{
    var devices = DeviceList.Local.GetHidDevices()
        .Where(d => (ushort)d.VendorID == vendorId && (ushort)d.ProductID == productId)
        .OrderByDescending(d => d.MaxInputReportLength)
        .ToList();

    return devices.FirstOrDefault()?.DevicePath;  // ← SIEMPRE devuelve el PRIMERO
}
```

**Problema:** Cuando hay dos mandos con el mismo VID/PID (ej. dos DualSense), `FindDevicePath` siempre selecciona el primer dispositivo HID que coincide. Ignora qué mando real se está configurando.

**Solución:** Filtrar también por `DevicePath` usando `ControllerInfo.HidDevicePath` (que ya se guarda al conectar el mando).

### 6.3 Overlay: Config Global

**Archivo:** `NovaPad.Core/Events/DatosConfigOverlay.cs`

```csharp
public class DatosConfigOverlay
{
    public double Opacidad { get; set; } = 0.8;
    public double Escala { get; set; } = 1.0;
    // ... todas las settings son globales
    public string ColorAcento { get; set; } = "#00BCD4";
    public string EstiloTarjeta { get; set; } = "Clasico";
    // NO hay ControllerId
}
```

**Problema:** No hay forma de tener configuraciones diferentes por mando en el overlay. Todas las tarjetas comparten el mismo color de acento, estilo, widgets visibles, etc.

---

## 7. Plan de Cambios

### Fase 1: Fix RGB FindDevicePath (v3.1.4)

**Prioridad: ALTA**

Objetivo: Que el RGB funcione correctamente cuando hay dos mandos idénticos conectados.

**Archivos a modificar:**

| Archivo | Cambio |
|---|---|
| `NovaPad.WPF/Services/LightningEngine.cs` | `GetOrOpenStream()` y `FindDevicePath()`: filtrar por `HidDevicePath` además de VID/PID |

**Detalle del cambio:**

En `GetOrOpenStream` (línea 504-528): Al seleccionar el dispositivo HID, filtrar los que coincidan con `ctx.Info.HidDevicePath` antes de ordenar por tamaño de reporte.

En `FindDevicePath` (línea 1001-1026): Agregar parámetro `string? devicePath` y filtrar por `d.DevicePath == devicePath` si no es null.

### Fase 2: Overlay por Mando (v3.1.5)

**Prioridad: ALTA**

Objetivo: Poder configurar el overlay de forma independiente para cada mando (color, estilo, widgets visibles).

**Archivos a modificar:**

| Archivo | Cambio |
|---|---|
| `NovaPad.Core/Events/DatosConfigOverlay.cs` | Agregar `ControllerId` opcional + propiedades de override por mando |
| `NovaPad.Core/Models/AjustesOverlay.cs` | Agregar `Dictionary<string, OverlayPerControllerConfig>` |
| **NUEVO:** `NovaPad.Core/Models/OverlayPerControllerConfig.cs` | Modelo: accent color, card style, toggles por mando |
| `NovaPad.Overlay/Enlace/DepositoEstado.cs` | Agregar `Dictionary<string, OverlayPerControllerConfig>` |
| `NovaPad.Overlay/Partes/TarjetaMando.cs` | `Reconfigurar()` aceptar overrides por mando |
| `NovaPad.Overlay/VentanaFondo.cs` | `ActualizarHud()` aplicar config por mando |
| `NovaPad.WPF/ViewModels/AdminOverlayVm.cs` | Agregar selector de mando + toggles individuales |
| `NovaPad.WPF/Views/PaginaOverlay.xaml` | UI para config por mando |
| `NovaPad.WPF/Services/LocalizationService.cs` | Nuevas keys de traducción |

### Fase 3: Nuevo Estilo InputVisualizer (v3.1.6)

**Prioridad: MEDIA**

Objetivo: Agregar un estilo de tarjeta que muestre los inputs en tiempo real (sticks, botones, triggers).

**Archivos a modificar:**

| Archivo | Cambio |
|---|---|
| `NovaPad.Overlay/Partes/TarjetaMando.cs` | Nuevo enum + constructor + método Apply para `InputVisualizer` |
| `NovaPad.Core/Events/InformeMandos.cs` | Agregar campos: `StickLX, StickLY, StickRX, StickRY, L2, R2, Buttons` |
| `NovaPad.WPF/ViewModels/MainViewModel.cs` | Incluir input data en `SendHudUpdateAsync` |
| `NovaPad.WPF/ViewModels/AdminOverlayVm.cs` | Incluir input data en `PushHud` |
| `NovaPad.Overlay/Dibujo/DisenadorColores.cs` | Brushes para representar inputs |
| `NovaPad.Overlay/Enlace/Clasificador.cs` | Registrar nuevo tipo si es necesario |

### Fase 4: Más Configuración y Ajustes (v3.1.7+)

**Prioridad: BAJA**

- Persistencia de InputProcessing (dead zones, curvas) por mando en `AppSettings`
- Per-controller overlay widget visibility
- Más opciones de tamaño/estilo en tarjetas
- Optimizaciones de rendimiento

---

## 8. Sistema de Versionado

### Esquema Semántico

```
v3.1.3
│ │ │
│ │ └── Patch: cambios menores, bugs, mejoras pequeñas (0-9)
│ │
│ └── Minor: nuevas funcionalidades, cambios significativos (0-?)
│
└── Major: cambios grandes, rompe compatibilidad (3)
```

### Reglas de incremento

1. **Cada cambio en el código** (bug fix, feature, mejora) incrementa el **Patch** en 1
2. Cuando Patch llega a **9**, vuelve a **0** y se incrementa **Minor** en 1
3. Cuando Minor llega a **9**, vuelve a **0** y se incrementa **Major** en 1

### Ejemplos

| Cambio | Versión |
|---|---|
| Estado actual | 3.1.4 |
| Overlay por mando | 3.1.5 |
| InputVisualizer style | 3.1.6 |
| Más ajustes | 3.1.7 |
| ... | ... |
| 7mo cambio | 3.1.9 |
| 8vo cambio | **3.2.0** |
| 9no cambio | 3.2.1 |

### Dónde actualizar la versión

| Archivo | Ruta |
|---|---|
| `Directory.Build.props` | `src/Directory.Build.props:3` — `<Version>3.1.3</Version>` |
| Este documento | `docs/DOCUMENTACION.md` — sección de versionado |
| CHANGELOG | `docs/CHANGELOG.md` — agregar entrada |

### Procedimiento para cada cambio

1. Implementar el cambio en el código
2. Registrar en CHANGELOG.md
3. Actualizar versión en `Directory.Build.props`
4. Actualizar la versión en este documento (sección 8)
5. Hacer commit con mensaje descriptivo

---

## 9. Registro de Cambios (CHANGELOG)

### 3.1.4 (Actual)

- Fix: RGB ahora selecciona el dispositivo HID correcto por `HidDevicePath` en lugar de solo VID/PID
- Esto permite controlar RGB independientemente en mandos idénticos (ej. dos DualSense)
- `FindDevicePath()` y `GetOrOpenStream()` filtran por el path exacto del dispositivo

### 3.1.5 (Planificado)

- Overlay: configuración por mando (color de acento, estilo de tarjeta, widgets visibles)
- Nuevo modelo `OverlayPerControllerConfig`
- Selector de mando en página de overlay
- IPC extendido para config individual

### 3.1.6 (Planificado)

- Nuevo estilo de tarjeta: `InputVisualizer`
- Muestra sticks, botones, triggers en tiempo real
- Datos de input incluidos en mensajes IPC

---

> Documento actualizado por última vez: 2026-05-15
