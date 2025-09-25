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

    private static long GiftKey(IInputSavedStarGift g) => g switch
    {
        TInputSavedStarGiftById x => x.GiftId,
        TInputSavedStarGiftBySlug x => x.Slug.GetHashCode(),
        _ => 0
    };

    private static long GiftKeyFromSaved(ISavedStarGift g) => g.Gift.Id;

    private class UserSaved
    {
        public ConcurrentDictionary<long, TSavedStarGift> Gifts { get; } = new();
        public ConcurrentDictionary<string, TStarGiftCollection> Collections { get; } = new();
        public long StarsBalance { get; set; }
    }
}

