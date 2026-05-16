# Guía de Trabajo con GitHub — NovaPad

Esta guía explica paso a paso cómo publicar NovaPad en GitHub, configurar el auto-update y mantener el flujo de trabajo.

---

## 1. Crear el Repositorio en GitHub

```bash
# Desde la carpeta del proyecto
cd C:\Users\USUARIO\Documents\CODEME\Mando

# Inicializar git (si no existe)
git init

# Crear .gitignore para proyectos .NET
# (opcional: dotnet new gitignore)

# Añadir todo y hacer commit inicial
git add .
git commit -m "Initial commit: NovaPad v3.2.0"
```

Luego en GitHub.com:
1. Crear un nuevo repositorio (ej: `anomalyco/NovaPad`)
2. Seguir las instrucciones para conectar el local:

```bash
git remote add origin https://github.com/anomalyco/NovaPad.git
git branch -M main
git push -u origin main
```

---

## 2. Estructura de Releases

Las releases en GitHub deben seguir **Semantic Versioning** (`vMAJOR.MINOR.PATCH`):

| Tipo | Ejemplo | Cuándo |
|------|---------|--------|
| Major | `v4.0.0` | Cambios incompatibles |
| Minor | `v3.3.0` | Nuevas funcionalidades |
| Patch | `v3.2.1` | Bug fixes |

### Crear una Release

```bash
# Actualizar versión en src/Directory.Build.props
# <Version>3.3.0</Version>

# Commit y tag
git add src/Directory.Build.props
git commit -m "Bump version to 3.3.0"
git tag v3.3.0
git push origin main --tags
```

Luego en GitHub:
1. Ir a Repo → Releases → Create a new release
2. Seleccionar el tag `v3.3.0`
3. Título: `NovaPad v3.3.0`
4. Descripción: lista de cambios (puedes copiar de `docs/CHANGELOG.md`)
5. Adjuntar el **archivo comprimido** del build (`NovaPad.zip`)
6. Publicar release

### Estructura del ZIP de Release

```
NovaPad-v3.3.0.zip
├── NovaPad.WPF.exe
├── NovaPad.WPF.dll
├── NovaPad.Core.dll
├── NovaPad.HID.dll
├── *.dll (dependencias)
├── NovaPadIcon.png
├── settings.json  (valores por defecto)
└── ... (resto de archivos del publish)
```

Para generar el ZIP automáticamente:

```bash
# Publicar
dotnet publish src/NovaPad.WPF/NovaPad.WPF.csproj -c Release -o publish

# Comprimir
Compress-Archive -Path publish/* -DestinationPath NovaPad-v3.3.0.zip
```

---

## 3. Cómo Funciona el Auto-Update

### Arquitectura

```
NovaPad (App)
  │
  ├── UpdateService.CheckForUpdatesAsync()
  │     └── GET https://api.github.com/repos/anomalyco/NovaPad/releases/latest
  │           └── JSON: { tag_name, assets[{ browser_download_url }] }
  │
  ├── UpdateService.DownloadUpdateAsync()
  │     └── GET { browser_download_url }
  │           └── Guarda en %TEMP%/NovaPad_update.zip
  │
  └── UpdateService.InstallUpdate()
        └── Extrae a %TEMP%/NovaPad_update/
              ├── Busca NovaPad.Updater.exe → lo ejecuta con args
              └── O busca setup.exe / install.exe → lo ejecuta
```

### Configuración

En `UpdateService.cs` (líneas 40-42):

```csharp
private const string Owner = "anomalyco";    // Cambiar a tu usuario/org
private const string Repo = "NovaPad";       // Cambiar a tu repo
```

### Estados del UpdateService

| Estado | Descripción | Botones visibles |
|--------|-------------|------------------|
| `Idle` | Inactivo | "Buscar actualizaciones" |
| `Checking` | Consultando API | — |
| `Available` | Nueva versión detectada | "Descargar" |
| `Downloading` | Descargando ZIP | — |
| `ReadyToInstall` | Descarga completa | "Instalar" |
| `UpToDate` | Ya tienes la última versión | "Buscar actualizaciones" |
| `Error` | Falló la operación | "Buscar actualizaciones" |

### Seguridad

- El `User-Agent` HTTP se establece como `NovaPad` (requerido por GitHub API)
- La descarga usa `HttpCompletionOption.ResponseHeadersRead` para streaming
- No se ejecuta código del ZIP sin extracción previa
- El instalador se lanza con `UseShellExecute = true` (permisos de usuario)

---

## 4. Flujo de Trabajo Diario

### Desarrollo en ramas

```bash
# Crear rama para nueva feature
git checkout -b feature/mi-feature

# Trabajar... commits...
git add .
git commit -m "Descripción del cambio"

# Al terminar, fusionar con main
git checkout main
git merge feature/mi-feature
git branch -d feature/mi-feature
```

### Antes de cada release

1. Verificar que todo compila:
   ```bash
   dotnet build src/NovaPad.WPF/NovaPad.WPF.csproj -c Release
   ```

2. Verificar que no hay errores:
   ```bash
   dotnet build src/NovaPad.WPF/NovaPad.WPF.csproj 2>&1 | Select-String "error"
   # Debe devolver 0 resultados
   ```

3. Actualizar `docs/CHANGELOG.md` con los cambios

4. Actualizar versión en `src/Directory.Build.props`

5. Hacer commit, tag y push (ver sección 2)

6. Crear release en GitHub con el ZIP adjunto

---

## 5. Publicar con GitHub Actions (Automático)

Crear `.github/workflows/build.yml`:

```yaml
name: Build and Release

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 8.0.x
    
    - name: Restore
      run: dotnet restore src/NovaPad.sln
    
    - name: Build
      run: dotnet build src/NovaPad.WPF/NovaPad.WPF.csproj -c Release --no-restore
    
    - name: Publish
      run: dotnet publish src/NovaPad.WPF/NovaPad.WPF.csproj -c Release -o publish --no-build
    
    - name: Package
      run: Compress-Archive -Path publish/* -DestinationPath NovaPad-${{ github.ref_name }}.zip
    
    - name: Release
      uses: softprops/action-gh-release@v2
      with:
        files: NovaPad-${{ github.ref_name }}.zip
        body_path: docs/CHANGELOG.md
```

---

## 6. Resolución de Problemas Comunes

### GitHub API rate limiting
La API pública de GitHub tiene un límite de 60 requests/hora para IPs no autenticadas. Si el update checker falla, puede ser por rate limiting. Solución: añadir token de autenticación en headers:

```csharp
// En UpdateService.cs, constructor:
// _http.DefaultRequestHeaders.Authorization = 
//     new AuthenticationHeaderValue("Bearer", "TOKEN");
```

### El ZIP de release no se descarga correctamente
- Verificar que el asset esté bien subido en la release de GitHub
- Verificar que `browser_download_url` en el JSON apunte al asset correcto
- Verificar que el nombre del asset coincida con lo que espera el UpdateService

### La app no se actualiza porque los archivos están en uso
- El EXE principal no puede reemplazarse a sí mismo mientras se ejecuta
- Solución actual: usar un instalador externo (setup.exe)
- Solución futura: crear `NovaPad.Updater.exe` que espera a que la app termine y luego reemplaza
