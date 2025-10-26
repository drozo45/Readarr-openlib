using System.Collections.Generic;
using Newtonsoft.Json;

namespace NzbDrone.Core.MetadataSource.OpenLibrary.Resources
{
    public class OpenLibrarySearchResponse
    {
        [JsonProperty("numFound")]
        public int NumFound { get; set; }

        [JsonProperty("start")]
        public int Start { get; set; }

        [JsonProperty("docs")]
        public List<OpenLibrarySearchDoc> Docs { get; set; }
    }

    public class OpenLibrarySearchDoc
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("author_name")]
        public List<string> AuthorName { get; set; }

        [JsonProperty("author_key")]
        public List<string> AuthorKey { get; set; }

        [JsonProperty("first_publish_year")]
        public int? FirstPublishYear { get; set; }

        [JsonProperty("isbn")]
        public List<string> Isbn { get; set; }

        [JsonProperty("edition_count")]
        public int? EditionCount { get; set; }

        [JsonProperty("cover_i")]
        public int? CoverId { get; set; }

        [JsonProperty("publisher")]
        public List<string> Publisher { get; set; }

        [JsonProperty("language")]
        public List<string> Language { get; set; }

        [JsonProperty("publish_year")]
        public List<int> PublishYear { get; set; }
    }
}
