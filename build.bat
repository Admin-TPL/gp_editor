@echo off
REM Build script for Group Policy Editor CLI tool (Windows Batch version)

echo Group Policy Editor CLI - Quick Build
echo ======================================
echo.

REM Check if dotnet is available
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo Error: .NET SDK not found. Please install .NET 8.0 SDK
    pause
    exit /b 1
)

echo Building CLI solution...
dotnet build GroupPolicyEditor.sln --configuration Release
if errorlevel 1 (
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo Build completed successfully!
echo.

REM Ask if user wants to run tests
set /p RUNTESTS=Run tests? (y/n): 
if /i "%RUNTESTS%"=="y" (
    echo Running tests...
    dotnet test --configuration Release --no-build
    if errorlevel 1 (
        echo Some tests failed!
        pause
        exit /b 1
    )
    echo All tests passed!
    echo.
)

REM Ask if user wants to setup Python
set /p SETUPPY=Setup Python integration? (y/n): 
if /i "%SETUPPY%"=="y" (
    echo Setting up Python integration...
    
    REM Create python/lib directory
    if not exist "python\lib" mkdir "python\lib"
    
    REM Copy built DLL
    copy "src\GroupPolicyEditor\bin\Release\net8.0\GroupPolicyEditor.dll" "python\lib\" >nul
    if errorlevel 1 (
        echo Failed to copy DLL
        pause
        exit /b 1
    )
    
    echo Python setup completed!
    echo.
    echo You can now:
    echo   1. cd python
    echo   2. pip install -r requirements.txt
    echo   3. python group_policy_editor.py
    echo.
)

echo Build script completed!
pause
