@echo off
echo Pushing to GitHub repository: https://github.com/DarkKing1201/SpaceWar
echo.
git push -u origin master

echo.
echo ========================================
if %ERRORLEVEL% EQU 0 (
    echo Push completed successfully!
) else (
    echo Push failed. Check the error messages above.
)
echo ========================================
echo.
pause

