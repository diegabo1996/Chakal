#!/usr/bin/env pwsh

# Start ClickHouse container if not already running
$containerName = "chakal-clickhouse-test"
$containerExists = docker ps -a --format "{{.Names}}" | Select-String -Pattern $containerName

if (-not $containerExists) {
    Write-Host "Creating and starting ClickHouse container..."
    docker run -d --name $containerName `
        -p 8123:8123 -p 9000:9000 `
        -e CLICKHOUSE_USER=default `
        -e CLICKHOUSE_PASSWORD=Chakal123! `
        -e CLICKHOUSE_DB=chakal `
        -e CLICKHOUSE_DEFAULT_ACCESS_MANAGEMENT=1 `
        -v ${PWD}/sql/chakal_clickhouse.sql:/docker-entrypoint-initdb.d/chakal_clickhouse.sql `
        -v ${PWD}/init-clickhouse.sh:/docker-entrypoint-initdb.d/init-clickhouse.sh `
        clickhouse/clickhouse-server:23.8
} elseif (-not (docker ps --format "{{.Names}}" | Select-String -Pattern $containerName)) {
    Write-Host "Starting existing ClickHouse container..."
    docker start $containerName
} else {
    Write-Host "ClickHouse container is already running."
}

# Wait for ClickHouse to be ready
Write-Host "Waiting for ClickHouse to be ready..."
$ready = $false
$attempts = 0
$maxAttempts = 30

while (-not $ready -and $attempts -lt $maxAttempts) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:8123/ping" -Method GET -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            $ready = $true
            Write-Host "ClickHouse is ready!"
        }
    } catch {
        $attempts++
        Write-Host "Waiting for ClickHouse to be ready... ($attempts/$maxAttempts)"
        Start-Sleep -Seconds 1
    }
}

if (-not $ready) {
    Write-Host "ClickHouse did not become ready in time. Please check the container logs."
    exit 1
}

# Set environment variables for the application
$env:TIKTOK_HOST = "mockhost"
$env:CLICKHOUSE_CONN = "Host=localhost;Port=8123;Database=chakal;User=default;Password=Chakal123!"
$env:LOG_LEVEL = "Debug"
$env:DEBUG_MODE = "true"
$env:BULK_BATCH_SIZE = "10"  # Small batch size for testing
$env:BULK_WAIT_MS = "5000"   # Longer wait time for testing

Write-Host "Environment variables set for testing:"
Write-Host "TIKTOK_HOST: $env:TIKTOK_HOST"
Write-Host "CLICKHOUSE_CONN: $env:CLICKHOUSE_CONN"
Write-Host "LOG_LEVEL: $env:LOG_LEVEL"
Write-Host "DEBUG_MODE: $env:DEBUG_MODE"
Write-Host "BULK_BATCH_SIZE: $env:BULK_BATCH_SIZE"
Write-Host "BULK_WAIT_MS: $env:BULK_WAIT_MS"

# Run the application
Write-Host "Starting the application..."
dotnet run --project Chakal.IngestSystem 