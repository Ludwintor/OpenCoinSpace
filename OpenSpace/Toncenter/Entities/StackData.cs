using Newtonsoft.Json;

namespace OpenSpace.Toncenter.Entities
{
    internal struct StackData
    {
        [JsonProperty("type")]
        public string Type { get; private set; }

        [JsonProperty("value")]
        public string Value { get; private set; }
    }
}
