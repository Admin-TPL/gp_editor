using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace GroupPolicyEditor.Core;

/// <summary>
/// Represents a Group Policy Object (GPO) with its associated settings and metadata
/// </summary>
public class GroupPolicyObject
{
    public string Name { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
    public DateTime ModifiedTime { get; set; }
    public GroupPolicyStatus Status { get; set; }
    public Dictionary<string, object> Settings { get; set; } = new();
}

/// <summary>
/// Enumeration of Group Policy status values
/// </summary>
public enum GroupPolicyStatus
{
    Enabled,
    Disabled,
    NotConfigured,
    PartiallyConfigured
}

/// <summary>
/// Represents different types of Group Policy settings
/// </summary>
public enum PolicyType
{
    UserConfiguration,
    ComputerConfiguration,
    SecuritySettings,
    AdministrativeTemplates,
    SoftwareInstallation,
    Scripts,
    FolderRedirection
}

/// <summary>
/// Represents a specific policy setting within a GPO
/// </summary>
public class PolicySetting
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PolicyType Type { get; set; }
    public object? Value { get; set; }
    public bool IsEnabled { get; set; }
    public string RegistryPath { get; set; } = string.Empty;
    public string RegistryKey { get; set; } = string.Empty;
    public RegistryValueKind ValueType { get; set; }
}
