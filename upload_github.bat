@echo off
setlocal

cd /d "%~dp0"

git rev-parse --is-inside-work-tree >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Current folder is not a Git repository.
    pause
    exit /b 1
)

for /f "usebackq delims=" %%i in (`git branch --show-current`) do set "BRANCH=%%i"
if not defined BRANCH (
    echo [ERROR] Could not detect the current branch.
    pause
    exit /b 1
)

set "COMMIT_MSG=%~1"
if "%COMMIT_MSG%"=="" (
    set /p "COMMIT_MSG=Commit message (leave blank for default): "
)
if "%COMMIT_MSG%"=="" (
    set "COMMIT_MSG=Update Unity shooter project"
)

echo.
echo [1/3] Staging files...
git add -A
if errorlevel 1 (
    echo [ERROR] git add failed.
    pause
    exit /b 1
)

git diff --cached --quiet
if errorlevel 1 (
    goto commit
)

echo [INFO] No staged changes to commit.
goto push

:commit
echo [2/3] Creating commit...
git commit -m "%COMMIT_MSG%"
if errorlevel 1 (
    echo [ERROR] git commit failed.
    pause
    exit /b 1
)

:push
echo [3/3] Pushing to origin/%BRANCH% ...
git push origin "%BRANCH%"
if errorlevel 1 (
    echo [ERROR] git push failed.
    pause
    exit /b 1
)

echo.
echo Upload completed successfully.
pause
exit /b 0
