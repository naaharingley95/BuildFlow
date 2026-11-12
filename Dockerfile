# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and publish
COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create directory for SQLite database (persistent volume on Railway)
RUN mkdir -p /app/data

COPY --from=build /app/publish .

# Environment
ENV ASPNETCORE_ENVIRONMENT=Production
# Railway injects $PORT dynamically; fall back to 8080 for local Docker runs
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ConnectionStrings__SqliteConnection="Data Source=/app/data/buildflow.db"

EXPOSE ${PORT:-8080}

ENTRYPOINT ["dotnet", "BuildFlow.dll"]

