using StackExchange.Redis;

namespace OpenSpace.Repositories
{
    internal sealed class RedisTonConnectRepository : ITonConnectRepository
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _database;

        public RedisTonConnectRepository(Config config)
        {
            _redis = ConnectionMultiplexer.Connect(config.RedisConnection);
            _database = _redis.GetDatabase();
        }

        public string? GetString(string key, string? defaultValue = null)
        {
            RedisValue value = _database.StringGet(key);
            return value.HasValue ? value : defaultValue;
        }

        public void SetString(string key, string value)
        {
            _database.StringSet(key, value);
        }

        public void DeleteKey(string key)
        {
            _database.KeyDelete(key);
        }

        public bool HasKey(string key)
        {
            return _database.KeyExists(key);
        }
    }
}
