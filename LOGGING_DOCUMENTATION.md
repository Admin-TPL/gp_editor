# Group Policy Editor CLI - Logging Documentation

## Overview

The Group Policy Editor CLI has been enhanced with comprehensive logging capabilities to ensure reliable operation and easy troubleshooting. The logging system uses Serilog with both console and file output to provide detailed insights into the application's behavior.

## Logging Features

### 1. Multi-Level Logging
- **Debug**: Detailed information for troubleshooting
- **Information**: General operational messages
- **Warning**: Non-critical issues that may need attention
- **Error**: Error conditions that affect functionality
- **Fatal**: Critical errors that may cause application termination

### 2. Multiple Output Targets
- **Console**: Real-time feedback during command execution
- **File**: Persistent logging for audit and troubleshooting
- **Structured Logging**: JSON-formatted log entries with rich metadata

### 3. Log File Management
- **Location**: `%APPDATA%\GroupPolicyEditor\Logs\`
- **Naming**: `gp-editor-{Date}.log` (e.g., `gp-editor-20250619.log`)
- **Rotation**: Daily log file rotation
- **Retention**: 30 days of log files are retained automatically

### 4. Verbose Mode
Use the `--verbose` or `-v` flag with any command to enable debug-level logging:
```powershell
GroupPolicyEditor.exe list --verbose
GroupPolicyEditor.exe get --id "12345" --verbose
```

## What Is Logged

### Application Lifecycle
- Application startup and shutdown
- Command line arguments
- Exit codes and execution time
- System information (OS, machine name, user, etc.)
- Security context (admin privileges, user identity)

### Command Execution
- Command parameters and validation
- Execution start and completion times
- Success/failure status
- Performance metrics

### GPO Operations
- Domain connection attempts
- GPO search operations (by ID, name)
- GPO creation, modification, deletion
- Policy setting retrieval and updates
- Data counts and processing times

### API Calls
- Internal API method calls
- Parameter validation
- Response times and result summaries
- Error conditions and exceptions

### Error Handling
- Detailed exception information
- Stack traces for debugging
- Context information at time of error
- Recovery attempts and results

## Log Format

### Console Output
```
[14:30:25 INF] Command Started: list with parameters {"domain":"contoso.com","format":"table","verbose":false}
[14:30:25 DBG] GroupPolicyApi instance created for domain: contoso.com
[14:30:26 INF] Retrieved 15 GPOs from domain contoso.com
[14:30:26 INF] List GPOs operation completed successfully in 1247ms
```

### File Output
```
[2025-06-19 14:30:25.123 +00:00 INF] [GroupPolicyEditor.Program] Command Started: list with parameters {"domain":"contoso.com","format":"table","verbose":false} {"Application":"GroupPolicyEditor","Version":"1.0.0","MachineName":"WORKSTATION01","UserName":"administrator","ProcessId":1234}
```

## Performance Monitoring

The logging system includes built-in performance monitoring that tracks:

### Operation Timing
- Individual command execution times
- API call response times
- Data processing durations
- Overall application runtime

### Resource Usage
- Number of GPOs processed
- Settings retrieved/modified
- Memory and performance patterns

### Examples
```
[14:30:26 INF] Performance: ListGPOs completed in 01:02.345 (1247ms) {"Operation":"ListGPOs","DurationMs":1247,"Domain":"contoso.com","RecordCount":15}
[14:30:27 INF] Data Operation: GetSettings processed 47 records in 892ms GPO: {12345-ABCD}, Format: json
```

## Security Logging

### Privilege Monitoring
The application logs security context information including:
- Current user identity
- Administrative privileges status
- Authentication method
- Security group memberships

### Access Attempts
- Domain connection attempts
- GPO access permissions
- Registry access (where applicable)
- Failed authorization attempts

### Example Security Logs
```
[14:30:24 INF] Security Context: {"UserName":"CONTOSO\\Administrator","IsAdmin":true,"IsAuthenticated":true,"Groups":["S-1-5-32-544","S-1-5-21-..."]}
[14:30:24 WRN] Application is not running with administrator privileges. Some operations may fail.
```

## Troubleshooting with Logs

### Common Scenarios

#### 1. Command Failures
Look for:
- Command validation errors
- API call failures
- Permission issues
- Network connectivity problems

#### 2. Performance Issues
Monitor:
- Operation durations
- Data processing times
- Resource usage patterns
- Network latency

#### 3. Access Denied Errors
Check:
- Security context logs
- Domain connection status
- Administrative privileges
- Group Policy permissions

### Log Analysis Tips

#### Finding Specific Operations
Search for patterns like:
- `Command Started: {command_name}`
- `GPO Operation: {operation_type}`
- `API Call Started: {method_name}`

#### Performance Analysis
Look for:
- `Performance:` entries for timing data
- `Data Operation:` entries for throughput metrics
- High duration values in milliseconds

#### Error Investigation
Search for:
- `Error in {context}:` for detailed error information
- `failed after {duration}ms` for timeout issues
- Stack trace information in exception logs

## Configuration

### Log Level Configuration
The application automatically configures logging based on:
- Command line `--verbose` flag
- Environment conditions
- Default settings (Information level)

### File Location
Default log directory: `%APPDATA%\GroupPolicyEditor\Logs\`

You can find your logs at:
```
C:\Users\{YourUsername}\AppData\Roaming\GroupPolicyEditor\Logs\
```

### Retention Policy
- **Daily Rotation**: New log file each day
- **Retention Period**: 30 days
- **Automatic Cleanup**: Old files removed automatically

## Best Practices

### For Administrators
1. **Enable verbose logging** when troubleshooting issues
2. **Monitor log files** for recurring warnings or errors
3. **Include log excerpts** when reporting issues
4. **Check security context** logs for permission issues

### For Developers
1. **Use structured logging** with consistent property names
2. **Include relevant context** in log messages
3. **Log both successes and failures** for complete audit trail
4. **Follow performance logging** patterns for consistency

### For Support
1. **Request verbose logs** for detailed troubleshooting
2. **Look for patterns** across multiple operations
3. **Check timestamps** for timing-related issues
4. **Correlate errors** with system events

## Example Usage

### Basic Logging
```powershell
# Standard logging (Information level)
GroupPolicyEditor.exe list --domain contoso.com

# Check logs
Get-Content "$env:APPDATA\GroupPolicyEditor\Logs\gp-editor-$(Get-Date -Format 'yyyy-MM-dd').log" | Select-Object -Last 20
```

### Verbose Logging
```powershell
# Enable debug logging
GroupPolicyEditor.exe list --domain contoso.com --verbose

# Monitor logs in real-time
Get-Content "$env:APPDATA\GroupPolicyEditor\Logs\gp-editor-$(Get-Date -Format 'yyyy-MM-dd').log" -Wait -Tail 10
```

### Performance Monitoring
```powershell
# Run operation and check performance
GroupPolicyEditor.exe settings --gpo-id "12345" --verbose

# Search for performance metrics
Select-String "Performance:" "$env:APPDATA\GroupPolicyEditor\Logs\gp-editor-$(Get-Date -Format 'yyyy-MM-dd').log"
```

## Conclusion

The comprehensive logging system ensures that the Group Policy Editor CLI provides full visibility into its operations, making it easy to:
- Troubleshoot issues quickly
- Monitor performance
- Audit administrative actions
- Maintain compliance
- Support users effectively

All logs are structured, timestamped, and include relevant context to make analysis and troubleshooting as efficient as possible.
