@echo off
setlocal

echo ========================================
echo IMPACT Support API Build Script
echo ========================================

echo.
echo [1/6] Cleaning project...
dotnet clean
if errorlevel 1 goto error

echo.
echo [2/6] Restoring packages...
dotnet restore
if errorlevel 1 goto error

echo.
echo [3/6] Building project...
dotnet build
if errorlevel 1 goto error

echo.
echo [4/6] Checking dotnet-ef...
dotnet ef --version
if errorlevel 1 (
    echo dotnet-ef not found. Installing...
    dotnet tool install --global dotnet-ef
    if errorlevel 1 goto error
)

echo.
echo [5/6] Creating migration...
dotnet ef migrations add InitialSupportChatSqlite
if errorlevel 1 (
    echo Migration may already exist. Continuing to database update...
)

echo.
echo [6/6] Updating SQLite database...
dotnet ef database update
if errorlevel 1 goto error

echo.
echo ========================================
echo Build and database update completed.
echo Starting API on HTTPS profile...
echo ========================================
echo.

dotnet run --launch-profile https
goto end

:error
echo.
echo ========================================
echo Build failed. Check the error above.
echo ========================================
exit /b 1

:end
endlocal