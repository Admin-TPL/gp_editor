# Group Policy Editor CLI

A command-line tool for managing Windows Group Policy settings.

## Overview

This project provides a comprehensive command-line interface for managing Windows Group Policy Objects (GPOs) and their settings. The CLI tool is built with .NET 8.0 and offers a modern, easy-to-use interface for Group Policy administration.

## Features

### Core Features

- ✅ List all Group Policy Objects
- ✅ Get GPO details by ID or name
- ✅ Read policy settings from GPOs
- ✅ Set and remove policy settings (local policies)
- ✅ Multiple output formats (table, JSON, CSV)
- ✅ Create new GPOs
- ✅ Delete existing GPOs
- ✅ Domain and local policy support

### CLI Features

- Modern command-line interface using System.CommandLine
- Intuitive subcommands and options
- Rich help system
- Multiple output formats
- Single-file executable option

## Requirements

### System Requirements

- .NET 8.0 SDK (for building)
- .NET 8.0 Runtime (for running)
- Windows OS (Group Policy is Windows-specific)
- Administrator privileges (for modifying policies)

## Installation

### Building the .NET Library

1. **Clone or download the project**
2. **Open PowerShell in the project directory**
3. **Build the solution:**
   ```powershell
   dotnet build GroupPolicyEditor.sln --configuration Release
   ```
4. **Run tests:**

   ```powershell dotnet test

   ```

5. **Create a single-file executable (optional):**

   ```powershell
   ./build.ps1 -Publish
   ```

   This creates a self-contained executable in the `publish` folder that doesn't require .NET runtime to be installed.

## Usage

### Command Line Usage

The CLI provides several commands for managing Group Policy Objects:

#### List all GPOs

```bash
# List all GPOs in table format
GroupPolicyEditor.exe list

# List with specific domain
GroupPolicyEditor.exe list --domain contoso.com

# Output as JSON
GroupPolicyEditor.exe list --format json

# Output as CSV
GroupPolicyEditor.exe list --format csv
```

#### Get specific GPO details

```bash
# Get GPO by name
GroupPolicyEditor.exe get --name "Default Domain Policy"

# Get GPO by ID
GroupPolicyEditor.exe get --id "{31B2F340-016D-11D2-945F-00C04FB984F9}"

# Output as JSON
GroupPolicyEditor.exe get --name "Default Domain Policy" --format json
```

#### Create a new GPO

```bash
# Create a new GPO
GroupPolicyEditor.exe create "My New Policy" --description "Custom policy for testing"
```

#### Delete a GPO

```bash
# Delete a GPO by ID
GroupPolicyEditor.exe delete --id "{GUID-HERE}"
```

#### Manage policy settings

```bash
# Get all settings for a GPO
GroupPolicyEditor.exe settings --gpo-id "LOCAL_COMPUTER_POLICY"

# Set a policy setting
GroupPolicyEditor.exe set --gpo-id "LOCAL_COMPUTER_POLICY" --name "PasswordComplexity" --value "1" --type Boolean
```

### API Usage in C#

```csharp
using GroupPolicyEditor.Api;

// Initialize the API
var api = new GroupPolicyApi();

// Get all GPOs
var gpos = await api.GetAllGPOsAsync();
foreach (var gpo in gpos)
{
    Console.WriteLine($"GPO: {gpo.Name} ({gpo.Id})");
}

// Get policy settings
var settings = await api.GetPolicySettingsAsync("LOCAL_COMPUTER_POLICY");
foreach (var setting in settings)
{
    Console.WriteLine($"Setting: {setting.Name} = {setting.Value}");
}

// Set a policy setting
bool success = await api.SetPolicySettingAsync(
    "LOCAL_COMPUTER_POLICY",
    "TestSetting",
    "TestValue",
    @"SOFTWARE\Policies\TestApp"
);
```

## Project Structure

```
src/
├── GroupPolicyEditor/
│   ├── Program.cs              # CLI entry point
│   ├── GroupPolicyEditor.csproj # Project configuration
│   ├── Api/
│   │   └── GroupPolicyApi.cs   # High-level API
│   ├── Core/
│   │   ├── GroupPolicyManager.cs # Core implementation
│   │   └── GroupPolicyModels.cs  # Data models
│   └── Interop/
│       └── GroupPolicyInterop.cs # Windows interop
tests/
└── GroupPolicyEditor.Tests/
    ├── GroupPolicyTests.cs     # Unit tests
    └── GroupPolicyEditor.Tests.csproj
```

    # Search for security settings
    security_settings = await manager.get_security_settings()
    print(f"Found {len(security_settings)} security settings")

    # Find settings by keyword
    password_settings = await manager.find_settings_by_keyword("password")
    for setting in password_settings:
        print(f"Password setting: {setting['setting']['name']}")

