# Multi-stage build for FinanceSentry API
# Build context: repository root

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy solution and project files (layer 1: stable dependencies)
COPY ["backend/NuGet.Config", "backend/"]
COPY ["backend/FinanceSentry.sln", "backend/"]
COPY ["backend/src/FinanceSentry.API/FinanceSentry.API.csproj", "backend/src/FinanceSentry.API/"]
COPY ["backend/src/FinanceSentry.Core/FinanceSentry.Core.csproj", "backend/src/FinanceSentry.Core/"]
COPY ["backend/src/FinanceSentry.Infrastructure/FinanceSentry.Infrastructure.csproj", "backend/src/FinanceSentry.Infrastructure/"]
COPY ["backend/src/FinanceSentry.Modules.BankSync/FinanceSentry.Modules.BankSync.csproj", "backend/src/FinanceSentry.Modules.BankSync/"]
COPY ["backend/src/FinanceSentry.Modules.Auth/FinanceSentry.Modules.Auth.csproj", "backend/src/FinanceSentry.Modules.Auth/"]
COPY ["backend/tests/FinanceSentry.Tests.Unit/FinanceSentry.Tests.Unit.csproj", "backend/tests/FinanceSentry.Tests.Unit/"]
COPY ["backend/tests/FinanceSentry.Tests.Integration/FinanceSentry.Tests.Integration.csproj", "backend/tests/FinanceSentry.Tests.Integration/"]

# Restore dependencies
WORKDIR /src/backend
RUN dotnet restore FinanceSentry.sln

# Copy source code (layer 2: changes frequently)
COPY ["backend/src/", "src/"]
COPY ["backend/tests/", "tests/"]

# Build application
RUN dotnet build FinanceSentry.sln -c Release

# Run tests before publishing (fail fast on test failures)
RUN dotnet test tests/FinanceSentry.Tests.Unit/FinanceSentry.Tests.Unit.csproj -c Release --no-build --nologo --verbosity minimal

# Publish application
RUN dotnet publish src/FinanceSentry.API/FinanceSentry.API.csproj -c Release -o /app/publish --no-build

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

# Install runtime dependencies
RUN apk add --no-cache curl tzdata

# Set working directory
WORKDIR /app

# Copy published application from build stage
COPY --from=build /app/publish .

# Create logs directory
RUN mkdir -p /app/logs && chown -R nobody:nobody /app

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:5000 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_RUNNING_IN_CONTAINER=true

# Expose port
EXPOSE 5000

# Health check endpoint
HEALTHCHECK --interval=10s --timeout=5s --retries=3 \
    CMD curl -f http://localhost:5000/api/v1/health || exit 1

# Run as non-root user
USER nobody

# Run application
ENTRYPOINT ["dotnet", "FinanceSentry.API.dll"]

