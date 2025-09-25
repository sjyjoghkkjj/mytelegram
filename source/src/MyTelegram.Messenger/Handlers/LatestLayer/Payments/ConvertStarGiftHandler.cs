using MyTelegram.Messenger.Services;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Payments;

///<summary>
/// Convert a <a href="https://corefork.telegram.org/api/gifts">received gift »</a> into Telegram Stars: this will permanently destroy the gift, converting it into <a href="https://corefork.telegram.org/constructor/starGift">starGift</a>.<code>convert_stars</code> <a href="https://corefork.telegram.org/api/stars">Telegram Stars</a>, added to the user's balance.Note that <a href="https://corefork.telegram.org/constructor/starGift">starGift</a>.<code>convert_stars</code> will be less than the buying price (<a href="https://corefork.telegram.org/constructor/starGift">starGift</a>.<code>stars</code>) of the gift if it was originally bought using Telegram Stars bought a long time ago.
/// See <a href="https://corefork.telegram.org/method/payments.convertStarGift" />
///</summary>
internal sealed class ConvertStarGiftHandler(ISavedStarGiftsService savedService, IResponseCacheAppService responseCache) : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestConvertStarGift, IBool>
{
    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Payments.RequestConvertStarGift obj)
    {
        return HandleAsync(input, obj);
    }

    private async Task<IBool> HandleAsync(IRequestInput input, MyTelegram.Schema.Payments.RequestConvertStarGift obj)
    {
        await savedService.ConvertAsync(input.UserId, obj.Stargift);

        // Отправим апдейт об изменении баланса звёзд
        var balance = await savedService.GetStarsBalanceAsync(input.UserId);
        var update = new TUpdateStarsBalance { Stars = balance };
        await responseCache.PushToQueueAsync(input.UserId, update);

        return new TBoolTrue();
    }
}
