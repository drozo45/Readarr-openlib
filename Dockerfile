# Multi-stage Dockerfile for Readarr with OpenLibrary
# Stage 1: Build backend
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS backend-build

WORKDIR /src

# Copy everything for dependency restore (for better layer caching, copy projects first)
COPY src/Readarr.sln src/Directory.Build.props src/NuGet.config ./

# Copy all source (includes all .csproj files)
COPY src ./

# Restore dependencies for the Console project only (avoids missing test projects)
RUN dotnet restore NzbDrone.Console/Readarr.Console.csproj --runtime linux-x64

# Build and publish (specify framework for multi-target projects)
RUN dotnet publish NzbDrone.Console/Readarr.Console.csproj \
    -c Release \
    -f net6.0 \
    -r linux-x64 \
    --self-contained false \
    --no-restore \
    -o /app \
    /p:EnableCompressionInSingleFile=false \
    /p:EnforceCodeStyleInBuild=false \
    /p:TreatWarningsAsErrors=false

# Stage 2: Build frontend (optional, can be skipped for backend-only)
FROM node:16-alpine AS frontend-build

WORKDIR /build

# Copy frontend package files from root
COPY package.json yarn.lock ./

# Copy frontend source code
COPY frontend ./frontend

# Install and build (with fallback to create empty dir if it fails)
RUN yarn install --frozen-lockfile --network-timeout 120000 || echo "Frontend dependencies skipped"
RUN yarn build --env production || mkdir -p _output/UI
RUN mkdir -p _output/UI || true

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:6.0

# Install runtime dependencies
RUN apt-get update && apt-get install -y \
    libicu-dev \
    curl \
    sqlite3 \
    && rm -rf /var/lib/apt/lists/*

# Create app user
RUN groupadd -r readarr && useradd -r -g readarr readarr

# Set working directory
WORKDIR /app

# Copy backend from build stage
COPY --from=backend-build /app ./

# Copy frontend from build stage (directory always exists, may be empty)
COPY --from=frontend-build /build/_output/UI ./UI/

# Create config directory
RUN mkdir -p /config && chown -R readarr:readarr /config /app

# Expose port
EXPOSE 8787

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8787/ping || exit 1

# Switch to app user
USER readarr

# Set environment variables
ENV READARR_CONFIG=/config

# Run Readarr
ENTRYPOINT ["./Readarr", "--nobrowser", "--data=/config"]
