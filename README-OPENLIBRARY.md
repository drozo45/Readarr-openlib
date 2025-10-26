# Readarr with OpenLibrary Integration

> **Modified version of Readarr using OpenLibrary instead of Goodreads for metadata**

This is a fork of [Readarr](https://github.com/Readarr/Readarr) with the Goodreads metadata source replaced by OpenLibrary (https://openlibrary.org/).

## ⚡ Quick Start

### Option 1: Simple Build (Backend Only)
```bash
chmod +x quick-start.sh
./quick-start.sh
```

Then run:
```bash
./_output/net6.0/linux-x64/Readarr/Readarr --nobrowser
```

### Option 2: Full Build (Backend + Frontend)
```bash
chmod +x build.sh
./build.sh --all
```

### Option 3: Development Mode
```bash
cd src/NzbDrone.Console
dotnet run -- --nobrowser
```

## 🔄 What Changed?

### Metadata Source: Goodreads → OpenLibrary

All metadata is now fetched from OpenLibrary API:

| Feature | Goodreads (Old) | OpenLibrary (New) |
|---------|----------------|-------------------|
| **API Key** | Required | Not required ✅ |
| **Rate Limits** | Yes | No ✅ |
| **Status** | Deprecated ❌ | Active ✅ |
| **Data** | Books, Authors, Reviews | Books, Authors, ISBNs ✅ |
| **Cost** | Discontinued | Free & Open ✅ |

### Implementation

The new OpenLibrary integration is located in:
```
src/NzbDrone.Core/MetadataSource/OpenLibrary/
```

Key files:
- `OpenLibraryBookInfoProxy.cs` - Main implementation
- `OpenLibraryProxy.cs` - Core API wrapper
- `OpenLibrarySearchProxy.cs` - Search functionality
- `Resources/` - API response models

### How It Works

1. **Search**: Uses OpenLibrary Search API (`/search.json`)
2. **Book Details**: Fetches work and edition data (`/works/` and `/books/` endpoints)
3. **Authors**: Retrieves from `/authors/` endpoint
4. **Caching**: 7-30 day cache to minimize API calls

## 📖 API Endpoints

Once running (default: http://localhost:8787):

### Search for books
```bash
curl "http://localhost:8787/api/v1/search?term=dune"
```

### Search by ISBN
```bash
curl "http://localhost:8787/api/v1/book?asin=9780441172719"
```

### Search authors
```bash
curl "http://localhost:8787/api/v1/author/lookup?term=frank+herbert"
```

## 🛠️ Requirements

- **.NET 6.0 SDK** or later ([Download](https://dotnet.microsoft.com/download))
- **Optional**: Node.js 16+ and yarn (for frontend development)

## 📦 Self-Hosting

### Docker (Recommended)
```bash
# Build
dotnet publish src/Readarr.sln -c Release -o ./publish

# Run
docker run -d \
  --name=readarr-openlibrary \
  -p 8787:8787 \
  -v /path/to/data:/config \
  --restart unless-stopped \
  /path/to/publish/Readarr
```

### Systemd Service (Linux)
```bash
# Build release
./build.sh --backend

# Copy to installation directory
sudo cp -r _output/net6.0/linux-x64/Readarr /opt/readarr

# Create systemd service
sudo nano /etc/systemd/system/readarr.service
```

**Service file:**
```ini
[Unit]
Description=Readarr with OpenLibrary
After=network.target

[Service]
Type=simple
User=readarr
WorkingDirectory=/opt/readarr
ExecStart=/opt/readarr/Readarr --nobrowser --data=/var/lib/readarr
Restart=on-failure

[Install]
WantedBy=multi-user.target
```

**Enable and start:**
```bash
sudo systemctl daemon-reload
sudo systemctl enable readarr
sudo systemctl start readarr
```

### Manual Run
```bash
# After building
cd _output/net6.0/linux-x64/Readarr
./Readarr --nobrowser
```

## 🔍 Troubleshooting

### Build Issues

**"Assets file not found" error:**
```bash
cd src
dotnet clean
dotnet restore Readarr.sln
dotnet build Readarr.sln -c Release
```

**NuGet restore timeout:**
- Check `src/NuGet.config` - Azure DevOps feeds are commented out
- Ensure internet connectivity
- Try: `dotnet restore --force`

### Runtime Issues

**Port already in use:**
```bash
./Readarr --nobrowser --data=./data --urls="http://0.0.0.0:5000"
```

**No search results:**
- Verify OpenLibrary is accessible: `curl https://openlibrary.org/search.json?q=test`
- Check application logs in the data directory
- Ensure internet connectivity

**Old Goodreads data:**
- OpenLibrary book IDs are different from Goodreads
- You may need to re-add authors/books after switching

## 🏗️ Architecture

### Dependency Injection
- Uses **DryIoc** with auto-registration
- OpenLibrary classes are automatically discovered at startup
- Old Goodreads proxy is disabled (renamed to `.bak`)

### Data Model
OpenLibrary has a different structure than Goodreads:
- **Work** = The abstract book (e.g., "Harry Potter")
- **Edition** = Specific publication (e.g., "US Hardcover 1997")
- The implementation maps both to Readarr's internal models

### Legacy Code
Original Goodreads files are preserved but disabled:
- `src/NzbDrone.Core/MetadataSource/BookInfo/LegacyBookInfoProxy.cs.bak`
- `src/NzbDrone.Core/MetadataSource/Goodreads/`
- `src/NzbDrone.Core/MetadataSource/GoodreadsSearchProxy/`

## ⚠️ Known Limitations

1. **Goodreads Book IDs**: The `SearchByGoodreadsBookId` method returns empty (no ID mapping available)
2. **Reviews**: OpenLibrary doesn't provide user reviews (focuses on bibliographic data)
3. **Cover Images**: May differ from Goodreads covers
4. **Legacy Data**: Existing Readarr databases with Goodreads IDs will need re-import

## 📄 License

GNU GPL v3 (same as original Readarr)

## 🔗 Links

- **Original Readarr**: https://github.com/Readarr/Readarr
- **OpenLibrary**: https://openlibrary.org/
- **OpenLibrary API Docs**: https://openlibrary.org/developers/api

## 🤝 Contributing

This is a metadata source replacement project. For core Readarr functionality, contribute to the [official repository](https://github.com/Readarr/Readarr).

---

**Made with ❤️ for the open book metadata community**
