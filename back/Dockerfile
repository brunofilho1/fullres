# syntax=docker/dockerfile:1

# ---------------------------------------------------------------------------
# Build stage
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore against the project file alone so code changes do not invalidate the
# NuGet layer cache.
COPY FullRes.Api/FullRes.Api.csproj FullRes.Api/
RUN dotnet restore FullRes.Api/FullRes.Api.csproj

COPY FullRes.Api/ FullRes.Api/
RUN dotnet publish FullRes.Api/FullRes.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ---------------------------------------------------------------------------
# Runtime stage
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# SkiaSharp (image re-encoding) loads its native library via fontconfig at runtime.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production

# Run as the non-root user that ships with the .NET images.
USER app

# PaaS platforms (Render) inject the port to bind through $PORT; fall back to 8080
# for local `docker run`. ASPNETCORE_URLS must be set at runtime so it can expand.
EXPOSE 8080
CMD ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet FullRes.Api.dll"]
