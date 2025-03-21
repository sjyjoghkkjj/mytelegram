using System.Collections.Concurrent;

namespace MyTelegram.Services.Services;

public class CacheHelper<TKey, TValue> : ICacheHelper<TKey, TValue> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, TValue> _caches = new();
    public bool TryAdd(TKey key, TValue value)
    {
        return _caches.TryAdd(key, value);
    }

    public bool TryGetValue(TKey key, out TValue? value)
    {
        return _caches.TryGetValue(key, out value);
    }

    public bool TryRemove(TKey key, out TValue? value)
    {
        return _caches.TryRemove(key, out value);
    }
}