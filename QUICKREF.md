# Quick Reference - Readarr with OpenLibrary

## 🚀 Getting Started (Choose One)

### 1. Docker (Easiest - Recommended)
```bash
docker-compose up -d
```
📖 Full guide: [DOCKER.md](DOCKER.md)

### 2. Quick Build Script
```bash
./quick-start.sh
./_output/net6.0/linux-x64/Readarr/Readarr --nobrowser
```

### 3. Full Build
```bash
./build.sh --backend
```

### 4. Development
```bash
cd src/NzbDrone.Console
dotnet run -- --nobrowser
```

## 📚 Documentation

| Guide | Purpose |
|-------|---------|
| [README-OPENLIBRARY.md](README-OPENLIBRARY.md) | Main overview and features |
| [DOCKER.md](DOCKER.md) | Complete Docker guide |
| [SETUP.md](SETUP.md) | Detailed setup instructions |
| [replit.md](replit.md) | Technical architecture |

## 🔗 Common URLs

- **Web UI**: http://localhost:8787
- **API Search**: http://localhost:8787/api/v1/search?term=dune
- **Health Check**: http://localhost:8787/ping

## 🐳 Docker Commands

```bash
# Start
docker-compose up -d

# Stop
docker-compose down

# Logs
docker-compose logs -f

# Restart
docker-compose restart

# Build
./docker-build.sh
```

## 🧪 Test OpenLibrary

```bash
# Search books
curl "http://localhost:8787/api/v1/search?term=dune"

# Search by ISBN
curl "http://localhost:8787/api/v1/book?asin=9780441172719"

# Search authors
curl "http://localhost:8787/api/v1/author/lookup?term=herbert"
```

## 📁 File Structure

```
.
├── src/                              # Source code
│   └── NzbDrone.Core/
│       └── MetadataSource/
│           └── OpenLibrary/          # OpenLibrary implementation ⭐
├── Dockerfile                        # Docker build file
├── docker-compose.yml                # Docker Compose config
├── docker-build.sh                   # Docker build script
├── quick-start.sh                    # Quick build script
├── build.sh                          # Full build script
└── README-OPENLIBRARY.md             # Main documentation
```

## ⚡ What's Different?

| Feature | Before | After |
|---------|--------|-------|
| Metadata | Goodreads ❌ | OpenLibrary ✅ |
| API Key | Required | Not needed |
| Rate Limits | Yes | No |
| Status | Deprecated | Active |

## 🛠️ Troubleshooting

| Problem | Solution |
|---------|----------|
| Port 8787 in use | Change port in docker-compose.yml |
| Permission errors | `sudo chown -R 1000:1000 config/` |
| No search results | Check internet, test OpenLibrary API |
| Container won't start | Check logs: `docker-compose logs` |

## 📦 Requirements

- Docker + Docker Compose **OR**
- .NET 6.0+ SDK
- Internet connection (for OpenLibrary API)

## 🔐 Default Credentials

None! Set up authentication on first launch via the web UI.

## 📊 Resource Usage

- **Memory**: ~512MB typical
- **CPU**: Low (spikes during searches)
- **Disk**: ~500MB + your book library

## 🎯 Next Steps

1. ✅ Start Readarr (Docker or build script)
2. ✅ Open http://localhost:8787
3. ✅ Complete setup wizard
4. ✅ Add authors/books
5. ✅ Configure download clients
6. ✅ Enjoy automated book management with OpenLibrary metadata!

---

**Need help?** Check the detailed guides above or open an issue.
