# AprsNet

[![Build](https://github.com/bartjay/AprsNet/actions/workflows/build.yml/badge.svg)](https://github.com/bartjay/AprsNet/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)

APRS packet processing system for .NET. Connects to APRS-IS, parses packets, stores them in PostgreSQL.

## Features

- **Parsing**: Mic-E, Weather, Position, Maidenhead grid locators
- **Storage**: PostgreSQL with EF Core, Redis for caching/deduplication
- **Real-time**: SignalR hub for live packet streaming
- **Observability**: OpenTelemetry metrics, Prometheus endpoint

## Quick Start

```bash
git clone https://github.com/bartjay/AprsNet.git
cd AprsNet

# Copy config files
cp src/Aprs.Api/appsettings.example.json src/Aprs.Api/appsettings.json
cp src/Aprs.Worker/appsettings.example.json src/Aprs.Worker/appsettings.json

# Run with Docker
docker-compose up --build
```

- API: http://localhost:5000/swagger
- Health: http://localhost:5000/health
- Metrics: http://localhost:5000/metrics
- SignalR: ws://localhost:5000/hubs/packets

## Configuration

Edit `appsettings.json`:

```json
{
  "Aprs": {
    "Callsign": "N0CALL",
    "Password": "-1",
    "Filter": "r/52/21/500"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=aprs;Username=postgres;Password=xxx",
    "Redis": "localhost:6379"
  }
}
```

## Running Locally

```bash
# Start Postgres and Redis
docker-compose up -d postgres redis

# Run migrations
dotnet ef database update --project src/Aprs.Infrastructure --startup-project src/Aprs.Api

# Start API
dotnet run --project src/Aprs.Api

# Start Worker (separate terminal)
dotnet run --project src/Aprs.Worker
```

## API

```http
GET /api/v1/packets?page=1&pageSize=50&sender=N0CALL
```

```json
{
  "items": [
    {
      "id": "...",
      "sender": "N0CALL",
      "type": "PositionWithTimestamp",
      "latitude": 52.2297,
      "longitude": 21.0122,
      "receivedAt": "2025-01-15T12:00:00Z"
    }
  ],
  "page": 1,
  "totalCount": 1234
}
```

## Project Structure

```
src/
├── Aprs.Domain/          # Entities, Value Objects
├── Aprs.Application/     # Commands, Queries, DTOs
├── Aprs.Infrastructure/  # Parsers, Repositories, APRS-IS client
├── Aprs.Api/             # REST API
├── Aprs.Worker/          # Background service
└── Aprs.Sdk/             # Client library
```

## Tests

```bash
dotnet test
```

148 unit tests covering parsers, validators, handlers.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

MIT
