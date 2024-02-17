using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace OpenSpace.Core
{
    public sealed class SimpleCache<TKey, TValue> where TKey : notnull
    {
        public event Action<TKey, TValue>? Evicted;

        private readonly ConcurrentDictionary<TKey, CacheEntry> _cache;
        private readonly TimeSpan _slidingExpiration;
        private readonly TimeSpan _expirationScanFrequency;
        private DateTime _lastExpirationScan;

        public SimpleCache(TimeSpan slidingExpiration, TimeSpan expirationScanFrequency)
        {
            _slidingExpiration = slidingExpiration;
            _expirationScanFrequency = expirationScanFrequency;
            _lastExpirationScan = DateTime.UtcNow;
            _cache = [];
        }

        public bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            value = default;
            if (!_cache.TryGetValue(key, out CacheEntry? entry))
                return false;
            DateTime now = DateTime.UtcNow; // TODO: Use DI version of clock
            if (entry.ExpiresAt <= now)
            {
                Evict(key);
                return false;
            }
            entry.ExpiresAt = now + _slidingExpiration;
            value = entry.Value;
            return true;
        }

        public void AddOrUpdate(TKey key, TValue value)
        {
            DateTime expiresAt = DateTime.UtcNow + _slidingExpiration;
            CacheEntry entry = new(key, value, expiresAt);
            _cache.AddOrUpdate(key, entry, (key, old) => entry);
            StartScanForExpiredItems();
        }

        public bool TryRemove(TKey key)
        {
            return _cache.TryRemove(key, out _);
        }

        private void StartScanForExpiredItems()
        {
            if (_expirationScanFrequency > DateTime.UtcNow - _lastExpirationScan)
                return;
            Task.Factory.StartNew(state => ScanForExpiredItems((SimpleCache<TKey, TValue>)state!), this,
                                  CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        private void ScanForExpiredItems(SimpleCache<TKey, TValue> cache)
        {
            DateTime now = DateTime.UtcNow;
            foreach ((TKey key, CacheEntry entry) in cache._cache)
            {
                if (entry.ExpiresAt <= now)
                    cache.Evict(key);
            }
        }

        private void Evict(TKey key)
        {
            if (!_cache.TryRemove(key, out CacheEntry? entry))
                return;
            Evicted?.Invoke(key, entry.Value);
        }

        private class CacheEntry
        {
            public TKey Key { get; }
            public TValue Value { get; }
            public DateTime ExpiresAt { get; set; }

            public CacheEntry(TKey key, TValue value, DateTime expiresAt)
            {
                Key = key;
                Value = value;
                ExpiresAt = expiresAt;
            }
        }
    }
}
