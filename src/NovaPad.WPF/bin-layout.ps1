param([string]$PublishDir)

$binDir = Join-Path $PublishDir "bin"
New-Item -ItemType Directory -Path $binDir -Force | Out-Null

function Safe-Move-File {
    param([string]$Path, [string]$DestinationDir)
    $dest = Join-Path $DestinationDir (Split-Path -Leaf $Path)
    for ($i = 0; $i -lt 5; $i++) {
        try {
            Move-Item -LiteralPath $Path -Destination $dest -Force -ErrorAction Stop
            return
        } catch {
            if ($i -ge 4) { return }
            Start-Sleep -Milliseconds 200
        }
    }
}

function Safe-Move-Dir {
    param([string]$Path, [string]$DestinationDir)
    $name = Split-Path -Leaf $Path
    $dest = Join-Path $DestinationDir $name
    if (Test-Path -LiteralPath $dest) { Remove-Item -LiteralPath $dest -Recurse -Force }
    for ($i = 0; $i -lt 5; $i++) {
        try {
            Move-Item -LiteralPath $Path -Destination $DestinationDir -Force -ErrorAction Stop
            return
        } catch {
            if ($i -ge 4) { return }
            Start-Sleep -Milliseconds 200
        }
    }
}

# --- Determine which DLLs come from NuGet packages (vs framework) ---
$depsPath = Join-Path $PublishDir "NovaPad.WPF.deps.json"
$nugetDlls = @{}  # hashtable: "filename.dll" -> $true

if (Test-Path -LiteralPath $depsPath) {
    $deps = Get-Content -LiteralPath $depsPath -Raw | ConvertFrom-Json
    $targets = $deps.targets
    # Pick the RID-specific target (one with a "/" in the name)
    $rid = $targets.PSObject.Properties.Name | Where-Object { $_ -like '*/*' } | Select-Object -First 1
    if ($rid -and $targets.$rid) {
        foreach ($pkg in $targets.$rid.PSObject.Properties) {
            $pkgName = $pkg.Name -replace '/.*$', ''  # "Serilog/4.2.0" -> "Serilog"
            # Framework runtime packages start with "runtimepack."
            $isFramework = $pkgName -like 'runtimepack.*'
            if ($pkg.Value.runtime) {
                foreach ($rt in $pkg.Value.runtime.PSObject.Properties) {
                    $dllName = Split-Path -Leaf $rt.Name  # "lib/net8.0/Serilog.dll" -> "Serilog.dll"
                    if ($dllName -and !$isFramework) { $nugetDlls[$dllName] = $true }
                }
            }
        }
    }
}

# App's own assemblies stay in root
$appDlls = @('NovaPad.WPF.dll', 'NovaPad.Overlay.dll', 'NovaPad.Core.dll', 'NovaPad.HID.dll')

# NuGet assemblies go to bin (only those identified in deps.json)
Get-ChildItem -LiteralPath $PublishDir -Filter *.dll | ForEach-Object {
    if ($nugetDlls.ContainsKey($_.Name) -and $_.Name -notin $appDlls) {
        Safe-Move-File -Path $_.FullName -DestinationDir $binDir
    }
}

# Move .pdb, .xml, and other non-essential files from root
Get-ChildItem -LiteralPath $PublishDir | Where-Object {
    !$_.PSIsContainer -and $_.Extension -ne '.exe' -and $_.Extension -ne '.dll' -and
    $_.Name -notlike '*.runtimeconfig.json' -and $_.Name -notlike '*.deps.json'
} | ForEach-Object {
    Safe-Move-File -Path $_.FullName -DestinationDir $binDir
}

# Move non-bin folders from root (runtimes, es, etc.)
Get-ChildItem -LiteralPath $PublishDir | Where-Object { $_.PSIsContainer -and $_.Name -ne 'bin' } | ForEach-Object {
    Safe-Move-Dir -Path $_.FullName -DestinationDir $binDir
}

# Remove empty leftover dirs
Get-ChildItem -LiteralPath $PublishDir -Directory | Where-Object { $_.Name -ne 'bin' } | ForEach-Object {
    $remaining = Get-ChildItem -LiteralPath $_.FullName -Recurse | Measure-Object | Select-Object -ExpandProperty Count
    if ($remaining -eq 0) { Remove-Item -LiteralPath $_.FullName -Recurse -Force }
}

# Patch runtimeconfig.json to add probingPaths for NuGet assemblies
@('NovaPad.WPF.runtimeconfig.json', 'NovaPad.Overlay.runtimeconfig.json') | ForEach-Object {
    $path = Join-Path $PublishDir $_
    if (Test-Path -LiteralPath $path) {
        $content = Get-Content -LiteralPath $path -Raw
        if ($content -notmatch 'probingPaths') {
            $content = $content -replace '"runtimeOptions":\s*\{', '"runtimeOptions": { "probingPaths": ["bin"],'
            Set-Content -LiteralPath $path -Value $content -NoNewline
        }
    }
}
