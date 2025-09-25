using MyTelegram.Schema;
using MyTelegram.Schema.Payments;

namespace MyTelegram.Messenger.Services;

public interface IResaleMarketService : ITransientDependency
{
    Task<IResaleStarGifts> GetAsync(long giftId, bool sortByPrice, bool sortByNum, string offset, int limit, TVector<IStarGiftAttributeId>? attributes, long? attributesHash, CancellationToken ct = default);
    Task<bool> UpsertListingAsync(long userId, IInputSavedStarGift gift, long priceStars);
    Task<bool> RemoveListingAsync(long userId, IInputSavedStarGift gift);
}

public class ResaleMarketService : IResaleMarketService
{
    private readonly List<(long OwnerId, IStarGift Gift, List<IStarGiftAttribute> Attrs)> _listings;

    public ResaleMarketService()
    {
        // Простейшие листинги с атрибутами
        _listings = new List<(long, IStarGift, List<IStarGiftAttribute>)>
        {
            (42,
            new TStarGift
            {
                Id = 2001,
                Title = "Crystal Heart (resale)",
                Stars = 520,
                ConvertStars = 360,
                AvailabilityResale = 1,
                Sticker = new TDocument { Id = 20010, AccessHash = 0, FileReference = ReadOnlyMemory<byte>.Empty, Date = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(), MimeType = "application/x-tgsticker", Size = 0, DcId = 1, Attributes = new TVector<IDocumentAttribute>(Array.Empty<IDocumentAttribute>()) }
            },
            new List<IStarGiftAttribute>{ new TStarGiftAttributeModel{ Model = "heart" } }),
            (43,
            new TStarGift
            {
                Id = 2002,
                Title = "Golden Balloon (resale)",
                Stars = 110,
                ConvertStars = 77,
                AvailabilityResale = 1,
                Sticker = new TDocument { Id = 20020, AccessHash = 0, FileReference = ReadOnlyMemory<byte>.Empty, Date = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(), MimeType = "application/x-tgsticker", Size = 0, DcId = 1, Attributes = new TVector<IDocumentAttribute>(Array.Empty<IDocumentAttribute>()) }
            },
            new List<IStarGiftAttribute>{ new TStarGiftAttributeModel{ Model = "balloon" } })
        };
    }

    public Task<IResaleStarGifts> GetAsync(long giftId, bool sortByPrice, bool sortByNum, string offset, int limit, TVector<IStarGiftAttributeId>? attributes, long? attributesHash, CancellationToken ct = default)
    {
        IEnumerable<(long Owner, IStarGift Gift, List<IStarGiftAttribute> Attrs)> query = _listings;
        if (giftId != 0)
            query = query.Where(x => (x.Gift as TStarGift)?.Id == giftId);
        // фильтр по атрибутам (по id-модели, паттерну, бэкдропу и т.п.) — здесь упростим до модели
        if (attributes != null && attributes.Count > 0)
        {
            var models = attributes.OfType<TStarGiftAttributeIdModel>().Select(a => a.Model).ToHashSet();
            if (models.Count > 0)
                query = query.Where(x => x.Attrs.OfType<TStarGiftAttributeModel>().Any(a => models.Contains(a.Model)));
        }

        if (sortByPrice)
            query = query.OrderBy(x => (x.Gift as TStarGift)!.Stars);
        else if (sortByNum)
            query = query.OrderBy(x => (x.Gift as TStarGift)!.AvailabilityResale ?? 0);

        // пагинация через offset как numeric id
        if (long.TryParse(offset, out var off) && off > 0)
            query = query.Where(x => (x.Gift as TStarGift)!.Id > off);
        var page = query.Take(Math.Max(1, Math.Min(100, limit))).ToList();

        // counters по атрибутам
        var counters = page
            .SelectMany(x => x.Attrs.OfType<TStarGiftAttributeModel>().Select(m => m.Model))
            .GroupBy(m => m)
            .Select(g => (IStarGiftAttributeCounter)new TStarGiftAttributeCounter
            {
                Attribute = new TStarGiftAttributeIdModel { Model = g.Key },
                Count = g.Count()
            })
            .ToList();

        var result = new TResaleStarGifts
        {
            Gifts = new TVector<IStarGift>(page.Select(x => x.Gift).ToList()),
            Attributes = new TVector<IStarGiftAttribute>(page.SelectMany(x => x.Attrs).ToList()),
            Counters = new TVector<IStarGiftAttributeCounter>(counters),
            Users = new TVector<IUser>(),
            Chats = new TVector<IChat>()
        };
        return Task.FromResult<IResaleStarGifts>(result);
    }

    public Task<bool> UpsertListingAsync(long userId, IInputSavedStarGift gift, long priceStars)
    {
        var id = gift switch
        {
            TInputSavedStarGiftById x => x.GiftId,
            TInputSavedStarGiftBySlug x => (long)x.Slug.GetHashCode(),
            _ => 0
        };
        var idx = _listings.FindIndex(x => (x.Gift as TStarGift)!.Id == id && x.OwnerId == userId);
        if (idx >= 0)
        {
            var (owner, g, attrs) = _listings[idx];
            if (g is TStarGift tg)
            {
                tg.Stars = priceStars;
                _listings[idx] = (owner, tg, attrs);
            }
        }
        else
        {
            _listings.Add((userId, new TStarGift
            {
                Id = id,
                Title = "Resale Item",
                Stars = priceStars,
                ConvertStars = (long)(priceStars * 0.7),
                AvailabilityResale = 1,
                Sticker = new TDocument { Id = id * 10, AccessHash = 0, FileReference = ReadOnlyMemory<byte>.Empty, Date = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(), MimeType = "application/x-tgsticker", Size = 0, DcId = 1, Attributes = new TVector<IDocumentAttribute>(Array.Empty<IDocumentAttribute>()) }
            }, new List<IStarGiftAttribute>()));
        }
        return Task.FromResult(true);
    }

    public Task<bool> RemoveListingAsync(long userId, IInputSavedStarGift gift)
    {
        var id = gift switch
        {
            TInputSavedStarGiftById x => x.GiftId,
            TInputSavedStarGiftBySlug x => (long)x.Slug.GetHashCode(),
            _ => 0
        };
        _listings.RemoveAll(x => (x.Gift as TStarGift)!.Id == id && x.OwnerId == userId);
        return Task.FromResult(true);
    }
}

