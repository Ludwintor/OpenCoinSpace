using Microsoft.Extensions.Logging;
using OpenSpace.Core;
using OpenSpace.Logging;
using OpenSpace.Repositories;
using OpenSpace.Toncenter;
using OpenSpace.Toncenter.Entities;
using TonSdk.Connect;

namespace OpenSpace.Services
{
    internal sealed class TonService : ITonService
    {
        private readonly ILogger _logger;
        private readonly ToncenterClient _client;
        private readonly TonConnectOptions _connectorOptions;
        private readonly ITonConnectRepository _repository;
        private readonly SimpleCache<long, TonConnect> _connectorCache;
        private readonly SimpleCache<(string, string), NftItem[]> _nftsCache;
        private readonly SimpleCache<(string, string), JettonWallet> _walletsCache;

        public TonService(ILogger logger, ToncenterClient client, ITonConnectRepository repository, Config config)
        {
            _logger = logger;
            _client = client;
            _repository = repository;
            _connectorCache = new(null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10));
            _connectorCache.Evicted += RecycleConnector;
            _nftsCache = new(TimeSpan.FromSeconds(5), null, TimeSpan.FromMinutes(10));
            _walletsCache = new(TimeSpan.FromSeconds(5), null, TimeSpan.FromMinutes(10));
            _connectorOptions = new()
            {
                ManifestUrl = config.TonConnectManifestUrl
            };
        }

        public async ValueTask<JettonWallet?> GetJettonWalletAsync(string owner, string masterAddress)
        {
            (string, string) key = (owner, masterAddress);
            if (_walletsCache.TryGet(key, out JettonWallet cached))
                return cached;
            JettonWallet? wallet = await _client.GetJettonWalletAsync(owner, masterAddress);
            if (wallet == null)
                return null;
            _walletsCache.AddOrUpdate(key, wallet.Value);
            return wallet;
        }

        public async ValueTask<NftItem[]> GetNftItemsAsync(string collection, string owner)
        {
            (string, string) key = (collection, owner);
            if (_nftsCache.TryGet(key, out NftItem[]? items))
                return items;
            items = await _client.GetNftItemsAsync(collection, owner).ConfigureAwait(false);
            _nftsCache.AddOrUpdate(key, items);
            return items;
        }

        public TonConnect GetUserConnector(long userId)
        {
            if (_connectorCache.TryGet(userId, out TonConnect? connector))
                return connector;
            connector = new(_connectorOptions, CreateStorage(userId));
            _connectorCache.AddOrUpdate(userId, connector);
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
            _logger.LogTrace(LogEvents.TonConnect, "User {Id} connector paused", userId.ToString());
            connector.PauseConnection();
        }
    }
}
