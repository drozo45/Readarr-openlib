using System.Collections.Generic;
using Newtonsoft.Json;

namespace NzbDrone.Core.MetadataSource.OpenLibrary.Resources
{
    public class OpenLibraryAuthorResource
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("birth_date")]
        public string BirthDate { get; set; }

        [JsonProperty("death_date")]
        public string DeathDate { get; set; }

        [JsonProperty("bio")]
        public object Bio { get; set; }

        [JsonProperty("alternate_names")]
        public List<string> AlternateNames { get; set; }

        [JsonProperty("links")]
        public List<OpenLibraryLink> Links { get; set; }

        [JsonProperty("photos")]
        public List<int> Photos { get; set; }
    }

    public class OpenLibraryLink
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("type")]
        public OpenLibraryTypeReference Type { get; set; }
    }
}
