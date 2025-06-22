using GroupPolicyEditor.Core;
using GroupPolicyEditor.Logging;
using Serilog;
using System.Diagnostics;
using System.Text.Json;

namespace GroupPolicyEditor.Api;

/// <summary>
/// High-level API for Group Policy management with simplified method signatures
/// Designed to be easily consumed by Python and other languages
/// </summary>
public class GroupPolicyApi
{
    private readonly IGroupPolicyManager _manager;    public GroupPolicyApi(string? domain = null)
    {
        Log.Debug("Creating GroupPolicyApi instance for domain: {Domain}", domain ?? "Local");
        _manager = new GroupPolicyManager(domain);
        Log.Debug("GroupPolicyManager initialized successfully");
    }

    #region GPO Management    /// <summary>
    /// Gets all Group Policy Objects
    /// </summary>
    public async Task<List<GroupPolicyInfo>> GetAllGPOsAsync()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            Log.Debug("API: Starting GetAllGPOsAsync operation");
            AppLogger.LogApiCallStart("GetAllGPOsAsync");
            
            var gpos = await _manager.GetAllGPOsAsync();
            var result = gpos.Select(ConvertToInfo).ToList();
            
            sw.Stop();
            Log.Debug("API: GetAllGPOsAsync completed successfully, retrieved {Count} GPOs in {Duration}ms", 
                result.Count, sw.ElapsedMilliseconds);
            AppLogger.LogApiCallComplete("GetAllGPOsAsync", true, sw.Elapsed, result);
            
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log.Error(ex, "API: GetAllGPOsAsync failed after {Duration}ms", sw.ElapsedMilliseconds);
            AppLogger.LogApiCallComplete("GetAllGPOsAsync", false, sw.Elapsed);
            AppLogger.LogError(ex, "GetAllGPOsAsync");
            throw;
        }
    }    /// <summary>
    /// Gets a specific GPO by ID
    /// </summary>
    public async Task<GroupPolicyInfo?> GetGPOByIdAsync(string gpoId)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            Log.Debug("API: Starting GetGPOByIdAsync operation for ID: {GpoId}", gpoId);
            AppLogger.LogApiCallStart("GetGPOByIdAsync", new { gpoId });
            
            var gpo = await _manager.GetGPOByIdAsync(gpoId);
            var result = gpo != null ? ConvertToInfo(gpo) : null;
            
            sw.Stop();
            Log.Debug("API: GetGPOByIdAsync completed in {Duration}ms, found: {Found}", 
                sw.ElapsedMilliseconds, result != null);
            AppLogger.LogApiCallComplete("GetGPOByIdAsync", true, sw.Elapsed, result);
            
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log.Error(ex, "API: GetGPOByIdAsync failed for ID {GpoId} after {Duration}ms", gpoId, sw.ElapsedMilliseconds);
            AppLogger.LogApiCallComplete("GetGPOByIdAsync", false, sw.Elapsed);
            AppLogger.LogError(ex, "GetGPOByIdAsync", new { gpoId });
            throw;
        }
    }

    /// <summary>
    /// Gets a specific GPO by name
    /// </summary>
    public async Task<GroupPolicyInfo?> GetGPOByNameAsync(string gpoName)
    {
        var gpo = await _manager.GetGPOByNameAsync(gpoName);
        return gpo != null ? ConvertToInfo(gpo) : null;
    }

    /// <summary>
    /// Creates a new Group Policy Object
    /// </summary>
    public async Task<bool> CreateGPOAsync(string name, string? description = null)
    {
        return await _manager.CreateGPOAsync(name, description);
    }

    /// <summary>
    /// Deletes a Group Policy Object
    /// </summary>
    public async Task<bool> DeleteGPOAsync(string gpoId)
    {
        return await _manager.DeleteGPOAsync(gpoId);
    }

    #endregion

    #region Policy Settings

    /// <summary>
    /// Gets all policy settings for a GPO
    /// </summary>
    public async Task<List<PolicySettingInfo>> GetPolicySettingsAsync(string gpoId)
    {
        var settings = await _manager.GetPolicySettingsAsync(gpoId);
        return settings.Select(ConvertToSettingInfo).ToList();
    }

    /// <summary>
    /// Sets a policy setting
    /// </summary>
    public async Task<bool> SetPolicySettingAsync(string gpoId, string settingName, object value, string registryPath, string policyType = "Computer")
    {        var setting = new PolicySetting
        {
            Name = settingName,
            Value = value,
            RegistryPath = registryPath,
            RegistryKey = settingName,
            IsEnabled = true,
            Type = policyType.Equals("User", StringComparison.OrdinalIgnoreCase) 
                ? PolicyType.UserConfiguration 
                : PolicyType.ComputerConfiguration,
#if WINDOWS
            ValueType = Microsoft.Win32.RegistryValueKind.String
#else
            ValueType = (Microsoft.Win32.RegistryValueKind)1 // String equivalent
#endif
        };

        return await _manager.SetPolicySettingAsync(gpoId, setting);
    }

    /// <summary>
    /// Sets a policy setting with full control
    /// </summary>
    public async Task<bool> SetPolicySettingAsync(string gpoId, PolicySettingInfo settingInfo)
    {
        var setting = ConvertFromSettingInfo(settingInfo);
        return await _manager.SetPolicySettingAsync(gpoId, setting);
    }

    /// <summary>
    /// Removes a policy setting
    /// </summary>
    public async Task<bool> RemovePolicySettingAsync(string gpoId, string settingName)
    {
        return await _manager.RemovePolicySettingAsync(gpoId, settingName);
    }

    #endregion

    #region GPO Linking

    /// <summary>
    /// Links a GPO to an Organizational Unit
    /// </summary>
    public async Task<bool> LinkGPOAsync(string gpoId, string organizationalUnit)
    {
        return await _manager.LinkGPOAsync(gpoId, organizationalUnit);
    }

    /// <summary>
    /// Unlinks a GPO from an Organizational Unit
    /// </summary>
    public async Task<bool> UnlinkGPOAsync(string gpoId, string organizationalUnit)
    {
        return await _manager.UnlinkGPOAsync(gpoId, organizationalUnit);
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Serializes GPO data to JSON
    /// </summary>
    public string SerializeGPO(GroupPolicyInfo gpo)
    {
        return JsonSerializer.Serialize(gpo, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Deserializes GPO data from JSON
    /// </summary>
    public GroupPolicyInfo? DeserializeGPO(string json)
    {
        return JsonSerializer.Deserialize<GroupPolicyInfo>(json);
    }

    /// <summary>
    /// Serializes policy settings to JSON
    /// </summary>
    public string SerializePolicySettings(List<PolicySettingInfo> settings)
    {
        return JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Deserializes policy settings from JSON
    /// </summary>
    public List<PolicySettingInfo>? DeserializePolicySettings(string json)
    {
        return JsonSerializer.Deserialize<List<PolicySettingInfo>>(json);
    }

    #endregion

    #region Private Helper Methods

    private static GroupPolicyInfo ConvertToInfo(GroupPolicyObject gpo)
    {
        return new GroupPolicyInfo
        {
            Id = gpo.Id,
            Name = gpo.Name,
            Domain = gpo.Domain,
            CreatedTime = gpo.CreatedTime,
            ModifiedTime = gpo.ModifiedTime,
            Status = gpo.Status.ToString(),
            SettingsCount = gpo.Settings.Count
        };
    }

    private static PolicySettingInfo ConvertToSettingInfo(PolicySetting setting)
    {
        return new PolicySettingInfo
        {
            Name = setting.Name,
            Description = setting.Description,
            Type = setting.Type.ToString(),
            Value = setting.Value?.ToString() ?? "",
            IsEnabled = setting.IsEnabled,
            RegistryPath = setting.RegistryPath,
            RegistryKey = setting.RegistryKey,
            ValueType = setting.ValueType.ToString()
        };
    }    private static PolicySetting ConvertFromSettingInfo(PolicySettingInfo info)
    {
        var setting = new PolicySetting
        {
            Name = info.Name,
            Description = info.Description,
            Type = Enum.Parse<PolicyType>(info.Type),
            Value = info.Value,
            IsEnabled = info.IsEnabled,
            RegistryPath = info.RegistryPath,
            RegistryKey = info.RegistryKey
        };

#if WINDOWS
        setting.ValueType = Enum.Parse<Microsoft.Win32.RegistryValueKind>(info.ValueType);
#else
        // For non-Windows platforms, use a default value
        setting.ValueType = (Microsoft.Win32.RegistryValueKind)1; // String equivalent
#endif

        return setting;
    }

    #endregion
}

/// <summary>
/// Simplified GPO information for API consumers
/// </summary>
public class GroupPolicyInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
    public DateTime ModifiedTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public int SettingsCount { get; set; }
}

/// <summary>
/// Simplified policy setting information for API consumers
/// </summary>
public class PolicySettingInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string RegistryPath { get; set; } = string.Empty;
    public string RegistryKey { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
}
