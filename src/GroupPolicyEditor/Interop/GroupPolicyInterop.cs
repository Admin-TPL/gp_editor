using System.Runtime.InteropServices;
using System.Text.Json;
using GroupPolicyEditor.Core;

namespace GroupPolicyEditor.Interop;

/// <summary>
/// Python-friendly wrapper for Group Policy management operations
/// Provides simple C-style exports that can be easily consumed by Python via ctypes
/// </summary>
public static class GroupPolicyInterop
{
    private static readonly GroupPolicyManager _manager = new();

    #region GPO Management

    /// <summary>
    /// Gets all Group Policy Objects as JSON string
    /// </summary>
    [DllExport("GetAllGPOs", CallingConvention = CallingConvention.Cdecl)]
    public static IntPtr GetAllGPOs()
    {
        try
        {
            var gpos = _manager.GetAllGPOsAsync().Result;
            var json = JsonSerializer.Serialize(gpos, new JsonSerializerOptions { WriteIndented = true });
            return MarshalString(json);
        }
        catch (Exception ex)
        {
            var error = new { error = ex.Message };
            var json = JsonSerializer.Serialize(error);
            return MarshalString(json);
        }
    }

    /// <summary>
    /// Gets a GPO by ID as JSON string
    /// </summary>
    [DllExport("GetGPOById", CallingConvention = CallingConvention.Cdecl)]
    public static IntPtr GetGPOById([MarshalAs(UnmanagedType.LPStr)] string gpoId)
    {
        try
        {
            var gpo = _manager.GetGPOByIdAsync(gpoId).Result;
            var json = JsonSerializer.Serialize(gpo, new JsonSerializerOptions { WriteIndented = true });
            return MarshalString(json);
        }
        catch (Exception ex)
        {
            var error = new { error = ex.Message };
            var json = JsonSerializer.Serialize(error);
            return MarshalString(json);
        }
    }

    /// <summary>
    /// Gets a GPO by name as JSON string
    /// </summary>
    [DllExport("GetGPOByName", CallingConvention = CallingConvention.Cdecl)]
    public static IntPtr GetGPOByName([MarshalAs(UnmanagedType.LPStr)] string gpoName)
    {
        try
        {
            var gpo = _manager.GetGPOByNameAsync(gpoName).Result;
            var json = JsonSerializer.Serialize(gpo, new JsonSerializerOptions { WriteIndented = true });
            return MarshalString(json);
        }
        catch (Exception ex)
        {
            var error = new { error = ex.Message };
            var json = JsonSerializer.Serialize(error);
            return MarshalString(json);
        }
    }

    /// <summary>
    /// Creates a new GPO
    /// </summary>
    [DllExport("CreateGPO", CallingConvention = CallingConvention.Cdecl)]
    public static bool CreateGPO([MarshalAs(UnmanagedType.LPStr)] string name, 
                                [MarshalAs(UnmanagedType.LPStr)] string? description)
    {
        try
        {
            return _manager.CreateGPOAsync(name, description).Result;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Deletes a GPO by ID
    /// </summary>
    [DllExport("DeleteGPO", CallingConvention = CallingConvention.Cdecl)]
    public static bool DeleteGPO([MarshalAs(UnmanagedType.LPStr)] string gpoId)
    {
        try
        {
            return _manager.DeleteGPOAsync(gpoId).Result;
        }
        catch (Exception)
        {
            return false;
        }
    }

    #endregion

    #region Policy Settings

    /// <summary>
    /// Gets policy settings for a GPO as JSON string
    /// </summary>
    [DllExport("GetPolicySettings", CallingConvention = CallingConvention.Cdecl)]
    public static IntPtr GetPolicySettings([MarshalAs(UnmanagedType.LPStr)] string gpoId)
    {
        try
        {
            var settings = _manager.GetPolicySettingsAsync(gpoId).Result;
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            return MarshalString(json);
        }
        catch (Exception ex)
        {
            var error = new { error = ex.Message };
            var json = JsonSerializer.Serialize(error);
            return MarshalString(json);
        }
    }

    /// <summary>
    /// Sets a policy setting from JSON
    /// </summary>
    [DllExport("SetPolicySetting", CallingConvention = CallingConvention.Cdecl)]
    public static bool SetPolicySetting([MarshalAs(UnmanagedType.LPStr)] string gpoId,
                                       [MarshalAs(UnmanagedType.LPStr)] string settingJson)
    {
        try
        {
            var setting = JsonSerializer.Deserialize<PolicySetting>(settingJson);
            if (setting == null) return false;
            
            return _manager.SetPolicySettingAsync(gpoId, setting).Result;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Removes a policy setting
    /// </summary>
    [DllExport("RemovePolicySetting", CallingConvention = CallingConvention.Cdecl)]
    public static bool RemovePolicySetting([MarshalAs(UnmanagedType.LPStr)] string gpoId,
                                          [MarshalAs(UnmanagedType.LPStr)] string settingName)
    {
        try
        {
            return _manager.RemovePolicySettingAsync(gpoId, settingName).Result;
        }
        catch (Exception)
        {
            return false;
        }
    }

    #endregion

    #region GPO Linking

    /// <summary>
    /// Links a GPO to an Organizational Unit
    /// </summary>
    [DllExport("LinkGPO", CallingConvention = CallingConvention.Cdecl)]
    public static bool LinkGPO([MarshalAs(UnmanagedType.LPStr)] string gpoId,
                              [MarshalAs(UnmanagedType.LPStr)] string organizationalUnit)
    {
        try
        {
            return _manager.LinkGPOAsync(gpoId, organizationalUnit).Result;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Unlinks a GPO from an Organizational Unit
    /// </summary>
    [DllExport("UnlinkGPO", CallingConvention = CallingConvention.Cdecl)]
    public static bool UnlinkGPO([MarshalAs(UnmanagedType.LPStr)] string gpoId,
                                [MarshalAs(UnmanagedType.LPStr)] string organizationalUnit)
    {
        try
        {
            return _manager.UnlinkGPOAsync(gpoId, organizationalUnit).Result;
        }
        catch (Exception)
        {
            return false;
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Frees memory allocated for string returns
    /// </summary>
    [DllExport("FreeString", CallingConvention = CallingConvention.Cdecl)]
    public static void FreeString(IntPtr ptr)
    {
        if (ptr != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    /// <summary>
    /// Gets the last error message
    /// </summary>
    [DllExport("GetLastError", CallingConvention = CallingConvention.Cdecl)]
    public static IntPtr GetLastError()
    {
        // This would store the last error in a thread-safe manner
        // For now, return a placeholder
        return MarshalString("No error information available");
    }

    #endregion

    #region Private Helper Methods

    private static IntPtr MarshalString(string str)
    {
        if (string.IsNullOrEmpty(str))
            return IntPtr.Zero;

        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str + "\0");
        IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, ptr, bytes.Length);
        return ptr;
    }

    #endregion
}

/// <summary>
/// Attribute to mark methods for DLL export
/// Note: This is a placeholder - actual DLL export requires additional tooling
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class DllExportAttribute : Attribute
{
    public string EntryPoint { get; }
    public CallingConvention CallingConvention { get; set; }

    public DllExportAttribute(string entryPoint)
    {
        EntryPoint = entryPoint;
    }
}
