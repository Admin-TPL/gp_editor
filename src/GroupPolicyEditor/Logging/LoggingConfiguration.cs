using Microsoft.Extensions.Logging;
using Serilog;
using System.Diagnostics;

namespace GroupPolicyEditor.Logging;

/// <summary>
/// Configuration for application logging
/// </summary>
public static class LoggingConfiguration
{
    private static ILogger<T> GetLogger<T>() => LoggerFactory.Create(builder =>
    {
        builder.AddSerilog();
    }).CreateLogger<T>();

    /// <summary>
    /// Configure Serilog with file and console logging
    /// </summary>
    public static void ConfigureLogging(LogLevel minimumLevel = LogLevel.Information, bool enableVerbose = false)
    {
        var logLevel = enableVerbose ? Serilog.Events.LogEventLevel.Debug : ConvertToSerilogLevel(minimumLevel);
        
        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GroupPolicyEditor",
            "Logs"
        );
        
        Directory.CreateDirectory(logDirectory);
        
        var logFilePath = Path.Combine(logDirectory, "gp-editor-{Date}.log");
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(logLevel)
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "GroupPolicyEditor")
            .Enrich.WithProperty("Version", GetApplicationVersion())
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .Enrich.WithProperty("UserName", Environment.UserName)
            .Enrich.WithProperty("ProcessId", Environment.ProcessId)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
            )
            .WriteTo.File(
                logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}"
            )
            .CreateLogger();

        Log.Information("=== GroupPolicyEditor CLI Started ===");
        Log.Information("Version: {Version}", GetApplicationVersion());
        Log.Information("OS: {OS}", Environment.OSVersion);
        Log.Information("Runtime: {Runtime}", Environment.Version);
        Log.Information("Working Directory: {WorkingDirectory}", Environment.CurrentDirectory);
        Log.Information("Command Line: {CommandLine}", Environment.CommandLine);
        Log.Information("Log Level: {LogLevel}", logLevel);
        Log.Information("Log Directory: {LogDirectory}", logDirectory);
    }

    /// <summary>
    /// Get application version
    /// </summary>
    private static string GetApplicationVersion()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <summary>
    /// Convert Microsoft.Extensions.Logging.LogLevel to Serilog LogEventLevel
    /// </summary>
    private static Serilog.Events.LogEventLevel ConvertToSerilogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => Serilog.Events.LogEventLevel.Verbose,
            LogLevel.Debug => Serilog.Events.LogEventLevel.Debug,
            LogLevel.Information => Serilog.Events.LogEventLevel.Information,
            LogLevel.Warning => Serilog.Events.LogEventLevel.Warning,
            LogLevel.Error => Serilog.Events.LogEventLevel.Error,
            LogLevel.Critical => Serilog.Events.LogEventLevel.Fatal,
            LogLevel.None => Serilog.Events.LogEventLevel.Fatal,
            _ => Serilog.Events.LogEventLevel.Information
        };
    }

    /// <summary>
    /// Log application shutdown
    /// </summary>
    public static void LogShutdown()
    {
        Log.Information("=== GroupPolicyEditor CLI Shutting Down ===");
        Log.CloseAndFlush();
    }

    /// <summary>
    /// Log performance metrics for an operation
    /// </summary>
    public static void LogPerformance(string operationName, TimeSpan duration, Dictionary<string, object>? additionalData = null)
    {
        var properties = new Dictionary<string, object>
        {
            ["Operation"] = operationName,
            ["DurationMs"] = duration.TotalMilliseconds,
            ["DurationFormatted"] = duration.ToString(@"mm\:ss\.fff")
        };

        if (additionalData != null)
        {
            foreach (var kvp in additionalData)
            {
                properties[kvp.Key] = kvp.Value;
            }
        }

        Log.Information("Performance: {Operation} completed in {DurationFormatted} ({DurationMs}ms) {AdditionalData}",
            operationName, duration.ToString(@"mm\:ss\.fff"), duration.TotalMilliseconds, properties);
    }

    /// <summary>
    /// Create a performance tracking scope
    /// </summary>
    public static IDisposable BeginPerformanceScope(string operationName, Dictionary<string, object>? additionalData = null)
    {
        return new PerformanceScope(operationName, additionalData);
    }

    private class PerformanceScope : IDisposable
    {
        private readonly string _operationName;
        private readonly Dictionary<string, object>? _additionalData;
        private readonly Stopwatch _stopwatch;

        public PerformanceScope(string operationName, Dictionary<string, object>? additionalData)
        {
            _operationName = operationName;
            _additionalData = additionalData;
            _stopwatch = Stopwatch.StartNew();
            
            Log.Debug("Starting operation: {Operation}", operationName);
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            LogPerformance(_operationName, _stopwatch.Elapsed, _additionalData);
        }
    }
}
