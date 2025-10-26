# Docker Deployment Guide - Readarr with OpenLibrary

This guide covers deploying Readarr with OpenLibrary metadata integration using Docker.

## Quick Start

### Using Docker Compose (Recommended)

```bash
# 1. Start the container
docker-compose up -d

# 2. Check logs
docker-compose logs -f

# 3. Access Readarr
# Open http://localhost:8787
```

That's it! The container will automatically:
- Build the application
- Set up the config directory
- Start Readarr with OpenLibrary integration
- Restart automatically on failure

### Using Docker CLI

```bash
# Build the image
docker build -t readarr-openlibrary:latest .

# Run the container
docker run -d \
  --name readarr-openlibrary \
  -p 8787:8787 \
  -v ./config:/config \
  -v ./books:/books \
  -v ./downloads:/downloads \
  -e TZ=America/New_York \
  readarr-openlibrary:latest
```

### Using the Build Script

```bash
# Simple build
./docker-build.sh

# With custom tag
./docker-build.sh --tag v1.0.0

# For ARM64 (e.g., Raspberry Pi)
./docker-build.sh --platform linux/arm64

# Build without cache
./docker-build.sh --no-cache
```

## Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `READARR_CONFIG` | `/config` | Configuration directory path |
| `TZ` | `America/New_York` | Timezone for logs and scheduling |
| `PUID` | `1000` | User ID for file permissions |
| `PGID` | `1000` | Group ID for file permissions |

### Volumes

Mount these directories for persistent data:

| Container Path | Purpose | Required |
|----------------|---------|----------|
| `/config` | Database, logs, settings | ✅ Yes |
| `/books` | Book library | Optional |
| `/downloads` | Download client output | Optional |

### Ports

| Port | Purpose |
|------|---------|
| `8787` | Web UI and API (default) |

## Docker Compose Configuration

### Basic Setup

```yaml
version: '3.8'

services:
  readarr:
    build: .
    container_name: readarr-openlibrary
    restart: unless-stopped
    ports:
      - "8787:8787"
    volumes:
      - ./config:/config
      - ./books:/books
    environment:
      - TZ=America/New_York
```

### Advanced Setup with Download Clients

```yaml
version: '3.8'

services:
  readarr:
    build: .
    container_name: readarr-openlibrary
    restart: unless-stopped
    ports:
      - "8787:8787"
    volumes:
      - ./config:/config
      - ./books:/books
      - ./downloads:/downloads
    environment:
      - TZ=America/New_York
      - PUID=1000
      - PGID=1000
    depends_on:
      - qbittorrent
    networks:
      - media

  qbittorrent:
    image: lscr.io/linuxserver/qbittorrent:latest
    container_name: qbittorrent
    restart: unless-stopped
    ports:
      - "8080:8080"
      - "6881:6881"
      - "6881:6881/udp"
    volumes:
      - ./qbittorrent-config:/config
      - ./downloads:/downloads
    environment:
      - TZ=America/New_York
    networks:
      - media

networks:
  media:
    driver: bridge
```

### With Calibre Integration

```yaml
version: '3.8'

services:
  readarr:
    build: .
    container_name: readarr-openlibrary
    restart: unless-stopped
    ports:
      - "8787:8787"
    volumes:
      - ./config:/config
      - ./books:/books
      - calibre-library:/calibre
    environment:
      - TZ=America/New_York
    depends_on:
      - calibre-web
    networks:
      - media

  calibre-web:
    image: lscr.io/linuxserver/calibre-web:latest
    container_name: calibre-web
    restart: unless-stopped
    ports:
      - "8083:8083"
    volumes:
      - ./calibre-web-config:/config
      - calibre-library:/books
    environment:
      - TZ=America/New_York
    networks:
      - media

volumes:
  calibre-library:

networks:
  media:
    driver: bridge
```

## Docker Commands

### Container Management

```bash
# Start container
docker-compose up -d

# Stop container
docker-compose down

# Restart container
docker-compose restart

# View logs
docker-compose logs -f readarr

# Enter container shell
docker-compose exec readarr /bin/bash

# Update container
docker-compose pull
docker-compose up -d
```

### Troubleshooting

```bash
# Check container status
docker-compose ps

# View resource usage
docker stats readarr-openlibrary

# Inspect container
docker inspect readarr-openlibrary

# Check health
docker-compose exec readarr curl http://localhost:8787/ping
```

## Building for Different Architectures

### AMD64 (Standard Intel/AMD)
```bash
docker build --platform linux/amd64 -t readarr-openlibrary:amd64 .
```

