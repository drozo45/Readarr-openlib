# Readarr - OpenLibrary Integration

## Project Overview
Readarr is an ebook and audiobook collection manager for Usenet and BitTorrent users. This project has been modified to use **OpenLibrary** (https://openlibrary.org/) as its primary metadata source instead of Goodreads.

## Technology Stack
- **Backend**: C# / .NET 6.0
  - ASP.NET Core web framework
  - DryIoc for dependency injection
  - Multiple projects (Host, Core, API, etc.)
- **Frontend**: React 17 + TypeScript
  - Webpack 5 for bundling
  - Redux for state management
  - SignalR for real-time updates

## Recent Changes: OpenLibrary Integration

### What Was Changed
The metadata source has been completely replaced from Goodreads to OpenLibrary API. The following files were created:

#### New OpenLibrary Metadata Source (`src/NzbDrone.Core/MetadataSource/OpenLibrary/`)
1. **OpenLibraryException.cs** - Custom exception handling
2. **OpenLibrarySearchProxy.cs** - Implements search functionality using OpenLibrary Search API
3. **OpenLibraryProxy.cs** - Main proxy for fetching book/author details from OpenLibrary
4. **OpenLibraryBookInfoProxy.cs** - Complete implementation that replaces BookInfoProxy
5. **Resources/** - Data models for OpenLibrary API responses:
   - `OpenLibrarySearchResponse.cs` - Search results
   - `OpenLibraryWorkResource.cs` - Book work details
   - `OpenLibraryAuthorResource.cs` - Author details
   - `OpenLibraryEditionResource.cs` - Book edition details

### How It Works
The OpenLibrary integration implements the same interfaces as the previous Goodreads implementation:
- `ISearchForNewBook` - Search for books by title/author, ISBN, ASIN
- `IProvideAuthorInfo` - Get author metadata
- `IProvideBookInfo` - Get book metadata
- `ISearchForNewAuthor` - Search for authors
- `ISearchForNewEntity` - General entity search

#### Implementation Details
1. **OpenLibrarySearchProxy** - Handles search queries using OpenLibrary's Search API
2. **OpenLibraryProxy** - Core proxy that:
   - Fetches book/work/edition data
   - Resolves linked resources (authors, works)
   - Handles both work IDs (`/works/OL123W`) and edition IDs (`/books/OL123M`)
   - Automatically fetches and populates related author metadata
3. **OpenLibraryBookInfoProxy** - Main entry point that implements all metadata interfaces

#### Work vs Edition Handling
OpenLibrary has a hierarchical structure: Work → Edition
- **Works** represent the abstract book (e.g., "Harry Potter and the Philosopher's Stone")
- **Editions** represent specific publications (e.g., UK hardcover 1997)

The implementation:
- Search results return work IDs (most common case)
- `GetBookInfo()` tries work lookup first, falls back to edition
- When fetching an edition, it also fetches the linked work for complete metadata
- Authors are always fetched from the work level

### OpenLibrary API Endpoints Used
- **Search**: `https://openlibrary.org/search.json?q={query}&limit=20`
- **Author Info**: `https://openlibrary.org/authors/{id}.json`
- **Book/Edition Info**: `https://openlibrary.org/books/{id}.json`
- **Work Info**: `https://openlibrary.org/works/{id}.json`

### Dependency Injection
The project uses DryIoc with auto-registration. The new OpenLibrary classes will be automatically discovered and registered when the assemblies are scanned. The `OpenLibraryBookInfoProxy` implements all the necessary interfaces and should replace the Goodreads-based implementation.

## Known Issues & Build Status

### ⚠️ Build/Dependency Issues
The project currently has build and dependency installation issues in the Replit environment:

1. **Frontend Dependencies (Node.js)**
   - `yarn install` hangs with ENOTEMPTY cache errors
   - `npm install --legacy-peer-deps` also times out
   - **Status**: Unable to install frontend dependencies

2. **Backend Dependencies (.NET)**
   - `dotnet restore` was initially hanging due to Azure DevOps package feeds
   - NuGet.config was updated to comment out slow/unavailable feeds
   - **Status**: Build may still have issues, needs testing

3. **Workflow Status**
   - Backend workflow configured but fails to start (port 5000 not opening)
   - Frontend dev server not configured (dependencies not installed)
   - **Status**: No working workflows currently

### Why These Issues Exist
- This is a retired/archived project with outdated dependencies
- Azure DevOps package feeds (pkgs.dev.azure.com) used by the original project may be inaccessible
- Node modules have peer dependency conflicts
- .NET SDK version mismatch (project targets .NET 6.0, Replit has .NET 7.0 - should be backward compatible)

## Next Steps to Get Running

### For Backend
1. Verify NuGet package restore works with updated config
2. Build the solution: `dotnet build src/Readarr.sln`
3. Run the backend: `cd src/NzbDrone.Console && dotnet run --urls="http://0.0.0.0:5000"`

### For Frontend
1. Resolve yarn/npm installation issues (may need cache cleanup or fresh install)
2. Install dependencies: `yarn install` or `npm install --legacy-peer-deps`
3. Build frontend: `yarn build` 
4. Run dev server: `yarn dev` (runs webpack-dev-server on port 5000)

### Testing OpenLibrary Integration
Once the backend builds and runs:
1. Test search API: `/api/v1/search?term=harry+potter`
2. Test author lookup: `/api/v1/author/lookup?term=tolkien`
3. Test book lookup: `/api/v1/book/lookup?term=lord+of+the+rings`

## Original Goodreads Implementation
The original Goodreads-based metadata source files are still present but disabled:
- `src/NzbDrone.Core/MetadataSource/Goodreads/` - Goodreads API proxy
- `src/NzbDrone.Core/MetadataSource/GoodreadsSearchProxy/` - Goodreads search
- `src/NzbDrone.Core/MetadataSource/BookInfo/LegacyBookInfoProxy.cs.bak` - Original wrapper (renamed to prevent DI registration)

The `BookInfoProxy` was renamed to `LegacyBookInfoProxy.cs.bak` to exclude it from DryIoc's auto-registration, ensuring only the OpenLibrary implementation is used.

### Why Goodreads Was Replaced
- Goodreads API was retired/deprecated
- OpenLibrary provides a free, open alternative with good coverage
- OpenLibrary has no API key requirements or rate limits (just user-agent)

## Architecture Notes
- The project uses a clean architecture with separation between API, Core business logic, and HTTP layers
- Metadata sources are abstracted behind interfaces, making it easy to swap implementations
- Dependency injection is handled via DryIoc with assembly scanning
- The frontend communicates with the backend via REST API and SignalR for real-time updates

## Development
- **Main entry point**: `src/NzbDrone.Console/ConsoleApp.cs`
- **API Controllers**: `src/Readarr.Api.V1/`
- **Core logic**: `src/NzbDrone.Core/`
- **Frontend**: `frontend/src/`

## Resources
- OpenLibrary API Docs: https://openlibrary.org/developers/api
- Original Readarr: https://github.com/Readarr/Readarr
- License: GNU GPL v3
