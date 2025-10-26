using System;
using System.Collections.Generic;
using System.Net;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Http;
using NzbDrone.Core.MetadataSource.OpenLibrary.Resources;

namespace NzbDrone.Core.MetadataSource.OpenLibrary
{
    public interface IOpenLibrarySearchProxy
    {
        List<OpenLibrarySearchDoc> Search(string query);
    }

    public class OpenLibrarySearchProxy : IOpenLibrarySearchProxy
    {
        private readonly ICachedHttpResponseService _cachedHttpClient;
        private readonly Logger _logger;
        private readonly IHttpRequestBuilderFactory _searchBuilder;

        public OpenLibrarySearchProxy(ICachedHttpResponseService cachedHttpClient,
            Logger logger)
        {
            _cachedHttpClient = cachedHttpClient;
            _logger = logger;

            _searchBuilder = new HttpRequestBuilder("https://openlibrary.org/search.json")
                .SetHeader("User-Agent", "Readarr/1.0 (https://readarr.com)")
                .KeepAlive()
                .CreateFactory();
        }

        public List<OpenLibrarySearchDoc> Search(string query)
        {
            try
            {
                var httpRequest = _searchBuilder.Create()
                    .AddQueryParam("q", query)
                    .AddQueryParam("limit", 20)
                    .Build();

                var response = _cachedHttpClient.Get<OpenLibrarySearchResponse>(httpRequest, true, TimeSpan.FromDays(7));

                return response.Resource?.Docs ?? new List<OpenLibrarySearchDoc>();
            }
            catch (HttpException ex)
            {
                _logger.Warn(ex);
                throw new OpenLibraryException("Search for '{0}' failed. Unable to communicate with OpenLibrary.", ex, query);
            }
            catch (WebException ex)
            {
                _logger.Warn(ex);
                throw new OpenLibraryException("Search for '{0}' failed. Unable to communicate with OpenLibrary.", ex, query);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex);
                throw new OpenLibraryException("Search for '{0}' failed. Invalid response received from OpenLibrary.", ex, query);
            }
        }
    }
}
