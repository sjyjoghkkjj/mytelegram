using MyTelegram.Messenger.Services;
using MyTelegram.Schema.Payments;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Payments;

///<summary>
/// Get a list of available <a href="https://corefork.telegram.org/api/gifts">gifts, see here »</a> for more info.
/// See <a href="https://corefork.telegram.org/method/payments.getStarGifts" />
///</summary>
internal sealed class GetStarGiftsHandler(IGiftCatalogService giftCatalogService)
    : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestGetStarGifts, MyTelegram.Schema.Payments.IStarGifts>
{
    protected override Task<MyTelegram.Schema.Payments.IStarGifts> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Payments.RequestGetStarGifts obj)
    {
        // Return catalog gifts
        return HandleAsync();
    }

    private async Task<MyTelegram.Schema.Payments.IStarGifts> HandleAsync()
    {
        var gifts = await giftCatalogService.GetCatalogAsync();
        return new TStarGifts
        {
            Gifts = new TVector<MyTelegram.Schema.IStarGift>(gifts)
        };
    }
}
