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
            new List<IStarGiftAttribute>{
                new TStarGiftAttributeModel{ Model = "heart" },
                new TStarGiftAttributePattern{ Pattern = "crystal" },
                new TStarGiftAttributeBackdrop{ Backdrop = "pink" },
                new TStarGiftAttributeOriginalDetails{ Url = "https://example.com/item/2001" }
            }),
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
            new List<IStarGiftAttribute>{
                new TStarGiftAttributeModel{ Model = "balloon" },
                new TStarGiftAttributePattern{ Pattern = "gold" },
                new TStarGiftAttributeBackdrop{ Backdrop = "blue" },
                new TStarGiftAttributeOriginalDetails{ Url = "https://example.com/item/2002" }
            })
        };
    }

    public Task<IResaleStarGifts> GetAsync(long giftId, bool sortByPrice, bool sortByNum, string offset, int limit, TVector<IStarGiftAttributeId>? attributes, long? attributesHash, CancellationToken ct = default)
    {
        IEnumerable<(long Owner, IStarGift Gift, List<IStarGiftAttribute> Attrs)> query = _listings;
        if (giftId != 0)
            query = query.Where(x => (x.Gift as TStarGift)?.Id == giftId);
        // фильтры по атрибутам (model/pattern/backdrop)
        if (attributes != null && attributes.Count > 0)
        {
            var models = attributes.OfType<TStarGiftAttributeIdModel>().Select(a => a.Model).ToHashSet();
            var patterns = attributes.OfType<TStarGiftAttributeIdPattern>().Select(a => a.Pattern).ToHashSet();
            var backdrops = attributes.OfType<TStarGiftAttributeIdBackdrop>().Select(a => a.Backdrop).ToHashSet();

            if (models.Count > 0)
                query = query.Where(x => x.Attrs.OfType<TStarGiftAttributeModel>().Any(a => models.Contains(a.Model)));
            if (patterns.Count > 0)
                query = query.Where(x => x.Attrs.OfType<TStarGiftAttributePattern>().Any(a => patterns.Contains(a.Pattern)));
            if (backdrops.Count > 0)
                query = query.Where(x => x.Attrs.OfType<TStarGiftAttributeBackdrop>().Any(a => backdrops.Contains(a.Backdrop)));
        }

        if (sortByPrice)
            query = query.OrderBy(x => (x.Gift as TStarGift)!.Stars).ThenBy(x => (x.Gift as TStarGift)!.Id);
        else if (sortByNum)
            query = query.OrderBy(x => (x.Gift as TStarGift)!.AvailabilityResale ?? 0).ThenBy(x => (x.Gift as TStarGift)!.Id);

        // пагинация через offset как numeric id
        if (long.TryParse(offset, out var off) && off > 0)
            query = query.Where(x => (x.Gift as TStarGift)!.Id > off);
        var take = Math.Max(1, Math.Min(100, limit));
        var page = query.Take(take).ToList();
        var lastId = page.LastOrDefault().Gift is TStarGift lg ? lg.Id : 0;

        // counters по атрибутам (model/pattern/backdrop)
        var counters = new List<IStarGiftAttributeCounter>();
        var modelCounters = page.SelectMany(x => x.Attrs.OfType<TStarGiftAttributeModel>().Select(m => m.Model))
            .GroupBy(v => v)
            .Select(g => (IStarGiftAttributeCounter)new TStarGiftAttributeCounter
            {
                Attribute = new TStarGiftAttributeIdModel { Model = g.Key },
                Count = g.Count()
            });
        var patternCounters = page.SelectMany(x => x.Attrs.OfType<TStarGiftAttributePattern>().Select(m => m.Pattern))
            .GroupBy(v => v)
            .Select(g => (IStarGiftAttributeCounter)new TStarGiftAttributeCounter
            {
                Attribute = new TStarGiftAttributeIdPattern { Pattern = g.Key },
                Count = g.Count()
            });
        var backdropCounters = page.SelectMany(x => x.Attrs.OfType<TStarGiftAttributeBackdrop>().Select(m => m.Backdrop))
            .GroupBy(v => v)
            .Select(g => (IStarGiftAttributeCounter)new TStarGiftAttributeCounter
            {
                Attribute = new TStarGiftAttributeIdBackdrop { Backdrop = g.Key },
                Count = g.Count()
            });
        counters.AddRange(modelCounters);
        counters.AddRange(patternCounters);
        counters.AddRange(backdropCounters);

        // вычислим hash атрибутов+счётчиков для кэш-валидации
        var allAttrs = page.SelectMany(x => x.Attrs).ToList();
        var hash = ComputeAttributesHash(allAttrs, counters);
        var attrsOut = (attributesHash.HasValue && attributesHash.Value == hash)
            ? new TVector<IStarGiftAttribute>()
            : new TVector<IStarGiftAttribute>(allAttrs);
        var countersOut = (attributesHash.HasValue && attributesHash.Value == hash)
            ? new TVector<IStarGiftAttributeCounter>()
            : new TVector<IStarGiftAttributeCounter>(counters);

        var result = new TResaleStarGifts
        {
            Gifts = new TVector<IStarGift>(page.Select(x => x.Gift).ToList()),
            Attributes = attrsOut,
            Counters = countersOut,
            Users = new TVector<IUser>(),
            Chats = new TVector<IChat>(),
            NextOffset = page.Count == take && lastId > 0 ? lastId.ToString() : null
        };
        return Task.FromResult<IResaleStarGifts>(result);
    }

    private static long ComputeAttributesHash(IEnumerable<IStarGiftAttribute> attrs, IEnumerable<IStarGiftAttributeCounter> counters)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        void AddString(string s)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(s);
            sha.TransformBlock(bytes, 0, bytes.Length, null, 0);
        }

        foreach (var a in attrs)
        {
            switch (a)
            {
                case TStarGiftAttributeModel m:
                    AddString($"m:{m.Model}");
                    break;
                case TStarGiftAttributePattern p:
                    AddString($"p:{p.Pattern}");
                    break;
                case TStarGiftAttributeBackdrop b:
                    AddString($"b:{b.Backdrop}");
                    break;
                case TStarGiftAttributeOriginalDetails od:
                    AddString($"o:{od.Url}");
                    break;
            }
        }

        foreach (var c in counters)
        {
            switch (c.Attribute)
            {
                case TStarGiftAttributeIdModel m:
                    AddString($"cm:{m.Model}:{c.Count}");
                    break;
                case TStarGiftAttributeIdPattern p:
                    AddString($"cp:{p.Pattern}:{c.Count}");
                    break;
                case TStarGiftAttributeIdBackdrop b:
                    AddString($"cb:{b.Backdrop}:{c.Count}");
                    break;
            }
        }

        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        var hashBytes = sha.Hash!;
        // первые 8 байт в Int64 (little-endian)
        return System.BitConverter.ToInt64(hashBytes, 0);
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

