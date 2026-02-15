using Microsoft.Extensions.Caching.Memory;


namespace EventRegistration.Core.Utilities;

public sealed class RegistrationCache(IMemoryCache cache)
{
    private readonly IMemoryCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    private static readonly TimeSpan CountCacheExpiration = TimeSpan.FromMinutes(5);

    public bool TryGetRegistrationCount(int eventId, out int count)
    {
        var cacheKey = GetCountCacheKey(eventId);
        return _cache.TryGetValue(cacheKey, out count);
    }

    public void SetRegistrationCount(int eventId, int count)
    {
        var cacheKey = GetCountCacheKey(eventId);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CountCacheExpiration,
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(cacheKey, count, cacheOptions);
    }

    public void RemoveRegistrationCount(int eventId)
    {
        var cacheKey = GetCountCacheKey(eventId);
        _cache.Remove(cacheKey);
    }

    private static string GetCountCacheKey(int eventId) => $"event:{eventId}:count";
}
