using System.Globalization;
using Microsoft.Extensions.Logging;
using OpenSpace.Core;
using OpenSpace.Entities;
using OpenSpace.Logging;
using OpenSpace.Repositories;
using OpenSpace.Toncenter;
using OpenSpace.Toncenter.Entities;
using TonSdk.Connect;

namespace OpenSpace.Services
{
    internal sealed class TonService : ITonService
    {
        private const string GET_STAKING_INFO = "get_staking_info";

        private readonly ILogger _logger;
        private readonly ToncenterClient _client;
        private readonly TonConnectOptions _connectorOptions;
        private readonly ITonConnectRepository _repository;
        private readonly SimpleCache<long, TonConnect> _cache;

        public TonService(ILogger logger, ToncenterClient client, ITonConnectRepository repository, Config config)
        {
            _logger = logger;
            _client = client;
            _repository = repository;
            _cache = new(null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10));
            _cache.Evicted += RecycleConnector;
            _connectorOptions = new()
            {
                ManifestUrl = config.TonConnectManifestUrl,
            };
        }

        public async Task<StakingInfo> GetStakingInfoAsync(string address)
        {
            GetMethodResult result = await _client.RunGetMethodAsync(address, GET_STAKING_INFO);
            StackData[] stack = result.Stack;
            // TODO: Stack reader!!!
            return new()
            {
                RewardSupply = UInt128.Parse(StripHexPrefix((string)stack[0].Value!), NumberStyles.HexNumber),
                Balance = UInt128.Parse(StripHexPrefix((string)stack[1].Value!), NumberStyles.HexNumber),
                BaseRewardSupply = UInt128.Parse(StripHexPrefix((string)stack[2].Value!), NumberStyles.HexNumber),
                MinStake = UInt128.Parse(StripHexPrefix((string)stack[3].Value!), NumberStyles.HexNumber),
                MaxPercent = ulong.Parse(StripHexPrefix((string)stack[4].Value!), NumberStyles.HexNumber),
                MinPercent = ulong.Parse(StripHexPrefix((string)stack[5].Value!), NumberStyles.HexNumber),
                MinLockup = ulong.Parse(StripHexPrefix((string)stack[6].Value!), NumberStyles.HexNumber),
            };
        }

        public Task<JettonWallet?> GetJettonWalletAsync(string owner, string masterAddress)
        {
            // TODO: Add cache?
            return _client.GetJettonWalletAsync(owner, masterAddress);
        }

        public TonConnect GetUserConnector(long userId)
        {
            if (_cache.TryGet(userId, out TonConnect? connector))
                return connector;
            connector = new(_connectorOptions, CreateStorage(userId));
            _cache.AddOrUpdate(userId, connector);
            return connector;
        }

        private RemoteStorage CreateStorage(long userId)
        {
            string id = userId.ToString();
            return new((key, defaultValue) => _repository.GetString(GetKey(key, id), defaultValue),
                       (key, value) => _repository.SetString(GetKey(key, id), value),
                       key => _repository.DeleteKey(GetKey(key, id)),
                       key => _repository.HasKey(GetKey(key, id)));

            static string GetKey(string key, string id)
            {
                return key + id;
            }
        }

        private void RecycleConnector(long userId, TonConnect connector)
        {
            _logger.LogInformation(LogEvents.TonConnect, "User {Id} connector paused", userId.ToString());
            connector.PauseConnection();
        }

        private static ReadOnlySpan<char> StripHexPrefix(ReadOnlySpan<char> prefixedHex)
        {
            if (prefixedHex.StartsWith("0x"))
                return prefixedHex[2..];
            return prefixedHex;
        }
    }
}
