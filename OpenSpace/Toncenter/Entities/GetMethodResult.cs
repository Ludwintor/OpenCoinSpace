using Newtonsoft.Json;

namespace OpenSpace.Toncenter.Entities
{
    internal struct GetMethodResult
    {
        [JsonProperty("gas_used")]
        public UInt128 GasUsed { get; private set; }

        [JsonProperty("exit_code")]
        public int ExitCode { get; private set; }

        [JsonProperty("stack")]
        public StackData[] Stack { get; private set; }
    }
}
