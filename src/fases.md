# NovaPad Overlay — Fases de Desarrollo

## FASE 1 — Overlay Básico

- Ventana transparente siempre encima de todo (WS_EX_TOPMOST, WS_EX_LAYERED, WS_EX_TRANSPARENT, WS_EX_TOOLWINDOW)
- Click-through: no bloquea clicks, no roba foco
- Render básico con CompositionTarget.Rendering
- Proyecto separado `NovaPad.Overlay` como proceso independiente (NovaPad.Overlay.exe)
- Named Pipe IPC entre overlay y app principal
- Event Bus simple para publicación/consumo de eventos
- Capa base: negro transparente con rounded corners

## FASE 2 — Notificaciones + Eventos

- Sistema de notificaciones flotantes animadas (fade, slide)
- Duración 2-4 segundos, stack inteligente (se apilan sin superponerse)
- Pool de objetos para reutilizar widgets/notificaciones (no crear nuevas cada frame)
- Eventos del Core: DeviceConnectedEvent, DeviceDisconnectedEvent, BatteryChangedEvent, ProfileChangedEvent
- Animaciones suaves (GPU accelerated)
- Plantillas de notificación: conexión, desconexión, batería baja, cambio de perfil

## FASE 3 — HUD Persistente

- Overlay pequeño en esquina con:
  - Batería (verde >70%, amarillo 30-70%, rojo <30%)
  - Latencia
  - Nombre del control
  - Estado de conexión
  - Polling rate (125/250/500/1000 Hz)
  - Perfil activo
- Capas: Layer 1 (HUD mínimo), Layer 2 (notificaciones)
- HUD con blur/acrylic background, diseño tipo Xbox Game Bar
- Actualización por eventos, no por polling

## FASE 4 — Plugins + Widgets + Themes

- Sistema de plugins modular
- Widgets intercambiables (FPS counter, micrófono, hora, etc.)
- Themes con colores, fondos, animaciones configurables
- APIs para desarrolladores externos
- Registro dinámico de widgets en el overlay

## FASE 5 — Overlay Expandido + Integraciones

- Overlay expandible con hotkey (CTRL + SHIFT + O)
- Panel completo con información detallada
- Browser widgets embebidos (WebView2 en overlay)
- Discord Rich Presence
- OBS widgets
- Integración con streaming
- Capas 3 y 4: overlay expandido + debug overlay (FPS, input latency)
