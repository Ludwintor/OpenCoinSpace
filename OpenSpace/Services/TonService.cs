using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenSpace.Core;
using OpenSpace.Logging;
using OpenSpace.Repositories;
using TonSdk.Connect;

namespace OpenSpace.Services
{
    internal sealed class TonService : ITonService
    {
        private readonly ILogger _logger;
        private readonly HttpClient _client;
        private readonly TonConnectOptions _connectorOptions;
        private readonly ITonConnectRepository _repository;
        private readonly SimpleCache<long, TonConnect> _cache;

        public TonService(ILogger logger, HttpClient client, ITonConnectRepository repository, Config config)
        {
            _logger = logger;
            _client = client;
            _repository = repository;
            _cache = new(TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10));
            _cache.Evicted += RecycleConnector;
            _connectorOptions = new()
            {
                ManifestUrl = config.TonConnectManifestUrl,
            };
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
    }
}
