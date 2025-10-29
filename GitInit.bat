@echo off
echo Initializing Git repository...
git init

echo.
echo Linking to GitHub repository: https://github.com/DarkKing1201/SpaceWar
git remote add origin https://github.com/DarkKing1201/SpaceWar.git

echo.
echo Git has been initialized and linked to GitHub.
echo.
echo To add files and commit, run: GitCommit.bat
echo To push manually, run: git push -u origin master
echo.
pause

