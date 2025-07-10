@echo off
setlocal EnableDelayedExpansion

REM Service Worker Cache Version Bumper (Windows)
REM Usage: bump-cache-version.bat [major|minor|patch] [-y] [-nb]
REM Default: patch
REM Flags:
REM   -y  : Auto-accept (skip confirmation)
REM   -nb : No backup (skip backup creation)

set "SCRIPT_DIR=%~dp0"
set "SERVICE_WORKER_FILE=%SCRIPT_DIR%wwwroot\service-worker.js"

REM Parse arguments
set "AUTO_ACCEPT=false"
set "NO_BACKUP=false"
set "BUMP_TYPE="

:parse_args
if "%~1"=="" goto :args_done
if /i "%~1"=="-y" (
    set "AUTO_ACCEPT=true"
    shift
    goto :parse_args
)
if /i "%~1"=="--yes" (
    set "AUTO_ACCEPT=true"
    shift
    goto :parse_args
)
if /i "%~1"=="-nb" (
    set "NO_BACKUP=true"
    shift
    goto :parse_args
)
if /i "%~1"=="--no-backup" (
    set "NO_BACKUP=true"
    shift
    goto :parse_args
)
if /i "%~1"=="major" (
    set "BUMP_TYPE=major"
    shift
    goto :parse_args
)
if /i "%~1"=="minor" (
    set "BUMP_TYPE=minor"
    shift
    goto :parse_args
)
if /i "%~1"=="patch" (
    set "BUMP_TYPE=patch"
    shift
    goto :parse_args
)
if "%~1" NEQ "" (
    if "%~1:~0,1%"=="-" (
        echo [91m❌[0m Unknown flag: %~1
        echo Usage: %0 [major^|minor^|patch] [-y] [-nb]
        echo Flags:
        echo   -y, --yes       Auto-accept (skip confirmation)
        echo   -nb, --no-backup No backup (skip backup creation)
        exit /b 1
    ) else (
        if "!BUMP_TYPE!"=="" (
            set "BUMP_TYPE=%~1"
        ) else (
            echo [91m❌[0m Unknown argument: %~1
            exit /b 1
        )
    )
)
shift
goto :parse_args

:args_done

if not exist "%SERVICE_WORKER_FILE%" (
    echo [91m❌[0m Service worker file not found: %SERVICE_WORKER_FILE%
    exit /b 1
)

REM Get current version from service worker
for /f "tokens=2 delims='" %%a in ('findstr "const CACHE_VERSION = " "%SERVICE_WORKER_FILE%"') do set "CURRENT_VERSION=%%a"

if "!CURRENT_VERSION!"=="" (
    echo [91m❌[0m Could not extract current cache version from service worker
    exit /b 1
)

echo [94mℹ[0m Current cache version: !CURRENT_VERSION!

REM Parse version components
for /f "tokens=1,2,3 delims=." %%a in ("!CURRENT_VERSION!") do (
    set "MAJOR=%%a"
    set "MINOR=%%b"
    set "PATCH=%%c"
)

REM Determine bump type (default to patch)
if "!BUMP_TYPE!"=="" set "BUMP_TYPE=patch"

if /i "!BUMP_TYPE!"=="major" (
    set /a "NEW_MAJOR=!MAJOR!+1"
    set "NEW_MINOR=0"
    set "NEW_PATCH=0"
    set "CHANGE_DESCRIPTION=Major version bump (breaking changes or complete overhaul)"
) else if /i "!BUMP_TYPE!"=="minor" (
    set "NEW_MAJOR=!MAJOR!"
    set /a "NEW_MINOR=!MINOR!+1"
    set "NEW_PATCH=0"
    set "CHANGE_DESCRIPTION=Minor version bump (new features, asset additions)"
) else if /i "!BUMP_TYPE!"=="patch" (
    set "NEW_MAJOR=!MAJOR!"
    set "NEW_MINOR=!MINOR!"
    set /a "NEW_PATCH=!PATCH!+1"
    set "CHANGE_DESCRIPTION=Patch version bump (bug fixes, small tweaks)"
) else (
    echo [91m❌[0m Invalid bump type: !BUMP_TYPE!
    echo Usage: %0 [major^|minor^|patch] [-y] [-nb]
    echo.
    echo Bump types:
    echo   major - Breaking changes or complete cache strategy overhaul
    echo   minor - New features, asset additions, strategy changes
    echo   patch - Bug fixes, small tweaks (default)
    echo.
    echo Flags:
    echo   -y, --yes       Auto-accept (skip confirmation)
    echo   -nb, --no-backup No backup (skip backup creation)
    exit /b 1
)

set "NEW_VERSION=!NEW_MAJOR!.!NEW_MINOR!.!NEW_PATCH!"

echo [94mℹ[0m !CHANGE_DESCRIPTION!
echo [93m⚠[0m Version change: !CURRENT_VERSION! → !NEW_VERSION!
echo.

if /i "!AUTO_ACCEPT!"=="false" (
    set /p "CONFIRM=Do you want to proceed? (y/N): "
    if /i not "!CONFIRM!"=="y" (
        echo [94mℹ[0m Version bump cancelled
        exit /b 0
    )
) else (
    echo [94mℹ[0m Auto-accepting changes (-y flag enabled)
)

REM Create backup unless disabled
if /i "!NO_BACKUP!"=="false" (
    set "BACKUP_FILE=%SERVICE_WORKER_FILE%.backup.%DATE:~10,4%%DATE:~4,2%%DATE:~7,2%_%TIME:~0,2%%TIME:~3,2%%TIME:~6,2%"
    set "BACKUP_FILE=!BACKUP_FILE: =0!"
    copy "%SERVICE_WORKER_FILE%" "!BACKUP_FILE!" >nul
    echo [94mℹ[0m Backup created: service-worker.js.backup...
) else (
    echo [93m⚠[0m Backup skipped (-nb flag enabled)
    set "BACKUP_FILE="
)

REM Update the version in service worker
powershell -Command "(Get-Content '%SERVICE_WORKER_FILE%') -replace \"const CACHE_VERSION = '[^']*'\", \"const CACHE_VERSION = '!NEW_VERSION!'\" | Set-Content '%SERVICE_WORKER_FILE%'"

REM Verify the change
for /f "tokens=2 delims='" %%a in ('findstr "const CACHE_VERSION = " "%SERVICE_WORKER_FILE%"') do set "NEW_VERSION_CHECK=%%a"

if "!NEW_VERSION_CHECK!"=="!NEW_VERSION!" (
    echo [92m✅[0m Cache version successfully updated to !NEW_VERSION!
    echo [94mℹ[0m Cache names will be:
    echo [94mℹ[0m   • neighbortools-static-v!NEW_VERSION!
    echo [94mℹ[0m   • neighbortools-dynamic-v!NEW_VERSION!
    echo.
    echo [93m⚠[0m Remember to:
    echo [93m⚠[0m   1. Test the service worker in development
    echo [93m⚠[0m   2. Commit these changes to git
    echo [93m⚠[0m   3. Deploy to force cache refresh for all users
) else (
    echo [91m❌[0m Version update failed. Version check: !NEW_VERSION_CHECK!
    if not "!BACKUP_FILE!"=="" if exist "!BACKUP_FILE!" (
        move "!BACKUP_FILE!" "%SERVICE_WORKER_FILE%" >nul
        echo [94mℹ[0m Backup restored
    )
    exit /b 1
)

echo.
echo [94mℹ[0m Changed in service-worker.js:
echo - const CACHE_VERSION = '!CURRENT_VERSION!';
echo + const CACHE_VERSION = '!NEW_VERSION!';

echo [92m✅[0m Cache version bump complete!