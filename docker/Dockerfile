# Multi-stage build for FinanceSentry API

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy project files
COPY ["backend/src/FinanceSentry.API/FinanceSentry.API.csproj", "FinanceSentry.API/"]
COPY ["backend/src/FinanceSentry.Core/FinanceSentry.Core.csproj", "FinanceSentry.Core/"]
COPY ["backend/src/FinanceSentry.Infrastructure/FinanceSentry.Infrastructure.csproj", "FinanceSentry.Infrastructure/"]
COPY ["backend/src/FinanceSentry.Modules.BankSync/FinanceSentry.Modules.BankSync.csproj", "FinanceSentry.Modules.BankSync/"]

# Restore dependencies
RUN dotnet restore "FinanceSentry.API/FinanceSentry.API.csproj"

# Copy source code
COPY ["backend/", "."]

# Build
RUN dotnet build "FinanceSentry.API/FinanceSentry.API.csproj" -c Release -o /app/build

# Publish
RUN dotnet publish "FinanceSentry.API/FinanceSentry.API.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

# Install curl for health checks
RUN apk add --no-cache curl

# Copy published application
COPY --from=build /app/publish .

# Expose port
EXPOSE 5000

# Health check
HEALTHCHECK --interval=10s --timeout=5s --retries=3 \
    CMD curl -f http://localhost:5000/api/v1/health || exit 1

# Run application
ENTRYPOINT ["dotnet", "FinanceSentry.API.dll"]
