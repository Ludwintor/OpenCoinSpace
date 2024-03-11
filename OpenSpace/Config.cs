using Newtonsoft.Json;

namespace OpenSpace
{
    internal sealed class Config
    {
        [JsonProperty("botApi", Required = Required.Always)]
        public string BotApi { get; private set; } = null!;

        [JsonProperty("toncenterUrl", Required = Required.Always)]
        public string ToncenterUrl { get; private set; } = null!;

        [JsonProperty("toncenterApi", Required = Required.Always)]
        public string ToncenterApi { get; private set; } = null!;

        [JsonProperty("tonConnectManifestUrl", Required = Required.Always)]
        public string TonConnectManifestUrl { get; private set; } = null!;

        [JsonProperty("stakingAddress", Required = Required.Always)]
        public string StakingAddress { get; private set; } = null!;

        [JsonProperty("tokenAddress", Required = Required.Always)]
        public string TokenAddress { get; private set; } = null!;

        [JsonProperty("tokenDecimals", Required = Required.Always)]
        public int TokenDecimals { get; private set; }

        [JsonProperty("redisConnection", Required = Required.Always)]
        public string RedisConnection { get; private set; } = null!;
    }
}
