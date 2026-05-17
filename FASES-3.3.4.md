# FASES-3.3.4.md — Plan de correcciones NovaPad

## FASE 1 — 🔧 Core HID (archivos: 2)
- [x] 1a `LightingEngine.cs:597` — `report[31] = 0x01` (RGB DualSense funcional)
- [x] 1b `LightingEngine.cs:608-615,672-679` — eliminar triple write redundante
- [x] 1c `LightingEngine.cs:58` — `IsSupported` solo Sony
- [x] 1d `HidService.cs:165,234` — pasar `_cts.Token` a `ReadLoopAsync`

## FASE 2 — 🧵 Threading & Memory (archivos: 3)
- [ ] 2a `VentanaFondo.cs:192-201` — `_tickDepu.Stop()` antes de nullear
- [ ] 2b `RealControllerManagerService.cs:92` — log de error en StartAsync
- [ ] 2c `RealControllerManagerService.cs:25-26` — cachear ConnectedControllers
- [ ] 2d `HidService.cs:444` — `HasBattery = true` para Xbox

## FASE 3 — ⚙️ Settings & Performance (archivos: 3)
- [ ] 3a `SettingsViewModel.cs:130-143` — debounce 500ms en AutoSave
- [ ] 3b `InputProcessingService.cs:96-105` — no persistir defaults vacíos
- [ ] 3c `App.xaml.cs:249-255` — fix `overlayTask = Task.CompletedTask`

## FASE 4 — 🧹 Code Cleanup (archivos: 5)
- [ ] 4a `RGBViewModel.cs:96-98` — eliminar dead code
- [ ] 4b `BatteryViewModel.cs:112` — eliminar PropertyChanged redundante
- [ ] 4c `MainViewModel.cs:198,260` — `UtcNow` en vez de `Now`
- [ ] 4d `MainViewModel.cs:84-91` — `nameof(...)` en navigation
- [ ] 4e `VentanaFondo.cs:32` y `AjustesOverlay.cs:25` — alinear defaults anclaje

## FASE 5 — 🏷️ Version + Release (archivos: 4)
- [ ] 5a `Directory.Build.props` → 3.3.4
- [ ] 5b `NovaPad.iss` → 3.3.4
- [ ] 5c `UpdateService.cs` → v3.3.4 fallbacks
- [ ] 5d Build → Publish → Installer → `gh release create`
