using MyTelegram.Schema;
using MyTelegram.Schema.Payments;

namespace MyTelegram.Messenger.Services;

public interface ISavedStarGiftsService : ITransientDependency
{
    Task<ISavedStarGifts> GetSavedAsync(long userId, CancellationToken cancellationToken = default);
    Task<bool> SaveAsync(long userId, IInputSavedStarGift stargift, bool unsave, CancellationToken cancellationToken = default);
    Task<IStarGiftCollections> GetCollectionsAsync(long userId, CancellationToken cancellationToken = default);
    Task<IStarGiftCollection> UpdateCollectionAsync(long userId,
        string slug,
        string title,
        TVector<IInputSavedStarGift>? delete,
        TVector<IInputSavedStarGift>? add,
        TVector<IInputSavedStarGift>? order,
        CancellationToken cancellationToken = default);

    Task<bool> TogglePinnedAsync(long userId, TVector<IInputSavedStarGift> gifts);

    Task<ISavedStarGifts> GetSavedByKeysAsync(long userId, TVector<IInputSavedStarGift> gifts);
    Task<IStarGiftWithdrawalUrl> GetWithdrawalUrlAsync(long userId, IInputSavedStarGift gift);

    Task<bool> ConvertAsync(long userId, IInputSavedStarGift gift);
    Task<long> GetStarsBalanceAsync(long userId);

    Task<IStarGiftCollection> CreateCollectionAsync(long userId, string title, TVector<IInputSavedStarGift> gifts);
    Task<bool> DeleteCollectionAsync(long userId, string slug);
    Task<bool> ReorderCollectionsAsync(long userId, TVector<string> order);
}

public class SavedStarGiftsService : ISavedStarGiftsService
{
    private readonly ConcurrentDictionary<long, UserSaved> _storage = new();

    public Task<ISavedStarGifts> GetSavedAsync(long userId, CancellationToken cancellationToken = default)
    {
        var state = _storage.GetOrAdd(userId, _ => new UserSaved());
        return Task.FromResult<ISavedStarGifts>(new TSavedStarGifts
        {
            Users = new TVector<IUser>(),
            Chats = new TVector<IChat>(),
            Gifts = new TVector<ISavedStarGift>(state.Gifts.Values)
        });
    }

    public Task<bool> SaveAsync(long userId, IInputSavedStarGift stargift, bool unsave, CancellationToken cancellationToken = default)
    {
        var state = _storage.GetOrAdd(userId, _ => new UserSaved());
        var key = GiftKey(stargift);
        if (unsave)
        {
            state.Gifts.TryRemove(key, out _);
            return Task.FromResult(true);
        }

        // Create placeholder saved gift entry if needed (a real impl would validate ownership)
        var saved = new TSavedStarGift
        {
            Gift = new TStarGift
            {
                Id = key,
                Title = "Saved Gift",
                Stars = 0,
                ConvertStars = 0,
                Sticker = new TDocument { Id = key * 10 + 2, AccessHash = 0, FileReference = ReadOnlyMemory<byte>.Empty, Date = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(), MimeType = "application/x-tgsticker", Size = 0, DcId = 1, Attributes = new TVector<IDocumentAttribute>(Array.Empty<IDocumentAttribute>()) }
            },
            Date = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Peer = null,
            Stars = 0
        };

        state.Gifts[key] = saved;
        return Task.FromResult(true);
    }

    public Task<IStarGiftCollections> GetCollectionsAsync(long userId, CancellationToken cancellationToken = default)
    {
        var state = _storage.GetOrAdd(userId, _ => new UserSaved());
        return Task.FromResult<IStarGiftCollections>(new TStarGiftCollections
        {
            Collections = new TVector<IStarGiftCollection>(state.Collections.Values)
        });
    }

