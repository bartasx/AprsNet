# Base stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Aprs.Api/Aprs.Api.csproj", "src/Aprs.Api/"]
COPY ["src/Aprs.Worker/Aprs.Worker.csproj", "src/Aprs.Worker/"]
COPY ["src/Aprs.Application/Aprs.Application.csproj", "src/Aprs.Application/"]
COPY ["src/Aprs.Domain/Aprs.Domain.csproj", "src/Aprs.Domain/"]
COPY ["src/Aprs.Infrastructure/Aprs.Infrastructure.csproj", "src/Aprs.Infrastructure/"]
COPY ["src/Aprs.Sdk/Aprs.Sdk.csproj", "src/Aprs.Sdk/"]

RUN dotnet restore "src/Aprs.Api/Aprs.Api.csproj"
RUN dotnet restore "src/Aprs.Worker/Aprs.Worker.csproj"

COPY . .
WORKDIR "/src/src/Aprs.Api"
RUN dotnet build "Aprs.Api.csproj" -c Release -o /app/build

WORKDIR "/src/src/Aprs.Worker"
RUN dotnet build "Aprs.Worker.csproj" -c Release -o /app/build

# Publish API
FROM build AS publish-api
WORKDIR "/src/src/Aprs.Api"
RUN dotnet publish "Aprs.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Publish Worker
FROM build AS publish-worker
WORKDIR "/src/src/Aprs.Worker"
RUN dotnet publish "Aprs.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final API
FROM base AS api
WORKDIR /app
COPY --from=publish-api /app/publish .
ENTRYPOINT ["dotnet", "Aprs.Api.dll"]

# Final Worker
FROM base AS worker
WORKDIR /app
COPY --from=publish-worker /app/publish .
ENTRYPOINT ["dotnet", "Aprs.Worker.dll"]
