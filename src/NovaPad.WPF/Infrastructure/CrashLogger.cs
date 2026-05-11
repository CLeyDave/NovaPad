using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Threading;

namespace NovaPad.WPF.Infrastructure;

public static class CrashLogger
{
    private static string _crashDir = string.Empty;
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        _crashDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NovaPad", "crashes");
        Directory.CreateDirectory(_crashDir);

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    public static void LogException(Exception ex, string context = "General")
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var fileName = $"crash-{timestamp}-{Guid.NewGuid():N}.log";
            var filePath = Path.Combine(_crashDir, fileName);

            using var writer = new StreamWriter(filePath, false);
            writer.WriteLine("===========================================");
            writer.WriteLine($" NovaPad Crash Report");
            writer.WriteLine($" Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"===========================================");
            writer.WriteLine();
            writer.WriteLine($"Context: {context}");
            writer.WriteLine($"Version: {Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0"}");
            writer.WriteLine($"Runtime: {Environment.Version}");
            writer.WriteLine($"OS: {Environment.OSVersion}");
            writer.WriteLine($"64-bit: {Environment.Is64BitProcess}");
            writer.WriteLine($"Working Directory: {Environment.CurrentDirectory}");
            writer.WriteLine($"Command Line: {Environment.CommandLine}");
            writer.WriteLine();
            writer.WriteLine("===========================================");
            writer.WriteLine(" EXCEPTION");
            writer.WriteLine("===========================================");
            writer.WriteLine($"Type: {ex.GetType().FullName}");
            writer.WriteLine($"Message: {ex.Message}");
            writer.WriteLine($"Source: {ex.Source}");
            writer.WriteLine($"TargetSite: {ex.TargetSite?.Name}");
            writer.WriteLine();
            writer.WriteLine("STACK TRACE:");
            writer.WriteLine(ex.StackTrace);

            if (ex.InnerException != null)
            {
                writer.WriteLine();
                writer.WriteLine("===========================================");
                writer.WriteLine(" INNER EXCEPTION");
                writer.WriteLine("===========================================");
                writer.WriteLine($"Type: {ex.InnerException.GetType().FullName}");
                writer.WriteLine($"Message: {ex.InnerException.Message}");
                writer.WriteLine($"Stack: {ex.InnerException.StackTrace}");
            }

            writer.WriteLine();
            writer.WriteLine("===========================================");
            writer.WriteLine(" LOADED ASSEMBLIES");
            writer.WriteLine("===========================================");
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()
                         .OrderBy(a => a.FullName))
            {
                try
                {
                    writer.WriteLine($"  {asm.FullName}");
                }
                catch { }
            }

            writer.Flush();
            Debug.WriteLine($"[CrashLogger] Crash report saved: {filePath}");
        }
        catch
        {
            // No podemos hacer nada si falla el logger
        }
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogException(ex, "AppDomain.UnhandledException");
        }
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException(e.Exception, "Dispatcher.UnhandledException");
        e.Handled = true;
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        if (e.Exception?.InnerException != null)
        {
            LogException(e.Exception.InnerException, "TaskScheduler.UnobservedTaskException");
        }
        else if (e.Exception != null)
        {
            LogException(e.Exception, "TaskScheduler.UnobservedTaskException");
        }
        e.SetObserved();
    }
}
