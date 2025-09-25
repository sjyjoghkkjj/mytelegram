using MyTelegram.Messenger.Services;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Payments;

///<summary>
/// See <a href="https://corefork.telegram.org/method/payments.getUniqueStarGift" />
///</summary>
internal sealed class GetUniqueStarGiftHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestGetUniqueStarGift, MyTelegram.Schema.Payments.IUniqueStarGift>
{
    protected override Task<MyTelegram.Schema.Payments.IUniqueStarGift> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Payments.RequestGetUniqueStarGift obj)
    {
        // Минимальная реализация: возвращаем уникальный подарок с атрибутами; в реальном мире подтягивается из БД/маркетплейса
        var gift = new MyTelegram.Schema.TStarGiftUnique
        {
            Id = obj.GiftId,
            Title = "Unique Gift",
            Stars = 1000,
            ConvertStars = 700,
            Sticker = new MyTelegram.Schema.TDocument { Id = obj.GiftId * 10, AccessHash = 0, FileReference = ReadOnlyMemory<byte>.Empty, Date = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(), MimeType = "application/x-tgsticker", Size = 0, DcId = 1, Attributes = new TVector<MyTelegram.Schema.IDocumentAttribute>(Array.Empty<MyTelegram.Schema.IDocumentAttribute>()) },
            Attributes = new TVector<MyTelegram.Schema.IStarGiftAttribute>(new MyTelegram.Schema.IStarGiftAttribute[]
            {
                new MyTelegram.Schema.TStarGiftAttributeModel{ Model = "unique" },
                new MyTelegram.Schema.TStarGiftAttributePattern{ Pattern = "oneofone" },
            })
        };
        var result = new MyTelegram.Schema.Payments.TUniqueStarGift { Gift = gift };
        return Task.FromResult<MyTelegram.Schema.Payments.IUniqueStarGift>(result);
    }
}
