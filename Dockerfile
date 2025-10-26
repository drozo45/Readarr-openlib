# Multi-stage Dockerfile for Readarr with OpenLibrary
# Stage 1: Build backend
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS backend-build

WORKDIR /src

# Copy solution and project files
COPY src/*.sln ./
COPY src/Directory.Build.props ./
COPY src/NzbDrone.Core/*.csproj ./NzbDrone.Core/
COPY src/NzbDrone.Common/*.csproj ./NzbDrone.Common/
COPY src/NzbDrone.Host/*.csproj ./NzbDrone.Host/
COPY src/NzbDrone.Console/*.csproj ./NzbDrone.Console/
COPY src/NzbDrone.Update/*.csproj ./NzbDrone.Update/
COPY src/NzbDrone.SignalR/*.csproj ./NzbDrone.SignalR/
COPY src/Readarr.Http/*.csproj ./Readarr.Http/
COPY src/Readarr.Api.V1/*.csproj ./Readarr.Api.V1/
COPY src/NzbDrone.Mono/*.csproj ./NzbDrone.Mono/
COPY src/NzbDrone.Windows/*.csproj ./NzbDrone.Windows/

# Restore dependencies
COPY src/NuGet.config ./
RUN dotnet restore Readarr.sln --runtime linux-x64

# Copy source code
COPY src/ ./

# Build and publish
RUN dotnet publish NzbDrone.Console/Readarr.Console.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained false \
    --no-restore \
    -o /app \
    /p:EnableCompressionInSingleFile=false

# Stage 2: Build frontend (optional, can be skipped for backend-only)
FROM node:16-alpine AS frontend-build

WORKDIR /src

# Copy frontend files
COPY package.json yarn.lock ./
COPY frontend/ ./frontend/

# Install and build (with fallback to skip if it fails)
RUN yarn install --frozen-lockfile --network-timeout 120000 || echo "Frontend dependencies skipped"
RUN yarn build --env production || echo "Frontend build skipped"

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

# Copy frontend from build stage (if available)
COPY --from=frontend-build /src/_output/UI ./UI/ 2>/dev/null || echo "No frontend UI copied"

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
