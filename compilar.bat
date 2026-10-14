@echo off
REM Genera un ejecutable unico (no requiere tener .NET instalado en el equipo destino)
dotnet publish SistemaCodice.csproj -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
echo.
echo Ejecutable generado en: publish\SistemaCodice.exe
pause
