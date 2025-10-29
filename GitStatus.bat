@echo off
echo Git Repository Status:
echo ========================================
echo.
git status

echo.
echo ========================================
echo Recent commits:
echo ========================================
git log --oneline -5

echo.
pause

