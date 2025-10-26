using System;
using System.Collections.Generic;
using System.Net;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Books;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Http;
using NzbDrone.Core.MetadataSource.OpenLibrary.Resources;

namespace NzbDrone.Core.MetadataSource.OpenLibrary
{
    public interface IOpenLibraryProxy
    {
        Book GetBookInfo(string foreignEditionId, bool useCache = true);
        Author GetAuthorInfo(string foreignAuthorId, bool useCache = true);
        List<Book> SearchForNewBook(string title, string author, bool getAllEditions = true);
    }

    public class OpenLibraryProxy : IOpenLibraryProxy
    {
        private readonly ICachedHttpResponseService _cachedHttpClient;
        private readonly Logger _logger;
        private readonly IHttpRequestBuilderFactory _requestBuilder;
        private readonly IOpenLibrarySearchProxy _searchProxy;

        public OpenLibraryProxy(ICachedHttpResponseService cachedHttpClient,
                                IOpenLibrarySearchProxy searchProxy,
                                Logger logger)
        {
            _cachedHttpClient = cachedHttpClient;
            _searchProxy = searchProxy;
            _logger = logger;

            _requestBuilder = new HttpRequestBuilder("https://openlibrary.org/{route}.json")
                .SetHeader("User-Agent", "Readarr/1.0 (https://readarr.com)")
                .KeepAlive()
                .CreateFactory();
        }

        public Author GetAuthorInfo(string foreignAuthorId, bool useCache = true)
        {
            _logger.Debug("Getting Author with OpenLibrary ID of {0}", foreignAuthorId);

            var httpRequest = _requestBuilder.Create()
                .SetSegment("route", $"authors/{foreignAuthorId}")
                .Build();

            httpRequest.AllowAutoRedirect = true;
            httpRequest.SuppressHttpError = true;

            var httpResponse = _cachedHttpClient.Get(httpRequest, useCache, TimeSpan.FromDays(30));

            if (httpResponse.HasHttpError)
            {
                if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new NotFoundException($"Author {foreignAuthorId} not found");
                }
                else
                {
                    throw new HttpException(httpRequest, httpResponse);
                }
            }

            var resource = httpResponse.Deserialize<OpenLibraryAuthorResource>();

            return MapAuthor(resource);
        }

        public Book GetBookInfo(string foreignEditionId, bool useCache = true)
        {
            _logger.Debug("Getting Book with OpenLibrary Edition ID of {0}", foreignEditionId);

            var httpRequest = _requestBuilder.Create()
                .SetSegment("route", $"books/{foreignEditionId}")
                .Build();

            httpRequest.AllowAutoRedirect = true;
            httpRequest.SuppressHttpError = true;

            var httpResponse = _cachedHttpClient.Get(httpRequest, useCache, TimeSpan.FromDays(90));

            if (httpResponse.HasHttpError)
            {
                if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new NotFoundException($"Book edition {foreignEditionId} not found");
                }
                else
                {
                    throw new HttpException(httpRequest, httpResponse);
                }
            }

            var resource = httpResponse.Deserialize<OpenLibraryEditionResource>();

            return MapBook(resource);
        }

        public List<Book> SearchForNewBook(string title, string author, bool getAllEditions = true)
        {
            var query = title.Trim();
            if (!string.IsNullOrWhiteSpace(author))
            {
                query += $" {author.Trim()}";
            }

            _logger.Debug("Searching OpenLibrary for: {0}", query);

            var searchResults = _searchProxy.Search(query);
            var books = new List<Book>();

            foreach (var result in searchResults)
            {
                try
                {
                    var book = MapSearchResultToBook(result);
                    if (book != null)
                    {
                        books.Add(book);
                    }

                    if (books.Count >= 20)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Error mapping search result for: {0}", result.Title);
                }
            }

            return books;
        }

        private Author MapAuthor(OpenLibraryAuthorResource resource)
        {
            var author = new Author
            {
                ForeignAuthorId = resource.Key?.Replace("/authors/", ""),
                Name = resource.Name,
                Overview = GetBioText(resource.Bio)
            };

            // Set author metadata
            var metadata = new AuthorMetadata
            {
                ForeignAuthorId = author.ForeignAuthorId,
                Name = author.Name,
                Overview = author.Overview,
                Aliases = resource.AlternateNames ?? new List<string>(),
                Born = TryParseDate(resource.BirthDate),
                Died = TryParseDate(resource.DeathDate)
            };

            author.Metadata = metadata;

            return author;
        }

        private Book MapBook(OpenLibraryEditionResource resource)
        {
            var book = new Book
            {
                ForeignBookId = resource.Key?.Replace("/books/", ""),
                Title = resource.Title,
                ReleaseDate = TryParseDate(resource.PublishDate)
            };

            // Create edition
            var edition = new Edition
            {
                ForeignEditionId = book.ForeignBookId,
                Title = resource.Title,
                Isbn13 = resource.Isbn13?.FirstOrDefault(),
                Publisher = resource.Publishers?.FirstOrDefault(),
                ReleaseDate = book.ReleaseDate,
                PageCount = resource.NumberOfPages ?? 0,
                Format = resource.PhysicalFormat ?? resource.Format
            };

            book.Editions = new List<Edition> { edition };

            return book;
        }

        private Book MapSearchResultToBook(OpenLibrarySearchDoc searchDoc)
        {
            if (string.IsNullOrWhiteSpace(searchDoc.Key))
            {
                return null;
            }

            var book = new Book
            {
                ForeignBookId = searchDoc.Key.Replace("/works/", ""),
                Title = searchDoc.Title,
                ReleaseDate = searchDoc.FirstPublishYear.HasValue ? new DateTime(searchDoc.FirstPublishYear.Value, 1, 1) : null
            };

            // Set author if available
            if (searchDoc.AuthorName != null && searchDoc.AuthorName.Count > 0 && 
                searchDoc.AuthorKey != null && searchDoc.AuthorKey.Count > 0)
            {
                var author = new Author
                {
                    ForeignAuthorId = searchDoc.AuthorKey[0].Replace("/authors/", ""),
                    Name = searchDoc.AuthorName[0]
                };

                book.Author = new LazyLoaded<Author>(author);
                book.AuthorMetadata = new LazyLoaded<AuthorMetadata>(new AuthorMetadata
                {
                    ForeignAuthorId = author.ForeignAuthorId,
                    Name = author.Name
                });
            }

            return book;
        }

        private string GetBioText(object bio)
        {
            if (bio == null)
            {
                return null;
            }

            // Bio can be a string or an object with a "value" property
            if (bio is string bioText)
            {
                return bioText;
            }

            try
            {
                var bioDict = bio as Dictionary<string, object>;
                if (bioDict != null && bioDict.ContainsKey("value"))
                {
                    return bioDict["value"]?.ToString();
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            return bio.ToString();
        }

        private DateTime? TryParseDate(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
            {
                return null;
            }

            if (DateTime.TryParse(dateString, out var date))
            {
                return date;
            }

            // Try to extract year only
            if (int.TryParse(dateString.Split(' ')[0], out var year) && year > 0 && year <= DateTime.Now.Year + 10)
            {
                return new DateTime(year, 1, 1);
            }

            return null;
        }
    }
}
