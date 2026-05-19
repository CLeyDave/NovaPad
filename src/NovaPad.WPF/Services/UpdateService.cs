using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows;

namespace NovaPad.WPF.Services;

public class GitHubRelease
{
    [JsonPropertyName("tag_name")] public string TagName { get; set; } = "";
    [JsonPropertyName("html_url")] public string HtmlUrl { get; set; } = "";
    [JsonPropertyName("body")] public string Body { get; set; } = "";
    [JsonPropertyName("published_at")] public string PublishedAt { get; set; } = "";
    [JsonPropertyName("assets")] public List<GitHubAsset> Assets { get; set; } = new();
}

public class GitHubAsset
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("browser_download_url")] public string BrowserDownloadUrl { get; set; } = "";
    [JsonPropertyName("size")] public long Size { get; set; }
}

public enum UpdateStatus
{
    Idle,
    Checking,
    Available,
    Downloading,
    ReadyToInstall,
    UpToDate,
    Error
}

public class UpdateService : INotifyPropertyChanged
{
    private const string Owner = "CleyDave";
    private const string Repo = "NovaPad";
    private const string ApiUrl = $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest";
    private static readonly string DownloadDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NovaPad", "Updates");

    private readonly HttpClient _http;
    private string _currentVersion;
    public string CurrentVersion => _currentVersion;

    private UpdateStatus _status = UpdateStatus.Idle;
    public UpdateStatus Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); OnPropertyChanged(nameof(CanCheck)); OnPropertyChanged(nameof(CanDownload)); OnPropertyChanged(nameof(CanInstall)); }
    }

    private string _statusText = "";
    public string StatusText
    {
        get
        {
            if (!string.IsNullOrEmpty(_statusText)) return _statusText;
            return _status switch
            {
                UpdateStatus.Idle => "Presiona 'Buscar actualizaciones' para verificar.",
                UpdateStatus.Checking => "Buscando actualizaciones...",
                UpdateStatus.Available => $"Nueva versión {_latestRelease?.TagName ?? ""} disponible.",
                UpdateStatus.Downloading => $"Descargando... {_downloadProgress:F0}%",
                UpdateStatus.ReadyToInstall => "Descarga completada. Instala y reinicia.",
                UpdateStatus.UpToDate => $"Estás al día ({_currentVersion}).",
                UpdateStatus.Error => "Error al buscar actualizaciones.",
                _ => ""
            };
        }
        set { _statusText = value; OnPropertyChanged(); }
    }

    private GitHubRelease? _latestRelease;
    public GitHubRelease? LatestRelease
    {
        get => _latestRelease;
        set { _latestRelease = value; OnPropertyChanged(); }
    }

    private double _downloadProgress;
    public double DownloadProgress
    {
        get => _downloadProgress;
        set { _downloadProgress = value; OnPropertyChanged(); }
    }

    public bool CanCheck => Status is UpdateStatus.Idle or UpdateStatus.UpToDate or UpdateStatus.Error or UpdateStatus.Available;
    public bool CanDownload => Status == UpdateStatus.Available;
    public bool CanInstall => Status == UpdateStatus.ReadyToInstall;

    public UpdateService()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("NovaPad");
        _http.Timeout = TimeSpan.FromSeconds(10);
        _currentVersion = GetCurrentVersion();
    }

    private static string GetCurrentVersion()
    {
        try
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v3.3.8";
        }
        catch
        {
            return "v3.3.8";
        }
    }

    public async Task CheckForUpdatesAsync()
    {
        Status = UpdateStatus.Checking;
        StatusText = "Buscando actualizaciones...";
        LatestRelease = null;
        DownloadProgress = 0;

        try
        {
            var response = await _http.GetAsync(ApiUrl);
            response.EnsureSuccessStatusCode();

            var release = await response.Content.ReadFromJsonAsync<GitHubRelease>();
            if (release == null)
            {
                Status = UpdateStatus.Error;
                StatusText = "No se pudo obtener la información de la versión.";
                return;
            }

            LatestRelease = release;

            StatusText = "";

            if (CompareVersions(release.TagName, _currentVersion) > 0)
            {
                Status = UpdateStatus.Available;
            }
            else
            {
                Status = UpdateStatus.UpToDate;
            }
        }
        catch (Exception ex)
        {
            Status = UpdateStatus.Error;
            StatusText = $"Error: {ex.Message}";
        }
    }

    public async Task DownloadUpdateAsync()
    {
        if (LatestRelease == null || LatestRelease.Assets.Count == 0)
        {
            Status = UpdateStatus.Error;
            StatusText = "No hay archivos para descargar.";
            return;
        }

        var asset = LatestRelease.Assets.FirstOrDefault(a => a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                   ?? LatestRelease.Assets[0];
        Directory.CreateDirectory(DownloadDir);
        var downloadPath = Path.Combine(DownloadDir, $"NovaPad_Setup_{LatestRelease.TagName}.exe");

        Status = UpdateStatus.Downloading;

        try
        {
            using var response = await _http.GetAsync(asset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? asset.Size;
            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[81920];
            long bytesRead = 0;
            int read;
            while ((read = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read));
                bytesRead += read;
                DownloadProgress = totalBytes > 0 ? (double)bytesRead / totalBytes * 100 : 0;
            }

            Status = UpdateStatus.ReadyToInstall;
            StatusText = "Descarga completada. Instala y reinicia.";
        }
        catch (Exception ex)
        {
            Status = UpdateStatus.Error;
            StatusText = $"Error al descargar: {ex.Message}";
        }
    }

    public void InstallUpdate()
    {
        var tag = LatestRelease?.TagName ?? "latest";
        var downloadPath = Path.Combine(DownloadDir, $"NovaPad_Setup_{tag}.exe");
        if (!File.Exists(downloadPath))
        {
            Status = UpdateStatus.Error;
            StatusText = "Archivo de actualización no encontrado.";
            return;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = downloadPath,
                Arguments = "/VERYSILENT /SUPPRESSMSGBOXES",
                UseShellExecute = true,
                WorkingDirectory = DownloadDir
            };
            Process.Start(psi);
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            Status = UpdateStatus.Error;
            StatusText = $"Error al instalar: {ex.Message}";
        }
    }

    private static int CompareVersions(string tag, string current)
    {
        var v1 = tag.TrimStart('v');
        var v2 = current.TrimStart('v');

        var parts1 = v1.Split('.');
        var parts2 = v2.Split('.');

        for (int i = 0; i < Math.Max(parts1.Length, parts2.Length); i++)
        {
            int p1 = i < parts1.Length && int.TryParse(parts1[i], out var n1) ? n1 : 0;
            int p2 = i < parts2.Length && int.TryParse(parts2[i], out var n2) ? n2 : 0;
            if (p1 > p2) return 1;
            if (p1 < p2) return -1;
        }
        return 0;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
