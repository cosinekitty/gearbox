@echo off
setlocal EnableDelayedExpansion
if "%1" == "" (
    set CONFIG=Release
) else (
    set CONFIG=%1
)
if exist ..\..\tables\*.endgame (del ..\..\tables\*.endgame)

dotnet build --configuration !CONFIG!
if errorlevel 1 (exit /b 1)

bin\!CONFIG!\net5.0\EndgameTableGen.exe pgen 2 4
if errorlevel 1 (exit /b 1)

exit /b 0
