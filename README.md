# NovaPad

Aplicacion de escritorio para Windows que permite detectar, monitorear y personalizar mandos de videojuegos. Soporta DualSense, DualShock 4, Xbox, Switch Pro, Joy-Con, Steam y mandos genericos HID.

## Que hace

- Detecta mandos conectados por USB o Bluetooth
- Muestra bateria, latencia y frecuencia de sondeo en tiempo real
- Controla iluminacion RGB en mandos Sony (DualSense y DS4) con varios efectos
- Overlay transparente en pantalla completa con HUD personalizable
- Panel de control que se abre con Ctrl+Shift+O (reloj, lista de mandos, estado)
- Notificaciones de conexion, desconexion y bateria baja
- Dashboard con graficas de estado general
- Pagina de prueba de mandos con WebView2
- Configuracion por mando: color de acento, estilo de tarjeta, posicion, widgets visibles
- Input processing: deadzone, curvas, sensibilidad, inversion (persistente por mando)
- Tema oscuro/claro y color de acento personalizable
- Auto-update via GitHub Releases

## Tecnologia

- C# 12 / .NET 8.0
- WPF con MVVM (CommunityToolkit.Mvvm)
- Inyeccion de dependencias (Microsoft.Extensions.Hosting)
- HidSharp + SDL2 para comunicacion HID
- Serilog para logs rotativos
- System.Text.Json para serializacion

## Estructura del proyecto

```
src/
  NovaPad.Core/         Modelos, enumeraciones, eventos e interfaces compartidas
  NovaPad.HID/          Comunicacion HID con dispositivos (HidSharp, SDL2)
  NovaPad.WPF/          Aplicacion principal (UI, ViewModels, Servicios, Overlay)
    Overlay/            VentanaFondo transparente, tarjetas, panel, notificaciones
    Views/              Paginas XAML (Dashboard, Mandos, RGB, Overlay, Settings...)
    ViewModels/         Logica MVVM de cada pagina
    Services/           Servicios DI (settings, input processing, RGB, update...)
```

## Versionado

Se usa versionado semantico: vMAJOR.MINOR.PATCH

- Patch: bugs, cambios menores
- Minor: funcionalidades nuevas
- Major: cambios incompatibles

La version actual se define en `src/Directory.Build.props`.

## Historial de versiones

### 3.2.0 (2026-05-15)

Overlay movido dentro del proceso principal de WPF. Ya no hay un NovaPad.Overlay.exe separado. La comunicacion es directa por llamadas a metodos, sin named pipes ni JSON. El overlay arranca en ~5ms en lugar de 500-5000ms.

La ventana principal aparece inmediatamente despues de construir la app. El escaneo de mandos, la restauracion RGB y el inicio del overlay se ejecutan en segundo plano. La ventana se ve ~300-800ms antes.

La deteccion de mandos ahora usa eventos del sistema (WMI) en lugar de dos bucles de polling cada 300ms. El consumo de CPU en reposo es practicamente cero.

Se agregaron estilos de tarjeta: InputVisualizer (sticks, gatillos y botones en tiempo real). Cada tarjeta tiene posicion independiente configurable.

Corregidos recursos rotos en PaginaOverlay.xaml. Eliminadas dependencias sin usar (Newtonsoft.Json, ModernWpfUI, LiveChartsCore).

Sistema de auto-update conectado a GitHub Releases con progreso de descarga e instalacion.

### 3.1.9

Tarjetas en la misma posicion ahora se apilan verticalmente con separacion de 6px en lugar de superponerse.

### 3.1.8

Cada tarjeta tiene posicion independiente (anclaje + desvio) y factor de escala propio. El overlay reemplazo el StackPanel compartido por posicionamiento individual en el Canvas.

### 3.1.7

InputProcessingConfig ahora se guarda en settings.json por mando (deadzone, curva, sensibilidad, inversion). El overlay aplica procesamiento de input a los datos antes de mostrarlos.

### 3.1.6

Nuevo estilo de tarjeta InputVisualizer que muestra en tiempo real la posicion de sticks, nivel de gatillos con barras de progreso y estado de botones. El overlay recibe datos de input desde AdminOverlayVm.

### 3.1.5

Overlay configurable por mando: cada tarjeta tiene su propio color de acento, fondo, estilo y visibilidad de elementos. Selector de mando en la pagina de ajustes del overlay.

### 3.1.4

RGB ahora selecciona el dispositivo HID correcto filtrando por el path exacto del dispositivo, ademas de VID/PID. Permite controlar RGB independientemente en dos mandos identicos (ej. dos DualSense).

## Como compilar

```powershell
dotnet build src/NovaPad.WPF/NovaPad.WPF.csproj
```

## Como ejecutar

```powershell
dotnet run --project src/NovaPad.WPF/NovaPad.WPF.csproj
```

O abrir la carpeta en Visual Studio y presionar F5.

## Como publicar una release

1. Actualiza la version en `src/Directory.Build.props`
2. Compila en Release:
   ```powershell
   dotnet publish src/NovaPad.WPF/NovaPad.WPF.csproj -c Release -o publish
   ```
3. Comprime la carpeta publish:
   ```powershell
   Compress-Archive -Path publish/* -DestinationPath NovaPad-vX.X.X.zip
   ```
4. Crea un tag en git:
   ```powershell
   git tag vX.X.X
   git push origin --tags
   ```
5. En GitHub: ve a Releases, crea una nueva release con el tag, adjunta el ZIP y publica.

## Auto-update

La app puede buscar actualizaciones en GitHub Releases. Desde Settings, usa "Buscar actualizaciones" para consultar la ultima version. Si hay una nueva, puedes descargarla e instalarla. La descarga muestra barra de progreso. Al instalar, busca un setup.exe dentro del ZIP o un instalador.

Para que funcione, las releases en GitHub deben tener un asset ZIP con el build completo. La configuracion del repo (owner/name) esta en `NovaPad.WPF/Services/UpdateService.cs`.

## Flujo de trabajo con git

Se recomienda usar ramas para cada cambio:

```powershell
git checkout -b feature/mi-cambio
# trabajar, commitear...
git checkout main
git merge feature/mi-cambio
git branch -d feature/mi-cambio
```

Antes de cada release, verificar que compila sin errores, actualizar CHANGELOG, version y crear tag.

## Notas

- Los logs se guardan en %APPDATA%/NovaPad/logs/
- Los crashes se guardan en %APPDATA%/NovaPad/crashes/
- La configuracion se persiste en settings.json junto al ejecutable
- Los nombres personalizados de mandos van en names.json
