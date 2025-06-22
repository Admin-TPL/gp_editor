using Serilog;
using System.Text.Json;

namespace GroupPolicyEditor.Logging;

/// <summary>
/// Application-specific logging helpers
/// </summary>
public static class AppLogger
{
    /// <summary>
    /// Safely serialize an object to JSON string
    /// </summary>
    private static string SafeSerialize(object? obj)
    {
        if (obj == null) return "null";
        
        try
        {
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions 
            { 
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to serialize object of type {Type}", obj.GetType().Name);
            return $"[Serialization failed for {obj.GetType().Name}]";
        }
    }/// <summary>
    /// Log command execution start
    /// </summary>
    public static void LogCommandStart(string commandName, object? parameters = null)
    {
        var parametersJson = SafeSerialize(parameters);
        Log.Information("Command Started: {Command} with parameters {Parameters}", commandName, parametersJson);
    }

    /// <summary>
    /// Log command execution completion
    /// </summary>
    public static void LogCommandComplete(string commandName, bool success, TimeSpan duration, string? additionalInfo = null)
    {
        if (success)
        {
            Log.Information("Command Completed Successfully: {Command} in {Duration}ms {AdditionalInfo}", 
                commandName, duration.TotalMilliseconds, additionalInfo ?? "");
        }
        else
        {
            Log.Warning("Command Failed: {Command} after {Duration}ms {AdditionalInfo}", 
                commandName, duration.TotalMilliseconds, additionalInfo ?? "");
        }
    }    /// <summary>
    /// Log API call start
    /// </summary>
    public static void LogApiCallStart(string apiMethod, object? parameters = null)
    {
        var parametersJson = SafeSerialize(parameters);
        Log.Debug("API Call Started: {Method} with parameters {Parameters}", apiMethod, parametersJson);
    }

    /// <summary>
    /// Log API call completion
    /// </summary>
    public static void LogApiCallComplete(string apiMethod, bool success, TimeSpan duration, object? result = null)
    {
        if (success)
        {
            var resultInfo = result switch
            {
                System.Collections.ICollection collection => $"returned {collection.Count} items",
                bool boolResult => $"returned {boolResult}",
                null => "returned null",
                _ => $"returned {result.GetType().Name}"
            };
            Log.Debug("API Call Completed: {Method} in {Duration}ms, {ResultInfo}", 
                apiMethod, duration.TotalMilliseconds, resultInfo);
        }
        else
        {
            Log.Warning("API Call Failed: {Method} after {Duration}ms", apiMethod, duration.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Log detailed error information
    /// </summary>
    public static void LogError(Exception exception, string context, object? additionalData = null)
    {
        var properties = new Dictionary<string, object>
        {
            ["Context"] = context,
            ["ExceptionType"] = exception.GetType().Name,
            ["Message"] = exception.Message,
            ["StackTrace"] = exception.StackTrace ?? "",
            ["Source"] = exception.Source ?? ""
        };        if (additionalData != null)
        {
            properties["AdditionalData"] = SafeSerialize(additionalData);
        }

        if (exception.InnerException != null)
        {
            properties["InnerException"] = exception.InnerException.Message;
            properties["InnerExceptionType"] = exception.InnerException.GetType().Name;
        }

        Log.Error(exception, "Error in {Context}: {Message} {Properties}", context, exception.Message, properties);
    }

    /// <summary>
    /// Log system information
    /// </summary>
    public static void LogSystemInfo()
    {
        try
        {
            var systemInfo = new
            {
                OS = Environment.OSVersion.ToString(),
                Architecture = Environment.ProcessorCount + " processors, " + (Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit"),
                MachineName = Environment.MachineName,
                UserName = Environment.UserName,
                WorkingDirectory = Environment.CurrentDirectory,
                ProcessId = Environment.ProcessId,
                RuntimeVersion = Environment.Version.ToString(),
                CommandLine = Environment.CommandLine
            };

            Log.Information("System Information: {@SystemInfo}", systemInfo);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to gather system information");
        }
    }

    /// <summary>
    /// Log GPO operation details
    /// </summary>
    public static void LogGpoOperation(string operation, string? gpoId = null, string? gpoName = null, string? domain = null, object? additionalData = null)
    {
        var operationDetails = new Dictionary<string, object?>
        {
            ["Operation"] = operation,
            ["GpoId"] = gpoId,
            ["GpoName"] = gpoName,
            ["Domain"] = domain ?? "Local"
        };

        if (additionalData != null)
        {
            operationDetails["AdditionalData"] = additionalData;
        }

        Log.Information("GPO Operation: {Operation} for GPO {GpoName} (ID: {GpoId}) in domain {Domain} {@OperationDetails}", 
            operation, gpoName ?? "Unknown", gpoId ?? "Unknown", domain ?? "Local", operationDetails);
    }    /// <summary>
    /// Log security context information
    /// </summary>
    public static void LogSecurityContext()
    {
        try
        {
#if WINDOWS
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);

            var securityInfo = new
            {
                UserName = identity.Name,
                AuthenticationType = identity.AuthenticationType,
                IsAuthenticated = identity.IsAuthenticated,
                IsAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator),
                IsSystem = identity.IsSystem,
                IsGuest = identity.IsGuest,
                IsAnonymous = identity.IsAnonymous,
                Groups = identity.Groups?.Select(g => g.Value).ToArray() ?? Array.Empty<string>()
            };

            Log.Information("Security Context: {@SecurityInfo}", securityInfo);

            if (!securityInfo.IsAdmin)
            {
                Log.Warning("Application is not running with administrator privileges. Some operations may fail.");
            }
#else
            Log.Information("Security Context: Running on non-Windows platform, limited security information available");
#endif
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to determine security context");
        }
    }

    /// <summary>
    /// Log validation results
    /// </summary>
    public static void LogValidation(string validationType, bool isValid, List<string>? errors = null, object? context = null)
    {
        if (isValid)
        {
            Log.Debug("Validation Passed: {ValidationType} {@Context}", validationType, context);
        }
        else
        {
            Log.Warning("Validation Failed: {ValidationType} with errors: {Errors} {@Context}", 
                validationType, errors ?? new List<string>(), context);
        }
    }    /// <summary>
    /// Log configuration settings
    /// </summary>
    public static void LogConfiguration(object configuration, string configType = "Configuration")
    {
        try
        {
            var configJson = SafeSerialize(configuration);
            Log.Debug("{ConfigType}: {Configuration}", configType, configJson);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to log {ConfigType}", configType);
        }
    }

    /// <summary>
    /// Log data operation results
    /// </summary>
    public static void LogDataOperation(string operation, int recordCount, TimeSpan duration, string? additionalInfo = null)
    {
        Log.Information("Data Operation: {Operation} processed {RecordCount} records in {Duration}ms {AdditionalInfo}", 
            operation, recordCount, duration.TotalMilliseconds, additionalInfo ?? "");
    }
}
