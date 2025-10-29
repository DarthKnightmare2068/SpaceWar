@echo off
echo Staging all changes...
git add .

echo.
echo Current status:
git status

echo.
echo Enter your commit message:
echo (Or press Enter to use default message "Update Unity project")
set /p commitMessage=""

if "%commitMessage%"=="" (
    set commitMessage=Update Unity project
)

echo.
echo Committing changes with message: %commitMessage%
git commit -m "%commitMessage%"

echo.
echo ========================================
echo Commit completed!
echo.
echo To push to GitHub, run:
echo   git push -u origin master
echo.
echo Or use: GitPush.bat
echo ========================================
echo.
pause