    public Task<IStarGiftCollection> UpdateCollectionAsync(long userId, string slug, string title, TVector<IInputSavedStarGift>? delete, TVector<IInputSavedStarGift>? add, TVector<IInputSavedStarGift>? order, CancellationToken cancellationToken = default)
    {
        var state = _storage.GetOrAdd(userId, _ => new UserSaved());
        var collection = state.Collections.GetOrAdd(slug, _ => new TStarGiftCollection { Slug = slug, Title = title, Gifts = new TVector<ISavedStarGift>() });
        collection.Title = title;

        if (delete != null)
        {
            var toDelete = delete.Select(GiftKey).ToHashSet();
            collection.Gifts = new TVector<ISavedStarGift>(collection.Gifts.Where(g => !toDelete.Contains(GiftKeyFromSaved(g))));
        }
        if (add != null)
        {
            foreach (var a in add)
            {
                var key = GiftKey(a);
                if (state.Gifts.TryGetValue(key, out var saved))
                {
                    collection.Gifts.Add(saved);
                }
            }
        }
        if (order != null && order.Count > 0)
        {
            var orderMap = order.Select((g, i) => new { Key = GiftKey(g), Index = i }).ToDictionary(x => x.Key, x => x.Index);
            collection.Gifts = new TVector<ISavedStarGift>(collection.Gifts.OrderBy(g => orderMap.GetValueOrDefault(GiftKeyFromSaved(g), int.MaxValue)));
        }

        return Task.FromResult<IStarGiftCollection>(collection);
    }

    public Task<IStarGiftCollection> CreateCollectionAsync(long userId, string title, TVector<IInputSavedStarGift> gifts)
    {
        var state = _storage.GetOrAdd(userId, _ => new UserSaved());
        var slug = Slugify(title);
        if (state.Collections.ContainsKey(slug))
        {
            RpcErrors.RpcErrors400.BadRequest("COLLECTION_EXISTS").ThrowRpcError();
        }
        var coll = new TStarGiftCollection { Slug = slug, Title = title, Gifts = new TVector<ISavedStarGift>() };
        foreach (var g in gifts)
        {
            var key = GiftKey(g);
            if (state.Gifts.TryGetValue(key, out var saved))
            {
                coll.Gifts.Add(saved);
            }
        }
        state.Collections[slug] = coll;
        return Task.FromResult<IStarGiftCollection>(coll);
    }

    public Task<bool> DeleteCollectionAsync(long userId, string slug)
    {
        var state = _storage.GetOrAdd(userId, _ => new UserSaved());
        return Task.FromResult(state.Collections.TryRemove(slug, out _));
    }

    public Task<bool> ReorderCollectionsAsync(long userId, TVector<string> order)
    {
        var state = _storage.GetOrAdd(userId, _ => new UserSaved());
        var existing = state.Collections.ToArray();
        var ordered = new List<KeyValuePair<string, TStarGiftCollection>>();
        foreach (var slug in order)
        {
            if (state.Collections.TryGetValue(slug, out var coll))
            {
                ordered.Add(new KeyValuePair<string, TStarGiftCollection>(slug, coll));
            }
        }
        // add leftovers
        foreach (var kv in existing)
        {
            if (!ordered.Any(x => x.Key == kv.Key))
            {
                ordered.Add(kv);
            }
        }
        state.Collections.Clear();
        foreach (var kv in ordered)
        {
            state.Collections[kv.Key] = kv.Value;
        }
        return Task.FromResult(true);
    }

    public Task<bool> TogglePinnedAsync(long userId, TVector<IInputSavedStarGift> gifts)
    {
        var state = _storage.GetOrAdd(userId, _ => new UserSaved());
        var set = gifts.Select(GiftKey).ToHashSet();
        foreach (var kv in state.Gifts)
        {
            var isPinned = set.Contains(kv.Key);
            kv.Value.PinnedTop = isPinned;
        }
        return Task.FromResult(true);
    }

    public Task<ISavedStarGifts> GetSavedByKeysAsync(long userId, TVector<IInputSavedStarGift> gifts)
    {
        var state = _storage.GetOrAdd(userId, _ => new UserSaved());
        var keys = gifts.Select(GiftKey).ToHashSet();
        var list = state.Gifts.Where(kv => keys.Contains(kv.Key)).Select(kv => (ISavedStarGift)kv.Value).ToList();
        return Task.FromResult<ISavedStarGifts>(new TSavedStarGifts
        {
            Users = new TVector<IUser>(),
            Chats = new TVector<IChat>(),
            Gifts = new TVector<ISavedStarGift>(list)
        });
    }

    public Task<IStarGiftWithdrawalUrl> GetWithdrawalUrlAsync(long userId, IInputSavedStarGift gift)
    {
        var key = GiftKey(gift);
        var state = _storage.GetOrAdd(userId, _ => new UserSaved());
        if (!state.Gifts.ContainsKey(key))
        {
            RpcErrors.RpcErrors400.BadRequest("STARGIFT_NOT_FOUND").ThrowRpcError();
        }

        // Реалистичный URL (в реальном мире был бы подписанный токен). Здесь — детерминированная ссылка.
        var url = $"https://mytelegram.local/withdraw/stargift/{userId}/{key}";
        return Task.FromResult<IStarGiftWithdrawalUrl>(new TStarGiftWithdrawalUrl { Url = url });
    }

