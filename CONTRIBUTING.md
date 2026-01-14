# Contributing to AprsNet

First off, thank you for considering contributing to AprsNet! It's people like you that make AprsNet such a great tool for the amateur radio community.

## Code of Conduct

This project and everyone participating in it is governed by our commitment to providing a welcoming and inclusive environment. Please be respectful and considerate in all interactions.

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check existing issues to avoid duplicates. When creating a bug report, include as many details as possible:

- **Use a clear and descriptive title**
- **Describe the exact steps to reproduce the problem**
- **Provide specific examples** (raw APRS packets that fail to parse, etc.)
- **Describe the expected behavior**
- **Include your environment details** (.NET version, OS, etc.)

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion:

- **Use a clear and descriptive title**
- **Provide a detailed description of the proposed functionality**
- **Explain why this enhancement would be useful**
- **List any alternative solutions you've considered**

### Pull Requests

1. **Fork the repo** and create your branch from `main`
2. **Follow the coding style** - we use `.editorconfig` for consistent formatting
3. **Add tests** for any new functionality
4. **Ensure the test suite passes** (`dotnet test`)
5. **Update documentation** if needed
6. **Write clear commit messages** following [Conventional Commits](https://www.conventionalcommits.org/)

## Development Setup

### Prerequisites

- .NET 10 SDK
- Docker & Docker Compose (for running dependencies)
- PostgreSQL 15+ (or use Docker)
- Redis (or use Docker)

### Getting Started

```bash
# Clone your fork
git clone https://github.com/YOUR_USERNAME/AprsNet.git
cd AprsNet

# Start dependencies
docker-compose up -d postgres redis

# Restore packages
dotnet restore

# Build
dotnet build

# Run tests
dotnet test

# Run the API
dotnet run --project src/Aprs.Api

# Run the Worker
dotnet run --project src/Aprs.Worker
```

### Project Structure

```
src/
├── Aprs.Domain/          # Core domain entities, value objects, interfaces
├── Aprs.Application/     # CQRS handlers, business logic
├── Aprs.Infrastructure/  # External concerns (DB, Redis, APRS-IS client)
├── Aprs.Api/            # REST API
├── Aprs.Worker/         # Background service for ingestion
└── Aprs.Sdk/            # Public SDK for consumers

tests/
├── Aprs.UnitTests/
└── Aprs.IntegrationTests/
```

## Coding Guidelines

### General

- Use **C# 12+ features** where appropriate
- Follow **Clean Architecture** principles
- Keep the **Domain layer free of external dependencies**
- Use **async/await** consistently
- Prefer **records** for DTOs and Value Objects

### Naming Conventions

- **PascalCase** for public members, types, namespaces
- **camelCase** with `_` prefix for private fields
- **Interfaces** prefixed with `I`
- **Async methods** suffixed with `Async`

### Testing

- Use **xUnit** as the test framework
- Use **FluentAssertions** for assertions
- Use **Moq** for mocking
- Name tests using pattern: `MethodName_Scenario_ExpectedResult`
- Aim for **80%+ code coverage** on parsers

### Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
feat: add compressed position parsing
fix: handle invalid callsign format gracefully
docs: update API documentation
test: add MicE parser edge cases
refactor: extract weather parsing logic
chore: update dependencies
```

## APRS-Specific Guidelines

When working on APRS parsing:

1. **Reference the APRS specification** (APRS101.pdf)
2. **Test with real-world packets** from APRS-IS
3. **Handle edge cases gracefully** - don't crash on malformed data
4. **Log parsing failures** at appropriate levels
5. **Consider backwards compatibility** with older packet formats

## Questions?

Feel free to open an issue for any questions about contributing!

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
