using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using OpenSpace.Core;
using OpenSpace.Repositories;
using TonSdk.Connect;

namespace OpenSpace.Services
{
    internal sealed class TonService : ITonService
    {
        private static readonly EventId _tonId = new(102, "TonConnect");

        private readonly ILogger _logger;
        private readonly HttpClient _client;
        private readonly TonConnectOptions _connectorOptions;
        private readonly ITonConnectRepository _repository;
        private readonly SimpleCache<long, TonConnect> _cache;

        public TonService(ILogger logger, HttpClient client, ITonConnectRepository repository)
        {
            _logger = logger;
            _client = client;
            _repository = repository;
            _cache = new(TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10));
            _cache.Evicted += RecycleConnector;
            _connectorOptions = new()
            {
                ManifestUrl = "https://raw.githubusercontent.com/Ludwintor/OpenCoinSpace/main/static/connect-manifest.json",
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
            connector.PauseConnection();
        }
    }
}
