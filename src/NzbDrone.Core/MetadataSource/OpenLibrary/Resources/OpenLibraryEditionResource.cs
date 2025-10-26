using System.Collections.Generic;
using Newtonsoft.Json;

namespace NzbDrone.Core.MetadataSource.OpenLibrary.Resources
{
    public class OpenLibraryEditionResource
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("authors")]
        public List<OpenLibraryAuthorKey> Authors { get; set; }

        [JsonProperty("publish_date")]
        public string PublishDate { get; set; }

        [JsonProperty("publishers")]
        public List<string> Publishers { get; set; }

        [JsonProperty("isbn_10")]
        public List<string> Isbn10 { get; set; }

        [JsonProperty("isbn_13")]
        public List<string> Isbn13 { get; set; }

        [JsonProperty("number_of_pages")]
        public int? NumberOfPages { get; set; }

        [JsonProperty("covers")]
        public List<int> Covers { get; set; }

        [JsonProperty("works")]
        public List<OpenLibraryAuthorKey> Works { get; set; }

        [JsonProperty("languages")]
        public List<OpenLibraryLanguage> Languages { get; set; }

        [JsonProperty("format")]
        public string Format { get; set; }

        [JsonProperty("physical_format")]
        public string PhysicalFormat { get; set; }
    }

    public class OpenLibraryLanguage
    {
        [JsonProperty("key")]
        public string Key { get; set; }
    }
}