    public TStarGiftUpgradePreview BuildUpgradePreview(long userId, long giftId)
    {
        // Находим подарок среди сохранённых
        var state = _storage.GetOrAdd(userId, _ => new UserSaved());
        var match = state.Gifts.Values.FirstOrDefault(x => x.Gift.Id == giftId);
        if (match == null)
        {
            RpcErrors.RpcErrors400.BadRequest("STARGIFT_NOT_FOUND").ThrowRpcError();
        }

        // Для примера вернём модельный набор атрибутов как превью
        var attrs = new TVector<IStarGiftAttribute>(new IStarGiftAttribute[]
        {
            new TStarGiftAttributeModel{ Model = "upgraded" },
            new TStarGiftAttributePattern{ Pattern = "deluxe" },
            new TStarGiftAttributeBackdrop{ Backdrop = "neon" }
        });
        return new TStarGiftUpgradePreview { SampleAttributes = attrs };
    }

    public Task<bool> ConvertAsync(long userId, IInputSavedStarGift gift)
    {
        var key = GiftKey(gift);
        var state = _storage.GetOrAdd(userId, _ => new UserSaved());
        if (!state.Gifts.TryGetValue(key, out var saved))
        {
            RpcErrors.RpcErrors400.BadRequest("STARGIFT_NOT_FOUND").ThrowRpcError();
        }

        // Считаем конверсию
        var convert = (saved.Gift as TStarGift)?.ConvertStars ?? 0;
        if (convert <= 0)
        {
            RpcErrors.RpcErrors400.BadRequest("STARGIFT_CONVERT_FORBIDDEN").ThrowRpcError();
        }

        // Удаляем подарок у пользователя (уничтожаем)
        state.Gifts.TryRemove(key, out _);
        // Зачисляем звёзды
        state.StarsBalance += convert;

        return Task.FromResult(true);
    }

    public Task<long> GetStarsBalanceAsync(long userId)
    {
        var state = _storage.GetOrAdd(userId, _ => new UserSaved());
        return Task.FromResult(state.StarsBalance);
    }

    public bool TryGetSaved(long userId, IInputSavedStarGift gift, out TSavedStarGift saved)
    {
        var key = GiftKey(gift);
        var state = _storage.GetOrAdd(userId, _ => new UserSaved());
        return state.Gifts.TryGetValue(key, out saved!);
    }

    public void DeductStars(long userId, long amount)
    {
        var state = _storage.GetOrAdd(userId, _ => new UserSaved());
        if (state.StarsBalance < amount)
        {
            RpcErrors.RpcErrors400.BalanceTooLow.ThrowRpcError();
        }
        state.StarsBalance -= amount;
    }

    private static long GiftKey(IInputSavedStarGift g) => g switch
    {
        TInputSavedStarGiftById x => x.GiftId,
        TInputSavedStarGiftBySlug x => x.Slug.GetHashCode(),
        _ => 0
    };

    public bool TransferTo(long fromUserId, long toUserId, IInputSavedStarGift gift, out TSavedStarGift transferred)
    {
        transferred = null!;
        var key = GiftKey(gift);
        var from = _storage.GetOrAdd(fromUserId, _ => new UserSaved());
        if (!from.Gifts.TryRemove(key, out var saved))
        {
            RpcErrors.RpcErrors400.BadRequest("STARGIFT_NOT_FOUND").ThrowRpcError();
        }
        var to = _storage.GetOrAdd(toUserId, _ => new UserSaved());
        // Сбросим пин у получателя; дата обновится
        saved.PinnedTop = false;
        saved.Date = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        to.Gifts[key] = saved;
        transferred = saved;
        return true;
    }

    private static long GiftKeyFromSaved(ISavedStarGift g) => g.Gift.Id;

    private static string Slugify(string title)
    {
        var s = (title ?? string.Empty).Trim().ToLowerInvariant();
        foreach (var c in System.IO.Path.GetInvalidFileNameChars()) s = s.Replace(c, '-');
        s = string.Join('-', s.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(s)) s = $"collection-{Guid.NewGuid():N}";
        return s;
    }

    private class UserSaved
    {
        public ConcurrentDictionary<long, TSavedStarGift> Gifts { get; } = new();
        public ConcurrentDictionary<string, TStarGiftCollection> Collections { get; } = new();
        public long StarsBalance { get; set; }
    }
}

