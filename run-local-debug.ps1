#!/usr/bin/env pwsh

# Set environment variables for local debugging
$env:TIKTOK_HOST = "mockhost"
$env:CLICKHOUSE_CONN = "Host=localhost;Port=8123;Database=chakal;User=default;Password=Chakal123!"
$env:LOG_LEVEL = "Debug"
$env:DEBUG_MODE = "true"
$env:BULK_BATCH_SIZE = "5000"
$env:BULK_WAIT_MS = "500"

Write-Host "Environment variables set for local debugging:"
Write-Host "TIKTOK_HOST: $env:TIKTOK_HOST"
Write-Host "CLICKHOUSE_CONN: $env:CLICKHOUSE_CONN"
Write-Host "LOG_LEVEL: $env:LOG_LEVEL"
Write-Host "DEBUG_MODE: $env:DEBUG_MODE"
Write-Host "BULK_BATCH_SIZE: $env:BULK_BATCH_SIZE"
Write-Host "BULK_WAIT_MS: $env:BULK_WAIT_MS"

# Run the application
dotnet run --project Chakal.IngestSystem 