# syntax=docker/dockerfile:1
# Multi-stage build for FinanceSentry API
# Build context: repository root

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY backend/ backend/

WORKDIR /src/backend
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore FinanceSentry.sln

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet build FinanceSentry.sln -c Release

RUN dotnet test tests/FinanceSentry.Tests.Unit/FinanceSentry.Tests.Unit.csproj -c Release --no-build --nologo --verbosity minimal

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish src/FinanceSentry.API/FinanceSentry.API.csproj -c Release -o /app/publish --no-build

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# curl/tzdata for health + timezones; postgresql16-client (pg_dump/pg_restore/createdb/dropdb) + age
# power the feature-024 off-host backup + restore-drill jobs. A v16 client dumps/restores the v14
# server (supported newer-client→older-server direction); backups no-op when BACKUP_* is unset.
RUN apk add --no-cache curl tzdata postgresql16-client age

COPY --from=build /app/publish .

RUN mkdir -p /app/logs && chown -R nobody:nobody /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:5000 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 5000

HEALTHCHECK --interval=10s --timeout=5s --retries=3 \
    CMD curl -f http://localhost:5000/api/v1/health || exit 1

USER nobody

ENTRYPOINT ["dotnet", "FinanceSentry.API.dll"]
