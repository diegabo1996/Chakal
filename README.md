# Chakal.IngestSystem

A real-time data ingestion system for TikTok livestreams using Clean Architecture in .NET 8.

## Architecture

This solution follows Clean Architecture principles with the following components:

- **Chakal.Core**: Core domain models and interfaces
- **Chakal.Application**: Application services and use cases
- **Chakal.Infrastructure**: Implementation of data services and external sources
- **Chakal.Infrastructure.Models**: Database model objects (POCOs)
- **Chakal.IngestSystem**: Worker service for data ingestion

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Docker](https://www.docker.com/products/docker-desktop/)
- [Docker Compose](https://docs.docker.com/compose/install/)

## Configuration

The application uses the following environment variables:

| Variable | Description | Default |
|----------|-------------|---------|
| `TIKTOK_HOST` | TikTok username to capture data from | - |
| `CLICKHOUSE_CONN` | ClickHouse connection string | - |
| `LOG_LEVEL` | Logging level (Debug, Information, Warning, Error, Critical) | Debug |
| `DEBUG_MODE` | Generate mock data (true/false) | false |

See the `env.sample` file for examples.

## Building and Running

### Using Docker Compose

The easiest way to run the application is using Docker Compose:

```bash
# Start the application with Docker Compose
docker-compose up --build
```

This will start:
- The ingest worker service (on port 5000)
- ClickHouse database (on ports 8123 and 9000)

### Using Debug Mode Locally

For local development with mock data:

```powershell
# PowerShell script to run with debug mode
./run-local-debug.ps1
```

### Initialize ClickHouse Database

The ClickHouse schema can be initialized using:

```bash
# Initialize ClickHouse with the schema
./init-clickhouse.sh
```

## API Endpoints

- **Health Check**: http://localhost:5000/healthz
- **Metrics**: http://localhost:5000/metrics (Prometheus compatible)

## Development Notes

- When `DEBUG_MODE=true`, the system generates mock events every 3 seconds
- The system uses a bounded channel for persistence (5000 events max, 500ms flush time)
- ClickHouse bulk insertion is used for optimal performance 