#!/usr/bin/env pwsh

# Set environment variables for local debug mode
$env:TIKTOK_HOST = "mockhost"
$env:CLICKHOUSE_CONN = "Host=localhost;Port=8123;Database=chakal;User=default;Password=Chakal123!"
$env:LOG_LEVEL = "Debug"
$env:DEBUG_MODE = "true"

# Build and run with Docker Compose
Write-Host "Building and running with Docker Compose..."
docker-compose up --build 