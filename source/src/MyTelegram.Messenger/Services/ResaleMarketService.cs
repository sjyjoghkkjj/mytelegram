using MyTelegram.Schema;
using MyTelegram.Schema.Payments;

namespace MyTelegram.Messenger.Services;

public interface IResaleMarketService : ITransientDependency
{
    Task<IResaleStarGifts> GetAsync(long? minPrice, long? maxPrice, TVector<IStarGiftAttributeId>? attributes, CancellationToken ct = default);
}

public class ResaleMarketService : IResaleMarketService
{
    private readonly List<IStarGift> _listings;

    public ResaleMarketService()
    {
        // Простейшие листинги с атрибутами
        _listings = new List<IStarGift>
        {
            new TStarGift
            {
                Id = 2001,
                Title = "Crystal Heart (resale)",
                Stars = 520,
                ConvertStars = 360,
                AvailabilityResale = 1,
                Sticker = new TDocument { Id = 20010, AccessHash = 0, FileReference = ReadOnlyMemory<byte>.Empty, Date = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(), MimeType = "application/x-tgsticker", Size = 0, DcId = 1, Attributes = new TVector<IDocumentAttribute>(Array.Empty<IDocumentAttribute>()) }
            },
            new TStarGift
            {
                Id = 2002,
                Title = "Golden Balloon (resale)",
                Stars = 110,
                ConvertStars = 77,
                AvailabilityResale = 1,
                Sticker = new TDocument { Id = 20020, AccessHash = 0, FileReference = ReadOnlyMemory<byte>.Empty, Date = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(), MimeType = "application/x-tgsticker", Size = 0, DcId = 1, Attributes = new TVector<IDocumentAttribute>(Array.Empty<IDocumentAttribute>()) }
            }
        };
    }

    public Task<IResaleStarGifts> GetAsync(long? minPrice, long? maxPrice, TVector<IStarGiftAttributeId>? attributes, CancellationToken ct = default)
    {
        IEnumerable<IStarGift> query = _listings;
        if (minPrice.HasValue)
            query = query.Where(g => (g as TStarGift)?.Stars >= minPrice);
        if (maxPrice.HasValue)
            query = query.Where(g => (g as TStarGift)?.Stars <= maxPrice);

        // Для примера атрибуты не храним подробно; вернём пустые counters/attributes
        var result = new TResaleStarGifts
        {
            Gifts = new TVector<IStarGift>(query.ToList()),
            Attributes = new TVector<IStarGiftAttribute>(),
            Counters = new TVector<IStarGiftAttributeCounter>(),
            Users = new TVector<IUser>(),
            Chats = new TVector<IChat>()
        };
        return Task.FromResult<IResaleStarGifts>(result);
    }
}