### ARM64 (Raspberry Pi 4, Apple Silicon)
```bash
docker build --platform linux/arm64 -t readarr-openlibrary:arm64 .
```

### Multi-Architecture Build
```bash
# Enable buildx
docker buildx create --use

# Build for multiple platforms
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  -t readarr-openlibrary:latest \
  --push \
  .
```

## Health Checks

The Docker image includes a health check that:
- Runs every 30 seconds
- Checks if the API is responsive
- Allows 60 seconds for startup
- Marks unhealthy after 3 failed checks

Check health status:
```bash
docker inspect --format='{{.State.Health.Status}}' readarr-openlibrary
```

## Data Persistence

### Backup Configuration

```bash
# Stop container
docker-compose down

# Backup config directory
tar -czf readarr-backup-$(date +%Y%m%d).tar.gz config/

# Restart container
docker-compose up -d
```

### Restore Configuration

```bash
# Stop container
docker-compose down

# Restore from backup
tar -xzf readarr-backup-20241026.tar.gz

# Restart container
docker-compose up -d
```

## Performance Tuning

### Resource Limits

Add to `docker-compose.yml`:

```yaml
services:
  readarr:
    # ... other settings ...
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 1G
        reservations:
          cpus: '0.5'
          memory: 512M
```

### Logging

Configure logging driver:

```yaml
services:
  readarr:
    # ... other settings ...
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
```

## Testing the OpenLibrary Integration

Once the container is running:

```bash
# Search for books
curl "http://localhost:8787/api/v1/search?term=dune"

# Search by ISBN
curl "http://localhost:8787/api/v1/book?asin=9780441172719"

# Check author lookup
curl "http://localhost:8787/api/v1/author/lookup?term=asimov"
```

## Security

### Run as Non-Root User

The Docker image runs as the `readarr` user (non-root) by default for security.

### Network Security

Consider using Docker networks to isolate services:

```yaml
networks:
  frontend:
    driver: bridge
  backend:
    driver: bridge
    internal: true
```

### Secrets Management

For sensitive data, use Docker secrets:

```yaml
secrets:
  api_key:
    file: ./secrets/api_key.txt

services:
  readarr:
    secrets:
      - api_key
```

## Troubleshooting

### Common Issues

**Build fails with "Could not find a part of the path '/Logo/64.png'":**
This error occurs if the Logo directory is not copied into the Docker build context. The Dockerfile includes:
```dockerfile
COPY Logo ../Logo
```
This ensures the embedded resource files are available during compilation.

**Container won't start:**
```bash
# Check logs
docker-compose logs readarr

# Check if port is in use
netstat -tuln | grep 8787

# Try different port
# Edit docker-compose.yml: "8788:8787"
```

**Permission errors:**
```bash
# Fix permissions
sudo chown -R 1000:1000 config/ books/

# Or match your user ID
id -u  # Get your UID
# Update PUID in docker-compose.yml
```

**Can't access web UI:**
```bash
# Verify container is running
docker ps | grep readarr

# Check container health
docker inspect readarr-openlibrary | grep Health

# Check container network
docker network inspect bridge
```

**OpenLibrary searches not working:**
```bash
# Test internet connectivity from container
docker-compose exec readarr curl https://openlibrary.org/search.json?q=test

# Check logs for API errors
docker-compose logs readarr | grep -i openlibrary
```

## Updating

### Update Container

```bash
# Pull latest code
git pull

# Rebuild and restart
docker-compose down
docker-compose build --no-cache
docker-compose up -d
```

### Auto-Update with Watchtower

```yaml
services:
  watchtower:
    image: containrrr/watchtower
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
    command: --interval 86400  # Check daily
```

## Production Deployment

### Behind Reverse Proxy (Nginx)

```nginx
server {
    listen 80;
    server_name readarr.example.com;
    
    location / {
        proxy_pass http://localhost:8787;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### With SSL (Using Let's Encrypt)

```yaml
services:
  readarr:
    # ... readarr config ...
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.readarr.rule=Host(`readarr.example.com`)"
      - "traefik.http.routers.readarr.tls.certresolver=letsencrypt"
      
  traefik:
    image: traefik:latest
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
      - ./traefik.yml:/traefik.yml
      - ./acme.json:/acme.json
```

## Support

For issues specific to:
- **Docker setup**: Check this guide
- **OpenLibrary integration**: See README-OPENLIBRARY.md
- **General Readarr**: Check SETUP.md

## License

GNU GPL v3 (same as Readarr)
