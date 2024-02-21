using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace OpenSpace.Toncenter.Entities
{
    internal readonly struct RunMethodBody
    {
        [SetsRequiredMembers]
        public RunMethodBody(string address, string method, StackData[]? stack = null)
        {
            Address = address;
            Method = method;
            Stack = stack ?? [];
        }

        [JsonProperty("address")]
        public required string Address { get; init; }

        [JsonProperty("method")]
        public required string Method { get; init; }

        [JsonProperty("stack")]
        public StackData[] Stack { get; init; } = [];
    }
}
