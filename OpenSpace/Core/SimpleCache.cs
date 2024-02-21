using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace OpenSpace.Core
{
    public sealed class SimpleCache<TKey, TValue> where TKey : notnull
    {
        public event Action<TKey, TValue>? Evicted;

        private readonly ConcurrentDictionary<TKey, CacheEntry> _cache;
        private readonly TimeSpan? _absoluteExpiration;
        private readonly TimeSpan _slidingExpiration;
        private readonly TimeSpan _expirationScanFrequency;
        private DateTime _lastExpirationScan;

        public SimpleCache(TimeSpan? absoluteExpiration, TimeSpan slidingExpiration, TimeSpan expirationScanFrequency)
        {
            _absoluteExpiration = absoluteExpiration;
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
            if (entry.SlidingExpiresAt <= now || entry.AbsoluteExpiresAt <= now)
            {
                Evict(key);
                return false;
            }
            entry.SlidingExpiresAt = now + _slidingExpiration;
            value = entry.Value;
            return true;
        }

        public void AddOrUpdate(TKey key, TValue value)
        {
            DateTime now = DateTime.UtcNow;
            DateTime absoluteExpiration = _absoluteExpiration.HasValue ? now + _absoluteExpiration.Value : DateTime.MaxValue;
            DateTime slidingExpiration = now + _slidingExpiration;
            CacheEntry entry = new(value, absoluteExpiration, slidingExpiration);
            _cache.AddOrUpdate(key, entry, (_, _) => entry);
            StartScanForExpiredItems();
        }

        public bool TryRemove(TKey key)
        {
            return _cache.TryRemove(key, out _);
        }

        public void Clear()
        {
            _cache.Clear();
        }

        private void StartScanForExpiredItems()
        {
            DateTime now = DateTime.UtcNow;
            if (_expirationScanFrequency > now - _lastExpirationScan)
                return;
            _lastExpirationScan = now;
            Task.Factory.StartNew(state => SimpleCache<TKey, TValue>.ScanForExpiredItems((SimpleCache<TKey, TValue>)state!), this,
                                  CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        private static void ScanForExpiredItems(SimpleCache<TKey, TValue> cache)
        {
            DateTime now = DateTime.UtcNow;
            foreach ((TKey key, CacheEntry entry) in cache._cache)
            {
                if (entry.SlidingExpiresAt <= now || entry.AbsoluteExpiresAt <= now)
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
            public TValue Value { get; }
            public DateTime AbsoluteExpiresAt { get; }
            public DateTime SlidingExpiresAt { get; set; }

            public CacheEntry(TValue value, DateTime absoluteExpiresAt, DateTime slidingExpiresAt)
            {
                Value = value;
                AbsoluteExpiresAt = absoluteExpiresAt;
                SlidingExpiresAt = slidingExpiresAt;
            }
        }
    }
}
