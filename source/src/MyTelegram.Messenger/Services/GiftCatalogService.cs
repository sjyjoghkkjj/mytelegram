using MyTelegram.Schema;

namespace MyTelegram.Messenger.Services;

public class GiftCatalogService : IGiftCatalogService
{
    private readonly IReadOnlyList<IStarGift> _gifts;

    public GiftCatalogService()
    {
        // Minimal sample catalog with synthetic sticker docs and prices
        _gifts = new List<IStarGift>
        {
            CreateGift(id: 1001, title: "Golden Balloon", stars: 99, limited: false),
            CreateGift(id: 1002, title: "Crystal Heart", stars: 499, limited: true, total: 1000, remains: 1000),
            CreateGift(id: 1003, title: "Dragon Flame", stars: 3999, limited: true, total: 100, remains: 100)
        };
    }

    public Task<IReadOnlyList<IStarGift>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_gifts);
    }

    private static TStarGift CreateGift(long id, string title, long stars, bool limited, int? total = null, int? remains = null)
    {
        var now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var document = new TDocument
        {
            Id = id * 10 + 1,
            AccessHash = 0,
            FileReference = ReadOnlyMemory<byte>.Empty,
            Date = now,
            MimeType = "application/x-tgsticker",
            Size = 0,
            DcId = 1,
            Attributes = new TVector<IDocumentAttribute>(new IDocumentAttribute[]
            {
                new TDocumentAttributeSticker { Alt = title, Mask = false },
                new TDocumentAttributeAnimated()
            })
        };

        var gift = new TStarGift
        {
            Id = id,
            Title = title,
            Stars = stars,
            ConvertStars = Math.Max(1, (long)(stars * 0.7)),
            Sticker = document,
            Limited = limited,
            AvailabilityTotal = total,
            AvailabilityRemains = remains,
            SoldOut = limited && remains == 0,
            Birthday = false,
            RequirePremium = false,
            LimitedPerUser = false,
            PerUserTotal = null,
            PerUserRemains = null
        };

        return gift;
    }
}

