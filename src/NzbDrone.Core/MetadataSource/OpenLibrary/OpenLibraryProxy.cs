using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Http;
using NzbDrone.Core.MetadataSource.OpenLibrary.Resources;

namespace NzbDrone.Core.MetadataSource.OpenLibrary
{
    public interface IOpenLibraryProxy
    {
        Book GetBookInfo(string foreignBookId, bool useCache = true);
        Author GetAuthorInfo(string foreignAuthorId, bool useCache = true);
        OpenLibraryWorkResource GetWorkInfo(string foreignWorkId, bool useCache = true);
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
                    throw new AuthorNotFoundException($"Author {foreignAuthorId} not found");
                }
                else
                {
                    throw new HttpException(httpRequest, httpResponse);
                }
            }

            var resource = Json.Deserialize<OpenLibraryAuthorResource>(httpResponse.Content);

            return MapAuthor(resource);
        }

        public OpenLibraryWorkResource GetWorkInfo(string foreignWorkId, bool useCache = true)
        {
            _logger.Debug("Getting Work with OpenLibrary ID of {0}", foreignWorkId);

            var httpRequest = _requestBuilder.Create()
                .SetSegment("route", $"works/{foreignWorkId}")
                .Build();

            httpRequest.AllowAutoRedirect = true;
            httpRequest.SuppressHttpError = true;

            var httpResponse = _cachedHttpClient.Get(httpRequest, useCache, TimeSpan.FromDays(90));

            if (httpResponse.HasHttpError)
            {
                if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new BookNotFoundException($"Work {foreignWorkId} not found");
                }
                else
                {
                    throw new HttpException(httpRequest, httpResponse);
                }
            }

            return Json.Deserialize<OpenLibraryWorkResource>(httpResponse.Content);
        }

        public Book GetBookInfo(string foreignBookId, bool useCache = true)
        {
            _logger.Debug("Getting Book/Work with OpenLibrary ID of {0}", foreignBookId);

            // Try as work first (most common case from search)
            try
            {
                var workResource = GetWorkInfo(foreignBookId, useCache);
                return MapWorkToBook(workResource, useCache);
            }
            catch (BookNotFoundException)
            {
                _logger.Debug("ID {0} not found as work, trying as edition", foreignBookId);
            }

            // Try as edition
            var httpRequest = _requestBuilder.Create()
                .SetSegment("route", $"books/{foreignBookId}")
                .Build();

            httpRequest.AllowAutoRedirect = true;
            httpRequest.SuppressHttpError = true;

            var httpResponse = _cachedHttpClient.Get(httpRequest, useCache, TimeSpan.FromDays(90));

            if (httpResponse.HasHttpError)
            {
                if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new BookNotFoundException($"Book/Work {foreignBookId} not found");
                }
                else
                {
                    throw new HttpException(httpRequest, httpResponse);
                }
            }

            var editionResource = Json.Deserialize<OpenLibraryEditionResource>(httpResponse.Content);
            return MapEditionToBook(editionResource, useCache);
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
                Name = resource.Name
                // Note: Author model doesn't have Overview, only AuthorMetadata does
            };

            var metadata = new AuthorMetadata
            {
                ForeignAuthorId = author.ForeignAuthorId,
                Name = author.Name,
                Overview = GetBioText(resource.Bio),
                Aliases = resource.AlternateNames ?? new List<string>(),
                Born = TryParseDate(resource.BirthDate),
                Died = TryParseDate(resource.DeathDate)
            };

            author.Metadata = metadata;

            return author;
        }

        private Book MapWorkToBook(OpenLibraryWorkResource workResource, bool useCache)
        {
            var workId = workResource.Key?.Replace("/works/", "");
            
            var book = new Book
            {
                ForeignBookId = workId,
                Title = workResource.Title,
                // Note: Book model doesn't have Overview/Description property
                ReleaseDate = TryParseDate(workResource.FirstPublishDate)
            };

            // Get author info
            if (workResource.Authors != null && workResource.Authors.Count > 0)
            {
                var firstAuthor = workResource.Authors[0];
                if (firstAuthor.Author?.Key != null)
                {
                    var authorId = firstAuthor.Author.Key.Replace("/authors/", "");
                    try
                    {
                        var author = GetAuthorInfo(authorId, useCache);
                        book.Author = new LazyLoaded<Author>(author);
                        book.AuthorMetadata = new LazyLoaded<AuthorMetadata>(author.Metadata.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn(ex, "Failed to fetch author {0} for work {1}", authorId, workId);
                    }
                }
            }

            // Create a basic edition for the work
            var edition = new Edition
            {
                ForeignEditionId = workId,
                Title = workResource.Title,
                Monitored = true
            };

            book.Editions = new List<Edition> { edition };

            return book;
        }

        private Book MapEditionToBook(OpenLibraryEditionResource editionResource, bool useCache)
        {
            var editionId = editionResource.Key?.Replace("/books/", "");
            
            var book = new Book
            {
                ForeignBookId = editionId,
                Title = editionResource.Title,
                ReleaseDate = TryParseDate(editionResource.PublishDate)
            };

            // Get work info if available
            if (editionResource.Works != null && editionResource.Works.Count > 0)
            {
                var workId = editionResource.Works[0].Key?.Replace("/works/", "");
                if (!string.IsNullOrWhiteSpace(workId))
                {
                    try
                    {
                        var workResource = GetWorkInfo(workId, useCache);
                        // Note: Book model doesn't have Overview/Description property
                        book.ForeignBookId = workId;
                        
                        // Get author from work
                        if (workResource.Authors != null && workResource.Authors.Count > 0)
                        {
                            var firstAuthor = workResource.Authors[0];
                            if (firstAuthor.Author?.Key != null)
                            {
                                var authorId = firstAuthor.Author.Key.Replace("/authors/", "");
                                try
                                {
                                    var author = GetAuthorInfo(authorId, useCache);
                                    book.Author = new LazyLoaded<Author>(author);
                                    book.AuthorMetadata = new LazyLoaded<AuthorMetadata>(author.Metadata.Value);
                                }
                                catch (Exception ex)
                                {
                                    _logger.Warn(ex, "Failed to fetch author {0}", authorId);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn(ex, "Failed to fetch work {0} for edition {1}", workId, editionId);
                    }
                }
            }

            // If no author from work, try edition authors
            if (book.Author?.Value == null && editionResource.Authors != null && editionResource.Authors.Count > 0)
            {
                var authorId = editionResource.Authors[0].Key?.Replace("/authors/", "");
                if (!string.IsNullOrWhiteSpace(authorId))
                {
                    try
                    {
                        var author = GetAuthorInfo(authorId, useCache);
                        book.Author = new LazyLoaded<Author>(author);
                        book.AuthorMetadata = new LazyLoaded<AuthorMetadata>(author.Metadata.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn(ex, "Failed to fetch author {0}", authorId);
                    }
                }
            }

            var edition = new Edition
            {
                ForeignEditionId = editionId,
                Title = editionResource.Title,
                Isbn13 = editionResource.Isbn13?.FirstOrDefault(),
                Publisher = editionResource.Publishers?.FirstOrDefault(),
                ReleaseDate = book.ReleaseDate,
                PageCount = editionResource.NumberOfPages ?? 0,
                Format = editionResource.PhysicalFormat ?? editionResource.Format,
                Monitored = true
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

            var workId = searchDoc.Key.Replace("/works/", "");
            
            var book = new Book
            {
                ForeignBookId = workId,
                Title = searchDoc.Title,
                ReleaseDate = searchDoc.FirstPublishYear.HasValue ? new DateTime(searchDoc.FirstPublishYear.Value, 1, 1) : null
            };

            // Set author if available (lightweight, don't fetch full details in search)
            if (searchDoc.AuthorName != null && searchDoc.AuthorName.Count > 0 && 
                searchDoc.AuthorKey != null && searchDoc.AuthorKey.Count > 0)
            {
                var authorId = searchDoc.AuthorKey[0].Replace("/authors/", "");
                var authorName = searchDoc.AuthorName[0];
                
                var author = new Author
                {
                    ForeignAuthorId = authorId,
                    Name = authorName
                };

                var metadata = new AuthorMetadata
                {
                    ForeignAuthorId = authorId,
                    Name = authorName
                };

                author.Metadata = new LazyLoaded<AuthorMetadata>(metadata);
                book.Author = new LazyLoaded<Author>(author);
                book.AuthorMetadata = new LazyLoaded<AuthorMetadata>(metadata);
            }

            // Create basic edition
            var edition = new Edition
            {
                ForeignEditionId = workId,
                Title = searchDoc.Title,
                Monitored = true
            };

            book.Editions = new List<Edition> { edition };

            return book;
        }

        private string GetDescriptionText(object description)
        {
            if (description == null)
            {
                return null;
            }

            if (description is string descText)
            {
                return descText;
            }

            try
            {
                var descDict = description as Dictionary<string, object>;
                if (descDict != null && descDict.ContainsKey("value"))
                {
                    return descDict["value"]?.ToString();
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            return description.ToString();
        }

        private string GetBioText(object bio)
        {
            return GetDescriptionText(bio);
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
