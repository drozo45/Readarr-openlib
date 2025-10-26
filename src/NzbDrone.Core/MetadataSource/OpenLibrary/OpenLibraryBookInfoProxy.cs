using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Books;
using NzbDrone.Core.MetadataSource.OpenLibrary.Resources;

namespace NzbDrone.Core.MetadataSource.OpenLibrary
{
    /// <summary>
    /// OpenLibrary implementation that provides book and author metadata
    /// This replaces the Goodreads-based metadata system
    /// </summary>
    public class OpenLibraryBookInfoProxy : IProvideAuthorInfo, IProvideBookInfo, ISearchForNewBook, ISearchForNewAuthor, ISearchForNewEntity
    {
        private readonly IOpenLibraryProxy _openLibraryProxy;
        private readonly IOpenLibrarySearchProxy _searchProxy;
        private readonly Logger _logger;

        public OpenLibraryBookInfoProxy(IOpenLibraryProxy openLibraryProxy,
                                       IOpenLibrarySearchProxy searchProxy,
                                       Logger logger)
        {
            _openLibraryProxy = openLibraryProxy;
            _searchProxy = searchProxy;
            _logger = logger;
        }

        public HashSet<string> GetChangedAuthors(DateTime startTime)
        {
            // OpenLibrary doesn't have a real-time change feed like Goodreads did
            // Return null to indicate full refresh should be used
            return null;
        }

        public Author GetAuthorInfo(string foreignAuthorId, bool useCache = true)
        {
            _logger.Debug("Getting Author details for OpenLibrary ID: {0}", foreignAuthorId);
            
            try
            {
                return _openLibraryProxy.GetAuthorInfo(foreignAuthorId, useCache);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Error getting author info for {0}", foreignAuthorId);
                throw;
            }
        }

        public HashSet<string> GetChangedBooks(DateTime startTime)
        {
            // OpenLibrary doesn't have a real-time change feed
            // Return null to indicate full refresh should be used
            return null;
        }

        public Tuple<string, Book, List<AuthorMetadata>> GetBookInfo(string foreignBookId)
        {
            _logger.Debug("Getting Book details for OpenLibrary ID: {0}", foreignBookId);
            
            try
            {
                var book = _openLibraryProxy.GetBookInfo(foreignBookId);
                
                // Get author metadata if available
                var authorMetadata = new List<AuthorMetadata>();
                if (book.Author?.Value != null)
                {
                    authorMetadata.Add(book.Author.Value.Metadata.Value);
                }

                return new Tuple<string, Book, List<AuthorMetadata>>(
                    book.ForeignBookId,
                    book,
                    authorMetadata
                );
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Error getting book info for {0}", foreignBookId);
                throw;
            }
        }

        public List<object> SearchForNewEntity(string title)
        {
            var books = SearchForNewBook(title, null, false);

            var result = new List<object>();
            foreach (var book in books)
            {
                if (book.Author?.Value != null)
                {
                    var author = book.Author.Value;
                    if (!result.Contains(author))
                    {
                        result.Add(author);
                    }
                }

                result.Add(book);
            }

            return result;
        }

        public List<Author> SearchForNewAuthor(string title)
        {
            var books = SearchForNewBook(title, null);

            return books
                .Where(x => x.Author?.Value != null)
                .Select(x => x.Author.Value)
                .DistinctBy(x => x.ForeignAuthorId)
                .ToList();
        }

        public List<Book> SearchForNewBook(string title, string author, bool getAllEditions = true)
        {
            _logger.Debug("Searching OpenLibrary for book: {0} by {1}", title, author ?? "any");
            
            try
            {
                return _openLibraryProxy.SearchForNewBook(title, author, getAllEditions);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Error searching for book: {0}", title);
                return new List<Book>();
            }
        }

        public List<Book> SearchByIsbn(string isbn)
        {
            _logger.Debug("Searching OpenLibrary by ISBN: {0}", isbn);
            
            try
            {
                // Search using ISBN directly
                var searchResults = _searchProxy.Search($"isbn:{isbn}");
                var books = new List<Book>();

                foreach (var result in searchResults.Take(5))
                {
                    try
                    {
                        // Get full book details
                        if (!string.IsNullOrWhiteSpace(result.Key))
                        {
                            var bookId = result.Key.Replace("/works/", "");
                            var book = _openLibraryProxy.GetBookInfo(bookId);
                            if (book != null)
                            {
                                books.Add(book);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn(ex, "Error getting book details for ISBN result");
                    }
                }

                return books;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Error searching by ISBN: {0}", isbn);
                return new List<Book>();
            }
        }

        public List<Book> SearchByAsin(string asin)
        {
            _logger.Debug("Searching OpenLibrary by ASIN: {0}", asin);
            
            // OpenLibrary doesn't support ASIN directly
            // Try searching as a regular identifier
            return SearchByIsbn(asin);
        }

        public List<Book> SearchByGoodreadsBookId(int goodreadsId, bool getAllEditions)
        {
            _logger.Warn("SearchByGoodreadsBookId called but OpenLibrary doesn't support Goodreads IDs: {0}", goodreadsId);
            
            // OpenLibrary doesn't have Goodreads ID mapping
            // Return empty list
            return new List<Book>();
        }
    }
}
