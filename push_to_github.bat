@echo off
setlocal EnableExtensions EnableDelayedExpansion

cd /d "%~dp0"

where git >nul 2>nul
if errorlevel 1 (
    echo [ERROR] Git was not found in PATH.
    pause
    exit /b 1
)

git rev-parse --is-inside-work-tree >nul 2>nul
if errorlevel 1 (
    echo [ERROR] This folder is not a Git repository.
    pause
    exit /b 1
)

set "BRANCH="
for /f "delims=" %%B in ('git rev-parse --abbrev-ref HEAD 2^>nul') do set "BRANCH=%%B"

if not defined BRANCH (
    echo [ERROR] Could not determine the current branch.
    pause
    exit /b 1
)

if /i "!BRANCH!"=="HEAD" (
    echo [ERROR] You are in a detached HEAD state. Please switch to a branch first.
    pause
    exit /b 1
)

set "MESSAGE=%~1"
if not defined MESSAGE set "MESSAGE=Auto upload"

echo [INFO] Current branch: !BRANCH!
echo [INFO] Staging all changes...
git add -A
if errorlevel 1 (
    echo [ERROR] git add failed.
    pause
    exit /b 1
)

git diff --cached --quiet
if not errorlevel 1 (
    echo [INFO] Nothing to commit.
    pause
    exit /b 0
)

echo [INFO] Committing with message: !MESSAGE!
git commit -m "!MESSAGE!"
if errorlevel 1 (
    echo [ERROR] git commit failed.
    pause
    exit /b 1
)

echo [INFO] Pushing to origin/!BRANCH! ...
git push -u origin HEAD
if errorlevel 1 (
    echo [ERROR] git push failed.
    pause
    exit /b 1
)

echo [SUCCESS] Uploaded to GitHub successfully.
pause
exit /b 0
