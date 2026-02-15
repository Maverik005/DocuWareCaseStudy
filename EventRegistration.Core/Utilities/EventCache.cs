using Microsoft.Extensions.Caching.Memory;

namespace EventRegistration.Core.Utilities;
public sealed class EventCache(IMemoryCache cache)
{
    private readonly IMemoryCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    private static readonly TimeSpan DefaultAbsoluteExpiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DefaultSlidingExpiration = TimeSpan.FromMinutes(2);

    public async Task<TEvent?> GetOrCreateNullableAsync<TEvent>(
        int eventId,
        Func<Task<TEvent?>> factory,
        CancellationToken cancellationToken = default) where TEvent : class
    {
        var cacheKey = GetCacheKey(eventId);

        // Try cache first
        if (_cache.TryGetValue<TEvent>(cacheKey, out var cached))
        {
            return cached;
        }

        // Call factory
        var result = await factory();

        // Only cache non-null results
        if (result != null)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = DefaultAbsoluteExpiration,
                SlidingExpiration = DefaultSlidingExpiration,
                Priority = CacheItemPriority.Normal
            };

            _cache.Set(cacheKey, result, cacheOptions);
        }

        return result;
    }

    public bool TryGetEvent<TEvent>(int eventId, out TEvent? value) where TEvent : class
    {
        var cachekey = GetCacheKey(eventId);
        return _cache.TryGetValue(cachekey, out value);
    }

    public TEvent? GetEvent<TEvent>(int eventId) where TEvent : class
    {
        var cachekey = GetCacheKey(eventId);
        return _cache.Get<TEvent>(cachekey);
    }

    public void SetEvent<TEvent>(int eventId, TEvent value) where TEvent : class
    {
        var cachekey = GetCacheKey(eventId);
        var cacheoptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = DefaultAbsoluteExpiration,
            SlidingExpiration = DefaultSlidingExpiration,
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(cachekey,value,cacheoptions);
    }

    public void RemoveEvent(int eventId)
    {
        var cacheKey = GetCacheKey(eventId);
        _cache.Remove(cacheKey);
    }

    private static string GetCacheKey(int eventId) => $"event:{eventId}";
}
