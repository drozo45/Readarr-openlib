using System.Collections.Generic;
using Newtonsoft.Json;

namespace NzbDrone.Core.MetadataSource.OpenLibrary.Resources
{
    public class OpenLibraryWorkResource
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("authors")]
        public List<OpenLibraryAuthorReference> Authors { get; set; }

        [JsonProperty("description")]
        public object Description { get; set; }

        [JsonProperty("covers")]
        public List<int> Covers { get; set; }

        [JsonProperty("first_publish_date")]
        public string FirstPublishDate { get; set; }

        [JsonProperty("subjects")]
        public List<string> Subjects { get; set; }
    }

    public class OpenLibraryAuthorReference
    {
        [JsonProperty("author")]
        public OpenLibraryAuthorKey Author { get; set; }

        [JsonProperty("type")]
        public OpenLibraryTypeReference Type { get; set; }
    }

    public class OpenLibraryAuthorKey
    {
        [JsonProperty("key")]
        public string Key { get; set; }
    }

    public class OpenLibraryTypeReference
    {
        [JsonProperty("key")]
        public string Key { get; set; }
    }
}
