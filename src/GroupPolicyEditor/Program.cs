using GroupPolicyEditor.Api;
using GroupPolicyEditor.Core;
using GroupPolicyEditor.Logging;
using Microsoft.Extensions.Logging;
using Serilog;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Text.Json;

namespace GroupPolicyEditor;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var sw = Stopwatch.StartNew();
        int exitCode = 0;

        try
        {
            // Parse arguments early to check for verbose flag
            var verboseEnabled = args.Contains("--verbose") || args.Contains("-v");
            var logLevel = verboseEnabled ? LogLevel.Debug : LogLevel.Information;

            // Configure logging first
            LoggingConfiguration.ConfigureLogging(logLevel, verboseEnabled);

            // Log startup information
            AppLogger.LogSystemInfo();
            AppLogger.LogSecurityContext();

            Log.Information("Application arguments: {Args}", string.Join(" ", args));

            var rootCommand = new RootCommand("Group Policy Editor CLI - Manage Windows Group Policy settings");

            // Add global verbose option
            var verboseOption = new Option<bool>(
                new[] { "--verbose", "-v" },
                "Enable verbose logging"
            );
            rootCommand.AddGlobalOption(verboseOption);

            // List GPOs command
            var listCommand = new Command("list", "List all Group Policy Objects");
            var domainOption = new Option<string?>("--domain", "Specify domain (leave empty for local)");
            var formatOption = new Option<string>("--format", () => "table", "Output format: table, json, csv")
                .FromAmong("table", "json", "csv");
            
            listCommand.AddOption(domainOption);
            listCommand.AddOption(formatOption);
            listCommand.SetHandler(async (domain, format, verbose) => 
            {
                await ExecuteCommandWithLogging("list", async () => await ListGPOs(domain, format), 
                    new { domain, format, verbose });
            }, domainOption, formatOption, verboseOption);

            // Get GPO command
            var getCommand = new Command("get", "Get details of a specific GPO");
            var gpoIdOption = new Option<string>("--id", "GPO ID");
            var gpoNameOption = new Option<string>("--name", "GPO name");
            getCommand.AddOption(gpoIdOption);
            getCommand.AddOption(gpoNameOption);
            getCommand.AddOption(domainOption);
            getCommand.AddOption(formatOption);
            getCommand.SetHandler(async (id, name, domain, format, verbose) => 
            {
                await ExecuteCommandWithLogging("get", async () => await GetGPO(id, name, domain, format), 
                    new { id, name, domain, format, verbose });
            }, gpoIdOption, gpoNameOption, domainOption, formatOption, verboseOption);

            // Create GPO command
            var createCommand = new Command("create", "Create a new Group Policy Object");
            var nameArgument = new Argument<string>("name", "Name of the new GPO");
            var descriptionOption = new Option<string?>("--description", "Description of the new GPO");
            createCommand.AddArgument(nameArgument);
            createCommand.AddOption(descriptionOption);
            createCommand.AddOption(domainOption);
            createCommand.SetHandler(async (name, description, domain, verbose) => 
            {
                await ExecuteCommandWithLogging("create", async () => await CreateGPO(name, description, domain), 
                    new { name, description, domain, verbose });
            }, nameArgument, descriptionOption, domainOption, verboseOption);

            // Delete GPO command
            var deleteCommand = new Command("delete", "Delete a Group Policy Object");
            var deleteGpoIdOption = new Option<string>("--id", "GPO ID to delete") { IsRequired = true };
            deleteCommand.AddOption(deleteGpoIdOption);
            deleteCommand.AddOption(domainOption);
            deleteCommand.SetHandler(async (id, domain, verbose) => 
            {
                await ExecuteCommandWithLogging("delete", async () => await DeleteGPO(id, domain), 
                    new { id, domain, verbose });
            }, deleteGpoIdOption, domainOption, verboseOption);

            // Get settings command
            var settingsCommand = new Command("settings", "Get policy settings for a GPO");
            var settingsGpoIdOption = new Option<string>("--gpo-id", "GPO ID") { IsRequired = true };
            settingsCommand.AddOption(settingsGpoIdOption);
            settingsCommand.AddOption(domainOption);
            settingsCommand.AddOption(formatOption);
            settingsCommand.SetHandler(async (gpoId, domain, format, verbose) => 
            {
                await ExecuteCommandWithLogging("settings", async () => await GetSettings(gpoId, domain, format), 
                    new { gpoId, domain, format, verbose });
            }, settingsGpoIdOption, domainOption, formatOption, verboseOption);

            // Set setting command
            var setCommand = new Command("set", "Set a policy setting in a GPO");
            var setGpoIdOption = new Option<string>("--gpo-id", "GPO ID") { IsRequired = true };
            var settingNameOption = new Option<string>("--name", "Setting name") { IsRequired = true };
            var settingValueOption = new Option<string>("--value", "Setting value") { IsRequired = true };
            var settingTypeOption = new Option<string>("--type", () => "String", "Setting type")
                .FromAmong("String", "Integer", "Boolean");
            setCommand.AddOption(setGpoIdOption);
            setCommand.AddOption(settingNameOption);
            setCommand.AddOption(settingValueOption);
            setCommand.AddOption(settingTypeOption);
            setCommand.AddOption(domainOption);
            setCommand.SetHandler(async (gpoId, name, value, type, domain, verbose) => 
            {
                await ExecuteCommandWithLogging("set", async () => await SetSetting(gpoId, name, value, type, domain), 
                    new { gpoId, name, value, type, domain, verbose });
            }, setGpoIdOption, settingNameOption, settingValueOption, settingTypeOption, domainOption, verboseOption);

            rootCommand.AddCommand(listCommand);
            rootCommand.AddCommand(getCommand);
            rootCommand.AddCommand(createCommand);
            rootCommand.AddCommand(deleteCommand);
            rootCommand.AddCommand(settingsCommand);
            rootCommand.AddCommand(setCommand);

            Log.Debug("Commands configured, invoking root command");
            exitCode = await rootCommand.InvokeAsync(args);
            
            sw.Stop();
            Log.Information("Application completed successfully in {Duration}ms with exit code {ExitCode}", 
                sw.ElapsedMilliseconds, exitCode);
        }
        catch (Exception ex)
        {
            sw.Stop();
            exitCode = 1;
            AppLogger.LogError(ex, "Application startup/execution", new { args, duration = sw.Elapsed });
            Console.WriteLine($"Fatal error: {ex.Message}");
            
            if (Log.Logger != null)
            {
                Log.Fatal(ex, "Application terminated unexpectedly after {Duration}ms", sw.ElapsedMilliseconds);
            }
        }
        finally
        {
            LoggingConfiguration.LogShutdown();
        }

        return exitCode;
    }

    /// <summary>
    /// Execute a command with comprehensive logging
    /// </summary>
    private static async Task ExecuteCommandWithLogging(string commandName, Func<Task> commandAction, object? parameters = null)
    {
        var sw = Stopwatch.StartNew();
        bool success = false;
        
        try
        {
            AppLogger.LogCommandStart(commandName, parameters);
            await commandAction();
            success = true;
        }
        catch (Exception ex)
        {
            AppLogger.LogError(ex, $"Command execution: {commandName}", parameters);
            Console.WriteLine($"Error executing {commandName}: {ex.Message}");
            throw;
        }
        finally
        {
            sw.Stop();
            AppLogger.LogCommandComplete(commandName, success, sw.Elapsed, 
                success ? "Command completed successfully" : "Command failed");
        }
    }    static async Task ListGPOs(string? domain, string format)
    {
        var sw = Stopwatch.StartNew();
        
        try
        {
            Log.Information("Starting GPO list operation for domain: {Domain}, format: {Format}", domain ?? "Local", format);
            
            using var performanceScope = LoggingConfiguration.BeginPerformanceScope("ListGPOs", 
                new Dictionary<string, object> { ["Domain"] = domain ?? "Local", ["Format"] = format });
            
            AppLogger.LogGpoOperation("List", domain: domain, additionalData: new { format });
            
            var api = new GroupPolicyApi(domain);
            Log.Debug("GroupPolicyApi instance created for domain: {Domain}", domain ?? "Local");
            
            var gpos = await api.GetAllGPOsAsync();
            Log.Information("Retrieved {Count} GPOs from domain {Domain}", gpos.Count, domain ?? "Local");

            switch (format.ToLower())
            {
                case "json":
                    Log.Debug("Serializing {Count} GPOs to JSON format", gpos.Count);
                    Console.WriteLine(JsonSerializer.Serialize(gpos, new JsonSerializerOptions { WriteIndented = true }));
                    break;
                case "csv":
                    Log.Debug("Formatting {Count} GPOs as CSV", gpos.Count);
                    Console.WriteLine("Id,Name,Domain,CreatedTime,ModifiedTime,Status");
                    foreach (var gpo in gpos)
                    {
                        Console.WriteLine($"{gpo.Id},{gpo.Name},{gpo.Domain},{gpo.CreatedTime},{gpo.ModifiedTime},{gpo.Status}");
                    }
                    break;
                default: // table
                    Log.Debug("Formatting {Count} GPOs as table", gpos.Count);
                    Console.WriteLine($"{"ID",-38} {"Name",-30} {"Domain",-20} {"Created",-20}");
                    Console.WriteLine(new string('-', 110));
                    foreach (var gpo in gpos)
                    {
                        Console.WriteLine($"{gpo.Id,-38} {gpo.Name?.Truncate(29),-30} {gpo.Domain?.Truncate(19),-20} {gpo.CreatedTime:yyyy-MM-dd HH:mm,-20}");
                    }
                    break;
            }

            Console.WriteLine($"\nTotal GPOs: {gpos.Count}");
            
            sw.Stop();
            AppLogger.LogDataOperation("ListGPOs", gpos.Count, sw.Elapsed, $"Format: {format}");
            Log.Information("List GPOs operation completed successfully in {Duration}ms", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            AppLogger.LogError(ex, "ListGPOs", new { domain, format, duration = sw.Elapsed });
            Console.WriteLine($"Error listing GPOs: {ex.Message}");
            throw;
        }
    }    static async Task GetGPO(string? id, string? name, string? domain, string format)
    {
        var sw = Stopwatch.StartNew();
        
        try
        {
            Log.Information("Starting get GPO operation - ID: {Id}, Name: {Name}, Domain: {Domain}, Format: {Format}", 
                id ?? "null", name ?? "null", domain ?? "Local", format);
            
            if (string.IsNullOrEmpty(id) && string.IsNullOrEmpty(name))
            {
                var error = "Either --id or --name must be specified";
                Log.Warning("Validation failed: {Error}", error);
                AppLogger.LogValidation("GetGPO", false, new List<string> { error }, new { id, name, domain, format });
                Console.WriteLine($"Error: {error}");
                return;
            }

            using var performanceScope = LoggingConfiguration.BeginPerformanceScope("GetGPO", 
                new Dictionary<string, object> 
                { 
                    ["Id"] = id ?? "null",
                    ["Name"] = name ?? "null", 
                    ["Domain"] = domain ?? "Local", 
                    ["Format"] = format 
                });

            AppLogger.LogGpoOperation("Get", id, name, domain, new { format });
            
            var api = new GroupPolicyApi(domain);
            Log.Debug("GroupPolicyApi instance created for domain: {Domain}", domain ?? "Local");
            
            GroupPolicyInfo? gpo = null;

            if (!string.IsNullOrEmpty(id))
            {
                Log.Debug("Searching for GPO by ID: {Id}", id);
                gpo = await api.GetGPOByIdAsync(id);
            }
            else if (!string.IsNullOrEmpty(name))
            {
                Log.Debug("Searching for GPO by name: {Name}", name);
                gpo = await api.GetGPOByNameAsync(name);
            }

            if (gpo == null)
            {
                var notFoundMsg = $"GPO not found - ID: {id}, Name: {name}";
                Log.Warning(notFoundMsg);
                Console.WriteLine("GPO not found");
                return;
            }

            Log.Information("Found GPO: {GpoName} (ID: {GpoId}) in domain {Domain}", 
                gpo.Name, gpo.Id, gpo.Domain);

            switch (format.ToLower())
            {
                case "json":
                    Log.Debug("Serializing GPO to JSON format");
                    Console.WriteLine(JsonSerializer.Serialize(gpo, new JsonSerializerOptions { WriteIndented = true }));
                    break;
                default:
                    Log.Debug("Displaying GPO details in table format");
                    Console.WriteLine($"ID: {gpo.Id}");
                    Console.WriteLine($"Name: {gpo.Name}");
                    Console.WriteLine($"Domain: {gpo.Domain}");
                    Console.WriteLine($"Created: {gpo.CreatedTime:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine($"Modified: {gpo.ModifiedTime:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine($"Status: {gpo.Status}");
                    Console.WriteLine($"Settings Count: {gpo.SettingsCount}");
                    break;
            }
            
            sw.Stop();
            Log.Information("Get GPO operation completed successfully in {Duration}ms", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            AppLogger.LogError(ex, "GetGPO", new { id, name, domain, format, duration = sw.Elapsed });
            Console.WriteLine($"Error getting GPO: {ex.Message}");
            throw;
        }
    }    static async Task CreateGPO(string name, string? description, string? domain)
    {
        var sw = Stopwatch.StartNew();
        
        try
        {
            Log.Information("Starting create GPO operation - Name: {Name}, Description: {Description}, Domain: {Domain}", 
                name, description ?? "null", domain ?? "Local");
            
            using var performanceScope = LoggingConfiguration.BeginPerformanceScope("CreateGPO", 
                new Dictionary<string, object> 
                { 
                    ["Name"] = name,
                    ["Description"] = description ?? "null", 
                    ["Domain"] = domain ?? "Local"
                });

            AppLogger.LogGpoOperation("Create", gpoName: name, domain: domain, additionalData: new { description });
            
            var api = new GroupPolicyApi(domain);
            Log.Debug("GroupPolicyApi instance created for domain: {Domain}", domain ?? "Local");
            
            Log.Debug("Attempting to create GPO with name: {Name}", name);
            var success = await api.CreateGPOAsync(name, description);

            if (success)
            {
                var successMsg = $"GPO '{name}' created successfully";
                Log.Information(successMsg);
                Console.WriteLine(successMsg);
            }
            else
            {
                var failMsg = $"Failed to create GPO '{name}'";
                Log.Warning(failMsg);
                Console.WriteLine(failMsg);
            }
            
            sw.Stop();
            Log.Information("Create GPO operation completed in {Duration}ms with result: {Success}", 
                sw.ElapsedMilliseconds, success);
        }
        catch (Exception ex)
        {
            sw.Stop();
            AppLogger.LogError(ex, "CreateGPO", new { name, description, domain, duration = sw.Elapsed });
            Console.WriteLine($"Error creating GPO: {ex.Message}");
            throw;
        }
    }    static async Task DeleteGPO(string id, string? domain)
    {
        var sw = Stopwatch.StartNew();
        
        try
        {
            Log.Information("Starting delete GPO operation - ID: {Id}, Domain: {Domain}", id, domain ?? "Local");
            
            using var performanceScope = LoggingConfiguration.BeginPerformanceScope("DeleteGPO", 
                new Dictionary<string, object> 
                { 
                    ["Id"] = id,
                    ["Domain"] = domain ?? "Local"
                });

            AppLogger.LogGpoOperation("Delete", id, domain: domain);
            
            var api = new GroupPolicyApi(domain);
            Log.Debug("GroupPolicyApi instance created for domain: {Domain}", domain ?? "Local");
            
            Log.Debug("Attempting to delete GPO with ID: {Id}", id);
            var success = await api.DeleteGPOAsync(id);

            if (success)
            {
                var successMsg = $"GPO '{id}' deleted successfully";
                Log.Information(successMsg);
                Console.WriteLine(successMsg);
            }
            else
            {
                var failMsg = $"Failed to delete GPO '{id}'";
                Log.Warning(failMsg);
                Console.WriteLine(failMsg);
            }
            
            sw.Stop();
            Log.Information("Delete GPO operation completed in {Duration}ms with result: {Success}", 
                sw.ElapsedMilliseconds, success);
        }
        catch (Exception ex)
        {
            sw.Stop();
            AppLogger.LogError(ex, "DeleteGPO", new { id, domain, duration = sw.Elapsed });
            Console.WriteLine($"Error deleting GPO: {ex.Message}");
            throw;
        }
    }    static async Task GetSettings(string gpoId, string? domain, string format)
    {
        var sw = Stopwatch.StartNew();
        
        try
        {
            Log.Information("Starting get settings operation - GPO ID: {GpoId}, Domain: {Domain}, Format: {Format}", 
                gpoId, domain ?? "Local", format);
            
            using var performanceScope = LoggingConfiguration.BeginPerformanceScope("GetSettings", 
                new Dictionary<string, object> 
                { 
                    ["GpoId"] = gpoId,
                    ["Domain"] = domain ?? "Local",
                    ["Format"] = format
                });

            AppLogger.LogGpoOperation("GetSettings", gpoId, domain: domain, additionalData: new { format });
            
            var api = new GroupPolicyApi(domain);
            Log.Debug("GroupPolicyApi instance created for domain: {Domain}", domain ?? "Local");
            
            Log.Debug("Retrieving policy settings for GPO: {GpoId}", gpoId);
            var settings = await api.GetPolicySettingsAsync(gpoId);
            Log.Information("Retrieved {Count} policy settings for GPO {GpoId}", settings.Count, gpoId);

            switch (format.ToLower())
            {
                case "json":
                    Log.Debug("Serializing {Count} policy settings to JSON format", settings.Count);
                    Console.WriteLine(JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
                    break;
                case "csv":
                    Log.Debug("Formatting {Count} policy settings as CSV", settings.Count);
                    Console.WriteLine("Name,Value,Type");
                    foreach (var setting in settings)
                    {
                        Console.WriteLine($"{setting.Name},{setting.Value},{setting.Type}");
                    }
                    break;
                default: // table
                    Log.Debug("Formatting {Count} policy settings as table", settings.Count);
                    Console.WriteLine($"{"Name",-40} {"Value",-30} {"Type",-15}");
                    Console.WriteLine(new string('-', 87));
                    foreach (var setting in settings)
                    {
                        Console.WriteLine($"{setting.Name?.Truncate(39),-40} {setting.Value?.ToString()?.Truncate(29),-30} {setting.Type,-15}");
                    }
                    break;
            }

            Console.WriteLine($"\nTotal settings: {settings.Count}");
            
            sw.Stop();
            AppLogger.LogDataOperation("GetSettings", settings.Count, sw.Elapsed, $"GPO: {gpoId}, Format: {format}");
            Log.Information("Get settings operation completed successfully in {Duration}ms", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            AppLogger.LogError(ex, "GetSettings", new { gpoId, domain, format, duration = sw.Elapsed });
            Console.WriteLine($"Error getting settings: {ex.Message}");
            throw;
        }
    }    static async Task SetSetting(string gpoId, string name, string value, string type, string? domain)
    {
        var sw = Stopwatch.StartNew();
        
        try
        {
            Log.Information("Starting set setting operation - GPO ID: {GpoId}, Setting: {Name}, Value: {Value}, Type: {Type}, Domain: {Domain}", 
                gpoId, name, value, type, domain ?? "Local");
            
            using var performanceScope = LoggingConfiguration.BeginPerformanceScope("SetSetting", 
                new Dictionary<string, object> 
                { 
                    ["GpoId"] = gpoId,
                    ["SettingName"] = name,
                    ["SettingValue"] = value,
                    ["SettingType"] = type,
                    ["Domain"] = domain ?? "Local"
                });

            AppLogger.LogGpoOperation("SetSetting", gpoId, domain: domain, 
                additionalData: new { settingName = name, settingValue = value, settingType = type });
            
            var api = new GroupPolicyApi(domain);
            Log.Debug("GroupPolicyApi instance created for domain: {Domain}", domain ?? "Local");
            
            Log.Debug("Parsing setting value '{Value}' as type '{Type}'", value, type);
            object parsedValue = type.ToLower() switch
            {
                "integer" => int.Parse(value),
                "boolean" => bool.Parse(value),
                _ => value
            };

            Log.Information("Parsed value: {ParsedValue} (Type: {ParsedType})", parsedValue, parsedValue.GetType().Name);
            
            Log.Debug("Attempting to set policy setting: {Name} = {Value} in GPO {GpoId}", name, parsedValue, gpoId);
            var success = await api.SetPolicySettingAsync(gpoId, name, parsedValue, "", type);

            if (success)
            {
                var successMsg = $"Setting '{name}' updated successfully";
                Log.Information(successMsg + " in GPO {GpoId} with value {Value}", gpoId, parsedValue);
                Console.WriteLine(successMsg);
            }
            else
            {
                var failMsg = $"Failed to update setting '{name}'";
                Log.Warning(failMsg + " in GPO {GpoId}", gpoId);
                Console.WriteLine(failMsg);
            }
            
            sw.Stop();
            Log.Information("Set setting operation completed in {Duration}ms with result: {Success}", 
                sw.ElapsedMilliseconds, success);
        }
        catch (FormatException ex)
        {
            sw.Stop();
            var error = $"Failed to parse value '{value}' as {type}";
            Log.Error(ex, error + " for setting {Name} in GPO {GpoId}", name, gpoId);
            AppLogger.LogError(ex, "SetSetting - Value parsing", new { gpoId, name, value, type, domain, duration = sw.Elapsed });
            Console.WriteLine($"Error setting policy: {error}");
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            AppLogger.LogError(ex, "SetSetting", new { gpoId, name, value, type, domain, duration = sw.Elapsed });
            Console.WriteLine($"Error setting policy: {ex.Message}");
            throw;
        }
    }
}

public static class StringExtensions
{
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
    }
}
