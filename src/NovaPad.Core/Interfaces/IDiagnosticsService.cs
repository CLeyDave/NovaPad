using NovaPad.Core.Enums;
using NovaPad.Core.Models;

namespace NovaPad.Core.Interfaces;

public interface IDiagnosticsService
{
    event EventHandler<LogEntry>? LogAdded;

    IReadOnlyList<LogEntry> Logs { get; }
    void Log(LogLevel level, string source, string message, string? category = null, Exception? exception = null);
    void Trace(string source, string message, string? category = null);
    void Debug(string source, string message, string? category = null);
    void Info(string source, string message, string? category = null);
    void Warning(string source, string message, string? category = null);
    void Error(string source, string message, Exception? exception = null, string? category = null);
    void Fatal(string source, string message, Exception? exception = null, string? category = null);
    void Clear();
    IEnumerable<LogEntry> GetLogsByLevel(LogLevel level);
    IEnumerable<LogEntry> GetLogsBySource(string source);
    Task ExportLogsAsync(string filePath);
}
