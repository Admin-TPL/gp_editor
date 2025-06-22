# Group Policy Configuration Commands

This document contains the CLI commands needed to configure specific Group Policy settings using the GroupPolicyEditor CLI tool.

## Prerequisites

- Run PowerShell as Administrator
- Ensure the GroupPolicyEditor.exe is built and accessible

## Build the CLI Tool

```powershell
# Build the project
.\build.ps1

# Or create a single-file executable
.\build.ps1 -Publish
```

## Configuration Commands

### 1. Hide Network-Proxy Settings Page

**Policy Path**: `Computer Configuration > Administrative Templates > Control Panel > Settings Page Visibility`  
**Setting**: Hide:Network-Proxy

```powershell
# Set the policy to hide Network-Proxy settings page
GroupPolicyEditor.exe set `
  --gpo-id "LOCAL_COMPUTER_POLICY" `
  --name "SettingsPageVisibility" `
  --value "hide:network-proxy" `
  --type "String"
```

### 2. Restrict Clipboard Transfer from Server to Client (RDP)

**Policy Path**: `User Configuration → Administrative Templates → Windows Components → Remote Desktop Services → Remote Desktop Session Host → Device and resource redirection`
**Setting**: Restrict clipboard transfer from server to client → Enabled

# Alternative using published executable

```powershell
GroupPolicyEditor.exe set `
  --gpo-id "LOCAL_USER_POLICY" `
  --name "fDisableClipboardRedirection" `
  --value "1" `
  --type "Integer"
```

## Registry Paths (For Reference)

These are the underlying registry paths that these policies modify:

### Settings Page Visibility

- **Registry Path**: `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer`
- **Value Name**: `SettingsPageVisibility`
- **Value Type**: `REG_SZ` (String)
- **Value Data**: `hide:network-proxy`

### Clipboard Redirection Restriction

- **Registry Path**: `HKEY_CURRENT_USER\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services`
- **Value Name**: `fDisableClipboardRedirection`
- **Value Type**: `REG_DWORD` (Integer)
- **Value Data**: `1` (Enabled) / `0` (Disabled)

## Verification Commands

Check if the policies were applied correctly:

```powershell
# List all settings for Local Computer Policy
GroupPolicyEditor.exe settings --gpo-id "LOCAL_COMPUTER_POLICY"

# List all settings for Local User Policy
GroupPolicyEditor.exe settings --gpo-id "LOCAL_USER_POLICY"

# Output as JSON for easier parsing
GroupPolicyEditor.exe settings --gpo-id "LOCAL_COMPUTER_POLICY" --format json

## Apply Changes

After setting the policies, you may need to:

1. **Refresh Group Policy**:

   ```powershell
   gpupdate /force
   ```

2. **Restart Windows Explorer** (for Settings Page Visibility):

   ```powershell
   Stop-Process -Name "explorer" -Force
   Start-Process "explorer"
   ```

3. **Log off and log back in** (for user policies)

## Notes

- These commands assume you're working with local policies (`LOCAL_COMPUTER_POLICY` and `LOCAL_USER_POLICY`)
- For domain policies, replace the GPO ID with the actual domain GPO GUID
- Some policy changes require a restart or logoff/logon to take effect
- Always test these changes in a non-production environment first

## Troubleshooting

If the commands don't work as expected:

1. Ensure you're running as Administrator
2. Check that the CLI tool is built correctly:
   ```powershell
   .\src\GroupPolicyEditor\bin\Release\net8.0\win-x64\GroupPolicyEditor.exe --help
   ```
3. Verify the policy was set:
   ```powershell
   .\src\GroupPolicyEditor\bin\Release\net8.0\win-x64\GroupPolicyEditor.exe settings --gpo-id "LOCAL_COMPUTER_POLICY" | findstr "SettingsPageVisibility"
   ```
