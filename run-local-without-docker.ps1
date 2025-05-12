#!/usr/bin/env pwsh

# Set environment variables for local execution without Docker
$env:TIKTOK_HOST = "mockhost"
$env:CLICKHOUSE_CONN = "Host=localhost;Port=8123;Database=chakal;User=default;Password=Chakal123!"
$env:LOG_LEVEL = "Debug"
$env:DEBUG_MODE = "true"

# Navegar al directorio del proyecto principal
cd .\Chakal.IngestSystem

# Ejecutar la aplicación directamente
Write-Host "Ejecutando Chakal.IngestSystem en modo de depuración (simulación)..."
dotnet run --environment Development 