# Readarr with OpenLibrary - Self-Hosting Setup Guide

This is a modified version of Readarr that uses **OpenLibrary** instead of Goodreads for metadata.

## Quick Start

### Prerequisites
- .NET 6.0 SDK or later
- Node.js 16+ and yarn (for frontend development only)
- Linux/macOS/Windows environment

### Build and Run

1. **Restore .NET dependencies:**
   ```bash
   cd src
   dotnet restore Readarr.sln
   ```

2. **Build the backend:**
   ```bash
   dotnet build src/Readarr.sln -c Release
   ```

3. **Run the application:**
   ```bash
   cd src/NzbDrone.Console
   dotnet run -c Release -- --nobrowser --data=/home/runner/workspace/.config
   ```

4. **Access the application:**
   - Open your browser to `http://localhost:8787` (default Readarr port)
   - Or the port shown in the console output

## What Changed: OpenLibrary Integration

### Metadata Source Replacement
This version replaces all Goodreads API calls with OpenLibrary API (https://openlibrary.org/):

- **Search books** - Uses OpenLibrary's search API
- **Get book details** - Fetches from OpenLibrary works/editions
- **Author information** - Retrieved from OpenLibrary author records
- **ISBN/ASIN lookup** - Searches OpenLibrary's ISBN database

### Key Features
- ✅ No API key required (OpenLibrary is free and open)
- ✅ No rate limits (just requires a user-agent header)
- ✅ Automatic caching (7-30 days depending on data type)
- ✅ Complete metadata (authors, titles, ISBNs, descriptions, covers)

### Implementation Files
The OpenLibrary integration is located at:
```
src/NzbDrone.Core/MetadataSource/OpenLibrary/
├── OpenLibraryBookInfoProxy.cs    # Main implementation
├── OpenLibraryProxy.cs             # Core API client
├── OpenLibrarySearchProxy.cs       # Search functionality
├── OpenLibraryException.cs         # Error handling
└── Resources/                      # API data models
    ├── OpenLibrarySearchResponse.cs
    ├── OpenLibraryWorkResource.cs
    ├── OpenLibraryAuthorResource.cs
    └── OpenLibraryEditionResource.cs
```

## Troubleshooting

### Build Issues

**Problem: "Assets file not found" error**
```bash
# Solution: Clean and restore
cd src
rm -rf obj bin _temp
dotnet clean
dotnet restore Readarr.sln
```

**Problem: Azure DevOps NuGet feed timeout**
```bash
# The NuGet.config has been updated to comment out slow feeds
# If restore still hangs, check src/NuGet.config
```

### Runtime Issues

**Problem: Port 8787 already in use**
```bash
# Run on a different port
dotnet run -- --nobrowser --data=./data --urls="http://0.0.0.0:5001"
```

**Problem: No metadata results**
- Check internet connectivity (OpenLibrary requires internet access)
- Verify the OpenLibrary API is accessible: `curl https://openlibrary.org/search.json?q=test`
- Check logs for API errors

### Frontend Development (Optional)

The frontend is optional for basic use. To build it:

```bash
# Install dependencies (may take time due to old packages)
yarn install --network-timeout 100000

# Build frontend
yarn build

# The compiled assets will be in _output/UI/
```

## API Testing

Once running, test the OpenLibrary integration:

### Search for books
```bash
curl "http://localhost:8787/api/v1/search?term=harry+potter"
```

### Lookup by ISBN
```bash
curl "http://localhost:8787/api/v1/book?asin=0439708184"
```

### Author search
```bash
curl "http://localhost:8787/api/v1/author/lookup?term=tolkien"
```

## Deployment

### Using Replit Deployments
This project is configured for Replit VM deployment:
- Build command: `dotnet build src/Readarr.sln -c Release`
- Run command: `dotnet run --project src/NzbDrone.Console/Readarr.Console.csproj -c Release --urls=http://0.0.0.0:5000`

### Self-Hosting on Your Server

1. **Build a release:**
   ```bash
   dotnet publish src/Readarr.sln -c Release -o ./publish
   ```

2. **Copy to your server:**
   ```bash
   scp -r ./publish user@yourserver:/opt/readarr/
   ```

3. **Create a systemd service (Linux):**
   ```ini
   [Unit]
   Description=Readarr with OpenLibrary
   After=network.target
   
   [Service]
   Type=simple
   User=readarr
   WorkingDirectory=/opt/readarr
   ExecStart=/usr/bin/dotnet /opt/readarr/Readarr.Console.dll --nobrowser --data=/var/lib/readarr
   Restart=on-failure
   
   [Install]
   WantedBy=multi-user.target
   ```

4. **Enable and start:**
   ```bash
   sudo systemctl enable readarr
   sudo systemctl start readarr
   ```

## Architecture

### Dependency Injection
This project uses **DryIoc** with auto-registration. The OpenLibrary classes are automatically discovered and registered at startup.

### Legacy Goodreads Code
The original Goodreads implementation files are still present but disabled:
- `src/NzbDrone.Core/MetadataSource/BookInfo/LegacyBookInfoProxy.cs.bak` (renamed to prevent loading)

### Data Flow
1. User searches for a book
2. `OpenLibrarySearchProxy` queries OpenLibrary search API
3. Results are mapped to Readarr's internal `Book` models
4. When user selects a book, `OpenLibraryProxy` fetches complete metadata
5. Related resources (authors, works) are automatically resolved and cached

## Support

### Known Limitations
- **Goodreads book IDs**: The `SearchByGoodreadsBookId` method returns empty results (OpenLibrary doesn't have Goodreads ID mapping)
- **Cover images**: OpenLibrary covers may differ from original Goodreads covers
- **Review data**: User reviews are not available (OpenLibrary focuses on bibliographic data)

### Original Readarr
- GitHub: https://github.com/Readarr/Readarr
- This is a modified version for metadata source replacement only

## License

GNU GPL v3 (same as original Readarr)
