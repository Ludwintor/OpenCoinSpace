using Newtonsoft.Json;
using TonSdk.Core;

namespace OpenSpace.Toncenter.Entities
{
    internal struct Collection
    {
        [JsonProperty("address")]
        public string Address { get; private set; }

        [JsonProperty("owner_address")]
        public string OwnerAddress { get; private set; }

        [JsonProperty("last_transaction_lt")]
        public UInt128 LastLt { get; private set; }

        [JsonProperty("next_item_index")]
        public ulong NextItemIndex { get; private set; }

        [JsonProperty("collection_content")]
        public Content Content { get; private set; }

        [JsonProperty("code_hash")]
        public string CodeHash { get; private set; }

        [JsonProperty("data_hash")]
        public string DataHash { get; private set; }
    }
}
