using Newtonsoft.Json;

namespace OpenSpace.Toncenter.Entities
{
    // cuz toncenter doesn't directly return json array but object that contains array
    internal struct JettonWallets
    {
        [JsonProperty("jetton_wallets")]
        public JettonWallet[] Wallets { get; private set; }
    }

    internal struct JettonWallet
    {
        [JsonProperty("address")]
        public string Address { get; private set; }

        [JsonProperty("balance")]
        public UInt128 Balance { get; private set; }

        [JsonProperty("owner")]
        public string OwnerAddress { get; private set; }

        [JsonProperty("jetton")]
        public string MasterAddress { get; private set; }

        [JsonProperty("last_transaction_lt")]
        public UInt128 LastLt { get; private set; }

        [JsonProperty("code_hash")]
        public string CodeHash { get; private set; }

        [JsonProperty("data_hash")]
        public string DataHash { get; private set; }
    }
}
