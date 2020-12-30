@echo off
setlocal EnableDelayedExpansion

if exist perf.csv (del perf.csv)

dotnet build --configuration Release
if errorlevel 1 (exit /b 1)

for %%x in (1000 3000 10000 30000 100000 300000 1000000 3000000 10000000 30000000) do (
    bin\Release\net5.0\BoardTest.exe puzzles %%x perf.csv
    if errorlevel 1 (exit /b 1)
)

exit /b 1
