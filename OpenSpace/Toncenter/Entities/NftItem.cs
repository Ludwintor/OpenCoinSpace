using Newtonsoft.Json;

namespace OpenSpace.Toncenter.Entities
{
    // cuz toncenter doesn't directly return json array but object that contains array
    internal struct NftItems
    {
        [JsonProperty("nft_items")]
        public NftItem[]? Items { get; private set; }
    }

    internal struct NftItem
    {
        [JsonProperty("address")]
        public string Address { get; private set; }

        [JsonProperty("collection_address")]
        public string CollectionAddress { get; private set; }

        [JsonProperty("owner_address")]
        public string OwnerAddress { get; private set; }

        [JsonProperty("init")]
        public bool Inited { get; private set; }

        [JsonProperty("index")]
        public ulong Index { get; private set; }

        [JsonProperty("last_transaction_lt")]
        public UInt128 LastLt { get; private set; }

        [JsonProperty("code_hash")]
        public string CodeHash { get; private set; }

        [JsonProperty("data_hash")]
        public string DataHash { get; private set; }

        [JsonProperty("content")]
        public Content Content { get; private set; }

        [JsonProperty("collection")]
        public Collection Collection { get; private set; }
    }
}
