using Newtonsoft.Json;

namespace OpenSpace.Toncenter.Entities
{
    internal struct Content
    {
        [JsonProperty("uri")]
        public string Url { get; private set; }
    }
}
