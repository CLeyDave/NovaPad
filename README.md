# NovaPad

Aplicación de escritorio para Windows que permite detectar, monitorear y personalizar mandos de videojuegos. Soporta DualSense, DualShock 4, Xbox, Switch Pro, Joy-Con, Steam y mandos genéricos HID.

---

## 📖 Índice

- [¿Qué hace NovaPad?](#qué-hace-novapad)
- [Cómo se ve](#cómo-se-ve)
- [Cómo funciona por dentro](#cómo-funciona-por-dentro)
  - [Arquitectura general](#arquitectura-general)
  - [Inicio de la aplicación](#inicio-de-la-aplicación)
  - [Detección de mandos](#detección-de-mandos)
  - [Overlay (superposición en juegos)](#overlay-superposición-en-juegos)
  - [Configuración por mando](#configuración-por-mando)
  - [Notificaciones](#notificaciones)
  - [Input Processing](#input-processing)
  - [Control RGB](#control-rgb)
  - [Tema oscuro/claro](#tema-oscuroclaro)
- [Auto-update (actualizaciones automáticas)](#auto-update-actualizaciones-automáticas)
  - [Cómo busca actualizaciones](#cómo-busca-actualizaciones)
  - [Cómo se descarga e instala](#cómo-se-descarga-e-instala)
  - [Cómo crear una release nueva](#cómo-crear-una-release-nueva)
- [Cómo compilar](#cómo-compilar)
- [Cómo ejecutar](#cómo-ejecutar)
- [Cómo contribuir](#cómo-contribuir)
- [Historial de versiones](#historial-de-versiones)
- [Notas técnicas](#notas-técnicas)

---

## ¿Qué hace NovaPad?

NovaPad es una herramienta todo-en-uno para jugar con mandos en PC:

- **Detecta mandos** conectados por USB o Bluetooth al instante
- **Muestra batería, latencia y frecuencia de sondeo** en tiempo real
- **Controla iluminación RGB** en mandos Sony (DualSense y DS4) con múltiples efectos
- **Overlay transparente** que se superpone a tus juegos con un HUD personalizable (batería, latencia, perfil, fps, reloj, y más)
- **Panel de control rápido** que se abre con `Ctrl+Mayús+O` dentro del overlay
- **Notificaciones** de conexión, desconexión y batería baja
- **Configuración independiente por mando**: color, estilo de tarjeta, posición, widgets visibles
- **Ajustes de input**: deadzone, curvas de respuesta, sensibilidad, inversión de ejes
- **Tema oscuro y claro** con color de acento personalizable
- **Auto-update**: se actualiza solo desde GitHub Releases

---

## Cómo funciona por dentro

### Arquitectura general

```
NovaPad.sln
├── NovaPad.Core/        ← El "cerebro" compartido: interfaces, modelos, eventos
│   ├── Interfaces/       Contratos: IOverlayService, IAppSettingsService, etc.
│   ├── Models/           Datos: AppSettings, AjustesOverlay, NotificationMessage
│   └── Events/           Eventos: DatosConfigOverlay, OverlayCardConfig, etc.
│
├── NovaPad.HID/          ← La "mano" que toca los mandos físicos
│   └── Services/         HidSharp + SDL2 para leer mandos y controlar RGB
│
└── NovaPad.WPF/          ← La "cara" que ves: ventanas, botones, overlay
    ├── Views/            Páginas XAML (Dashboard, Mandos, RGB, Overlay, Settings...)
    ├── ViewModels/       Lógica MVVM de cada página
    ├── Services/         Servicios de la app (settings, update, notificaciones...)
    ├── Overlay/          La ventana transparente y sus componentes visuales
    │   ├── VentanaFondo.cs    La ventana overlay en sí
    │   ├── TarjetaMando/      Cada tarjeta de mando en el HUD
    │   ├── Partes/            Panel, notificaciones, debug
    │   └── Dibujo/            Estilos visuales de las tarjetas
    └── Styles/           Colores, animaciones y estilos de toda la UI
```

La app usa **.NET 8 con WPF** y sigue el patrón **MVVM** con `CommunityToolkit.Mvvm`. Las dependencias se inyectan con `Microsoft.Extensions.Hosting` (como en ASP.NET, pero para escritorio).

### Inicio de la aplicación

Cuando abres NovaPad:

1. **Single instance**: Si ya hay otra instancia corriendo, la trae al frente y cierra la nueva
2. **Logger**: Arranca Serilog para escribir logs en `%APPDATA%/NovaPad/logs/`
3. **DI Container**: Construye el "mundo" de servicios con Host.CreateDefaultBuilder
4. **Carga settings**: Lee `settings.json` (está junto al .exe)
5. **Arranca detección**: Inicia el servicio que escucha conexiones de mandos
6. **Muestra ventana**: La interfaz aparece (rápido, ~300ms)
7. **En segundo plano**: Escanea mandos, restaura RGB, inicia el overlay si está configurado, y busca actualizaciones

### Detección de mandos

El sistema usa dos tecnologías juntas:

- **HidSharp**: Para comunicación directa con dispositivos HID (lectura de entrada, control RGB)
- **SDL2**: Como respaldo para detectar mandos que HidSharp no ve

Usa eventos del sistema (WMI) en lugar de un bucle de polling, así el consumo de CPU en reposo es prácticamente cero.

### Overlay (superposición en juegos)

El overlay es una **ventana transparente de pantalla completa** que se dibuja sobre tus juegos. No es una captura de pantalla ni un programa aparte: vive dentro del mismo proceso que NovaPad.

**Cómo se comunica**: No usa pipes, sockets ni archivos. Todo es por llamadas directas a métodos a través de la interfaz `IOverlayService`. El ViewModel (`AdminOverlayVm`) llama:
- `_overlay.ApplyConfig(config)` → cambia opacidad, escala, colores, etc.
- `_overlay.ApplyCardConfig(card)` → cambia configuración de un mando específico
- `_overlay.UpdateHud(data)` → envía datos en tiempo real (batería, sticks, botones)
- `_overlay.ShowNotification(title, msg)` → muestra una notificación flotante

**Partes del overlay**:
| Componente | Qué hace |
|---|---|
| `TarjetaMando` | Una tarjeta por mando con batería, latencia, perfil, sticks, gatillos |
| `PanelExtendido` | Panel con teclas rápidas Ctrl+Shift+O |
| `OrganizadorBurbujas` | Notificaciones que aparecen y desaparecen |
| `CuadroDepuracion` | FPS del overlay (solo si activas "Mostrar FPS") |

**Estilos de tarjeta**: Cada mando puede mostrarse en 10 estilos distintos: Clásico, Moderno, Mini, Terminal, Minimal, Solo Batería, Compacto, Input Visualizer, Línea, Detallado.

### Configuración por mando

Puedes tener configuraciones distintas para cada mando: color de acento, color de fondo, estilo de tarjeta, posición en pantalla, y qué elementos mostrar (batería, latencia, frecuencia, perfil, conexión, tipo).

Cuando seleccionas "Global", los cambios aplican a todos los mandos. Cuando seleccionas un mando específico, los cambios aplican solo a ese mando. La página de overlay te muestra un badge azul cuando estás en modo "por mando".

Los datos se guardan en `settings.json` bajo `Overlay.PorMando` (un diccionario por ID de mando).

### Notificaciones

NovaPad tiene **dos sistemas de notificaciones** independientes:

1. **Notificaciones en la ventana principal** (`NotificationService`): Aparecen en la interfaz de NovaPad (conexión/desconexión, battery warnings). Se muestran como tarjetas que desaparecen solas.
2. **Notificaciones en el overlay**: Cuando el overlay está activo, aparecen burbujas flotantes sobre el juego. Se usan para conexión/desconexión en caliente y la prueba de "Test Alert".

El `TestAlert` intenta mostrar la notificación en el overlay si está corriendo, y si no, la envía a la ventana principal de NovaPad.

### Input Processing

Puedes ajustar cómo se interpreta la señal de los mandos:

- **Deadzone**: Zona muerta del stick (ignora movimientos mínimos)
- **Curva**: Cómo responde el stick (lineal, cuadrática, exponencial, cúbica, agresiva)
- **Sensibilidad**: Multiplicador de la respuesta
- **Inversión**: Invertir ejes X o Y

Cada mando guarda su configuración independiente en `settings.json`.

### Control RGB

Funciona con mandos Sony (DualSense y DualShock 4):
- Varios efectos: color fijo, respiración, arcoíris, ciclo, pulso, onda
- Color primario y secundario independientes
- Presets de colores
- Se restaura automáticamente al conectar el mando

### Tema oscuro/claro

La UI completa (ventana principal, tarjetas, sidebar, botones) cambia entre tema oscuro y claro. Los colores se definen en `Styles/Colors.xaml` como recursos `DynamicResource` para que el cambio sea instantáneo.

---

## Auto-update (actualizaciones automáticas)

### Cómo busca actualizaciones

Al iniciar, NovaPad consulta **GitHub Releases** en segundo plano. El servicio `UpdateService`:
1. Obtiene la lista de releases de `CLeyDave/NovaPad` via la API de GitHub
2. Compara el tag más reciente con la versión actual
3. Si hay una versión nueva, muestra un diálogo preguntando qué hacer

La versión actual se obtiene del ensamblado (`Assembly.GetExecutingAssembly().GetName().Version`) y el tag de GitHub se define en `UpdateService.cs`.

### Cómo se descarga e instala

Cuando hay una actualización disponible, puedes elegir:
- **Descargar e instalar**: Descarga el `NovaPad-Setup-vX.X.X.exe`, lo ejecuta en modo silencioso (`/VERYSILENT /SUPPRESSMSGBOXES`), y cierra la app. El instalador (Inno Setup) reemplaza los archivos y vuelve a abrir la app.
- **Solo descargar**: Descarga el instalador pero no lo ejecuta.
- **Más tarde**: Cierra el diálogo y no hace nada.

La descarga muestra una **barra de progreso** en la página de Settings.

### Cómo crear una release nueva

1. **Actualiza la versión del assembly** en `src/Directory.Build.props` (ej: `3.3.0`)
2. **Actualiza las versiones de respaldo** en `src/NovaPad.WPF/Services/UpdateService.cs` (2 líneas con `"v3.X.X"`)
3. **Actualiza el instalador** en `NovaPad.iss` (la línea `#define MyAppVersion "3.3.0"`)
4. **Compila el publicación**:
   ```powershell
   dotnet publish src/NovaPad.WPF/NovaPad.WPF.csproj -c Release -o publish
   ```
5. **Reorganiza los archivos**:
   ```powershell
   powershell -File src/NovaPad.WPF/bin-layout.ps1 -PublishDir ./publish
   ```
6. **Crea el instalador** (necesitas Inno Setup 6):
   ```powershell
   iscc NovaPad.iss
   ```
   Esto genera `NovaPad-Setup-v3.3.0.exe`
7. **Sube a GitHub**:
   ```powershell
   gh release create v3.3.0 NovaPad-Setup-v3.3.0.exe --title "v3.3.0" --notes "## novedades"
   ```
8. El auto-update detectará la nueva versión la próxima vez que la app inicie.

> **⚠️ Importante**: La versión de la app se lee automáticamente del assembly (`Assembly.GetExecutingAssembly().GetName().Version`), que se define en `Directory.Build.props`. Se muestra en:
> - **SettingsView** → etiqueta "Versión"
> - **Sidebar** → junto al logo (propiedad `AppVersion` en `MainViewModel.cs`)
>
> Al actualizar `Directory.Build.props`, todas las vistas reflejan el cambio automáticamente.

---

## Cómo compilar

```powershell
dotnet build src/NovaPad.WPF/NovaPad.WPF.csproj -c Release
```

Para development:
```powershell
dotnet build src/NovaPad.WPF/NovaPad.WPF.csproj
```

## Cómo ejecutar

```powershell
dotnet run --project src/NovaPad.WPF/NovaPad.WPF.csproj
```

O abre la carpeta en **Visual Studio 2022** y presiona `F5`.

## Cómo contribuir

1. Crea una rama: `git checkout -b feature/mi-cambio`
2. Haz tus cambios y commitea
3. Asegúrate que compile: `dotnet build`
4. Crea un PR contra `main`

---

## Historial de versiones

### 3.3.4
- Fix: RGB DualSense ahora funciona (lightbar enable byte corregido)
- Fix: eliminadas escrituras HID redundantes (mejor rendimiento y estabilidad)
- Fix: read loops HID ahora se cancelan correctamente al desconectar
- Fix: overlay no acumula timers al reiniciarse (ResetReadyState)
- Fix: errores de HidService.StartAsync ya no se tragan silenciosamente
- Fix: configuraciones de input processing ya no se persisten automáticamente
- Fix: cache en ConnectedControllers elimina allocaciones innecesarias
- Fix: AutoSave en Settings con debounce de 500ms
- Fix: overlayTask ya no causa NullReferenceException en startup
- Fix: detección de batería en mandos Xbox habilitada
- Mejora: nombres de vistas con nameof() en lugar de strings
- Mejora: PropertyChanged duplicados eliminados
- Mejora: DateTime.UtcNow usado consistentemente
- Limpieza: dead code eliminado en RGBViewModel

### 3.3.3
- Overlay con lifecycle async correcto (TaskCompletionSource + WaitUntilReadyAsync)
- Auto-start del overlay restaurado (se inicia al abrir la app si estaba activo)
- Fix: ya no requiere dos clicks para aplicar configs al overlay
- Fix: popup de actualización con DialogResult funcional
- Overlay con 3 estados: Detenido / Iniciando... / Ejecutándose
- Código más limpio y mantenible en el arranque del overlay

### 3.3.2
- Fix: overlay presets se guardan antes de arrancar (SaveGlobal + SaveTarjeta en StartLayer)
- Fix: popup de actualización aparece correctamente (se quitó AllowsTransparency que rompía con WindowChrome)
- Fix: botones del popup setean DialogResult = true para que el flujo de descarga funcione

### 3.3.1
- Fix: se quitó el smiley emoji del título de la release
- La versión de la app ahora se lee dinámicamente del assembly (ya no está hardcodeada)

### 3.3.0
- Overlay con botón grande Iniciar/Detener en sidebar principal
- TestAlert funcional sin overlay (notificación en ventana principal)
- Escaneo de mandos optimizado (300ms + input-triggered)
- Popup de actualización rediseñado con tema de la app
- AutoStartOverlay eliminado temporalmente (overlay nunca arranca solo)
- Fix: overlay save ya no acopla Activado con settings generales
- La versión de la app se muestra en el sidebar y settings

### 3.2.9
- Fix: overlay save ya no acopla AutoStartOverlay con Overlay.Activado
- Fix: TestAlert funciona aunque el overlay no esté corriendo (notificación en ventana principal)
- Documentación completa del sistema

### 3.2.8
- Fix: overlay save/load con RefreshSettings() público
- Fix: TestAlert con fallback a INotificationService
- Estilos UI seguros (implicit property-only + keyed)

### 3.2.7
- Fix: Config ya no se queda en blanco (eliminados estilos globales implícitos con ControlTemplates)
- Fix: overlay button sincronizado via PaginaOverlay.Loaded
- Fix: hotkey Ctrl+Shift+O actualiza correctamente

### 3.2.5 – 3.2.6 (eliminados)
- Versiones con bugs críticos (botones circulares, Config no abría)

### 3.2.4
- Auto-update al inicio con diálogo Descargar/Instalar/Más tarde

### 3.2.2
- Sistema de auto-update funcional

### 3.2.0 (2026-05-15)
- Overlay movido DENTRO del proceso WPF (antes era un .exe separado)
- Comunicación directa por método, sin named pipes ni JSON
- Arranque ~5ms en lugar de 500-5000ms
- Ventana principal aparece inmediatamente (~300ms), escaneo y RGB en segundo plano
- Detección por eventos WMI en lugar de polling (CPU ~0 en reposo)
- Nuevo estilo InputVisualizer (sticks + gatillos + botones en tiempo real)
- Posición independiente por tarjeta

Versiones anteriores (3.1.x): Mejoras incrementales en overlay, RGB, input processing.

---

## Notas técnicas

| Dato | Valor |
|---|---|
| Lenguaje | C# 12 |
| Framework | .NET 8.0 (Windows) |
| UI | WPF con MVVM (CommunityToolkit.Mvvm) |
| DI | Microsoft.Extensions.Hosting |
| HID | HidSharp + SDL2 |
| Logs | Serilog → `%APPDATA%/NovaPad/logs/` |
| Config | `settings.json` junto al .exe (JSON con System.Text.Json) |
| Instalador | Inno Setup 6 → `NovaPad-Setup-vX.X.X.exe` |
| Auto-update | GitHub Releases API |
| Single instance | Mutex global + FindWindow |
| Crash reports | `%APPDATA%/NovaPad/crashes/` |