asyncio.run(advanced_example())

```

## Project Structure

```

gp_editor/
├── GroupPolicyEditor.sln # Visual Studio solution
├── src/
│ └── GroupPolicyEditor/
│ ├── GroupPolicyEditor.csproj # Main project file
│ ├── Core/ # Core functionality
│ │ ├── GroupPolicyModels.cs # Data models
│ │ └── GroupPolicyManager.cs# Main implementation
│ ├── Api/ # High-level API
│ │ └── GroupPolicyApi.cs # Simplified API
│ └── Interop/ # C-style exports
│ └── GroupPolicyInterop.cs# DLL exports
├── tests/
│ └── GroupPolicyEditor.Tests/ # Unit tests

## API Reference

### Core Classes

#### GroupPolicyManager

Main class for Group Policy operations.

**Methods:**

- `GetAllGPOsAsync()` - Get all GPOs
- `GetGPOByIdAsync(string id)` - Get GPO by ID
- `GetGPOByNameAsync(string name)` - Get GPO by name
- `GetPolicySettingsAsync(string gpoId)` - Get policy settings
- `SetPolicySettingAsync(string gpoId, PolicySetting setting)` - Set policy setting
- `RemovePolicySettingAsync(string gpoId, string settingName)` - Remove setting

#### GroupPolicyApi

Simplified API for common operations.

**Methods:**

- `GetAllGPOsAsync()` - Returns `List<GroupPolicyInfo>`
- `SetPolicySettingAsync(gpoId, name, value, path, type)` - Simplified setting

### Data Models

#### GroupPolicyInfo

Represents a Group Policy Object.

**Properties:**

- `Id`, `Name`, `Description`, `Domain`
- `CreationDate`, `ModificationDate`, `Version`

#### PolicySetting

Represents a policy setting.

**Properties:**

- `Name`, `Value`, `Type`
- `RegistryPath`, `RegistryKey`

## CLI Commands Reference

### Global Options

- `--domain <domain>` - Specify domain (leave empty for local)
- `--format <format>` - Output format: table (default), json, csv

### Available Commands

- `list` - List all Group Policy Objects
- `get` - Get details of a specific GPO
- `create <name>` - Create a new GPO
- `delete --id <id>` - Delete a GPO
- `settings --gpo-id <id>` - Get policy settings for a GPO
- `set --gpo-id <id> --name <name> --value <value>` - Set a policy setting

## Limitations and Notes

### Current Limitations

1. **Domain Operations**: Creating, deleting, and linking GPOs to OUs require additional COM interop implementation
2. **Permissions**: Setting policy values requires administrator privileges
3. **Windows Only**: Group Policy is Windows-specific
4. **Local Focus**: Current implementation is optimized for local policy management

### Security Considerations

- Always run with appropriate permissions
- Test policy changes in a safe environment
- Backup existing policies before making changes
- Validate policy settings before applying

### Performance Notes

- Reading policy settings may be slow for large numbers of settings
- Registry operations are synchronous in the current implementation
- Consider caching for frequently accessed data

## Development

### Building from Source

```powershell
# Build the solution
dotnet build --configuration Release

# Run tests
dotnet test --configuration Release

# Create single-file executable
./build.ps1 -Publish
```

### Running Tests

```powershell
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "TestMethodName"
```

### Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass
6. Submit a pull request

## Troubleshooting

### Common Issues

1. **"Assembly not found" error**

   - Ensure the CLI tool is built properly
   - Check that .NET 8.0 runtime is installed

2. **"Access denied" when setting policies**

   - Run as administrator
   - Check that the target registry path exists and is writable

3. **"GPO not found" errors**
   - Verify you're running on a domain-joined machine for domain GPOs
   - Check that the GPO ID/name is correct

### Debug Mode

Enable debug logging by setting environment variables:

```powershell
$env:DOTNET_LOGGING_LEVEL = "Debug"
$env:GP_EDITOR_DEBUG = "true"
```

## License

This project is provided as-is for educational and development purposes. Please ensure compliance with your organization's policies and Microsoft's licensing terms when using Group Policy management functionality.

## Support

For issues and questions:

1. Check the troubleshooting section above
2. Review the CLI help: `GroupPolicyEditor.exe --help`
3. Ensure all requirements are met
4. Test with the provided unit tests

## Roadmap

Future enhancements may include:

- [ ] Full COM interop for domain GPO management
- [ ] PowerShell module wrapper
- [ ] GUI management interface
- [ ] Advanced filtering and search capabilities
- [ ] Import/export functionality for GPO templates
- [ ] Integration with Active Directory management
#   g p _ e d i t o r  
 