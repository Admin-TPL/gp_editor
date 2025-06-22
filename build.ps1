#!/usr/bin/env pwsh
# Build script for Group Policy Editor CLI tool

param(
    [string]$Configuration = "Release",
    [switch]$RunTests,
    [switch]$Publish,
    [switch]$Clean,
    [switch]$Help
)

if ($Help) {
    Write-Host "Group Policy Editor CLI Build Script"
    Write-Host "===================================="
    Write-Host ""
    Write-Host "Usage: ./build.ps1 [options]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -Configuration <Debug|Release>  Build configuration (default: Release)"
    Write-Host "  -RunTests                       Run unit tests after build"
    Write-Host "  -Publish                        Create a single-file executable"
    Write-Host "  -Clean                          Clean build artifacts before building"
    Write-Host "  -Help                           Show this help message"
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  ./build.ps1                     # Build in Release mode"
    Write-Host "  ./build.ps1 -Configuration Debug -RunTests"
    Write-Host "  ./build.ps1 -Publish            # Build and create executable"
    exit 0
}

# Set error handling
$ErrorActionPreference = "Stop"

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

Write-Host "Group Policy Editor CLI - Build Script" -ForegroundColor Green
Write-Host "=======================================" -ForegroundColor Green
Write-Host ""

# Clean if requested
if ($Clean) {
    Write-Host "Cleaning build artifacts..." -ForegroundColor Yellow
    if (Test-Path "src/GroupPolicyEditor/bin") {
        Remove-Item "src/GroupPolicyEditor/bin" -Recurse -Force
    }
    if (Test-Path "src/GroupPolicyEditor/obj") {
        Remove-Item "src/GroupPolicyEditor/obj" -Recurse -Force
    }
    if (Test-Path "tests/GroupPolicyEditor.Tests/bin") {
        Remove-Item "tests/GroupPolicyEditor.Tests/bin" -Recurse -Force
    }
    if (Test-Path "tests/GroupPolicyEditor.Tests/obj") {
        Remove-Item "tests/GroupPolicyEditor.Tests/obj" -Recurse -Force
    }
    Write-Host "✓ Clean completed" -ForegroundColor Green
    Write-Host ""
}

# Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

# Check .NET SDK
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET SDK version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ .NET SDK not found. Please install .NET 8.0 SDK" -ForegroundColor Red
    exit 1
}

# Check if we're on Windows
if ($PSVersionTable.Platform -ne "Win32NT" -and $PSVersionTable.PSVersion.Major -ge 6) {
    Write-Host "⚠ Warning: Group Policy is Windows-specific. Some features may not work." -ForegroundColor Yellow
}

Write-Host ""

# Restore dependencies
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore GroupPolicyEditor.sln
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Package restore failed" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Package restore completed" -ForegroundColor Green
Write-Host ""

# Build solution
Write-Host "Building solution ($Configuration)..." -ForegroundColor Yellow
dotnet build GroupPolicyEditor.sln --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Build completed successfully" -ForegroundColor Green
Write-Host ""

# Run tests if requested
if ($RunTests) {
    Write-Host "Running unit tests..." -ForegroundColor Yellow
    dotnet test GroupPolicyEditor.sln --configuration $Configuration --no-build --verbosity normal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ Tests failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ All tests passed" -ForegroundColor Green
    Write-Host ""
}

# Publish CLI executable if requested
if ($Publish) {
    Write-Host "Publishing CLI executable..." -ForegroundColor Yellow
    
    # Create publish directory
    $PublishDir = "publish"
    if (-not (Test-Path $PublishDir)) {
        New-Item -ItemType Directory -Path $PublishDir -Force | Out-Null
    }
    
    # Publish as single file executable
    dotnet publish src/GroupPolicyEditor/GroupPolicyEditor.csproj `
        --configuration $Configuration `
        --runtime win-x64 `
        --self-contained true `
        --output $PublishDir `
        --no-restore
        
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ Publish failed" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✓ CLI executable published to $PublishDir" -ForegroundColor Green
    Write-Host ""
    
    # Show usage
    Write-Host "Usage:" -ForegroundColor Green
    Write-Host "  .\publish\GroupPolicyEditor.exe --help" -ForegroundColor Cyan
    Write-Host "  .\publish\GroupPolicyEditor.exe list" -ForegroundColor Cyan
    Write-Host "  .\publish\GroupPolicyEditor.exe get --name 'Default Domain Policy'" -ForegroundColor Cyan
    Write-Host ""
}

# Show build summary
Write-Host "Build Summary" -ForegroundColor Green
Write-Host "=============" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan
Write-Host "Tests run: $(if ($RunTests) { 'Yes' } else { 'No' })" -ForegroundColor Cyan
Write-Host "Published: $(if ($Publish) { 'Yes' } else { 'No' })" -ForegroundColor Cyan

# Show output locations
$OutputDir = "src/GroupPolicyEditor/bin/$Configuration/net8.0"
if (Test-Path $OutputDir) {
    Write-Host ""
    Write-Host "Build outputs:" -ForegroundColor Green
    Write-Host "  CLI executable: $OutputDir/GroupPolicyEditor.exe" -ForegroundColor Cyan
    
    if ($Publish -and (Test-Path "publish/GroupPolicyEditor.exe")) {
        Write-Host "  Published executable: publish/GroupPolicyEditor.exe" -ForegroundColor Cyan
    }
}

Write-Host ""
Write-Host "✓ Build script completed successfully!" -ForegroundColor Green
