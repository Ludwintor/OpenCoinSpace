using System.Collections.Concurrent;

namespace OpenSpace.Repositories
{
    internal sealed class InMemoryTonConnectRepository : ITonConnectRepository
    {
        private readonly ConcurrentDictionary<string, string?> _dictionary;

        public InMemoryTonConnectRepository()
        {
            _dictionary = new();
        }

        public string? GetString(string key, string? defaultValue = null)
        {
            return _dictionary.GetValueOrDefault(key, defaultValue);
        }

        public void SetString(string key, string value)
        {
            _dictionary.AddOrUpdate(key, value, (key, oldValue) => value);
        }

        public void DeleteKey(string key)
        {
            _dictionary.TryRemove(key, out _);
        }

        public bool HasKey(string key)
        {
            return _dictionary.ContainsKey(key);
        }
    }
}
