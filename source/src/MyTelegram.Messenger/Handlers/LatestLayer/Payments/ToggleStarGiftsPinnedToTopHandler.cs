using MyTelegram.Messenger.Services;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Payments;

///<summary>
/// See <a href="https://corefork.telegram.org/method/payments.toggleStarGiftsPinnedToTop" />
///</summary>
internal sealed class ToggleStarGiftsPinnedToTopHandler(ISavedStarGiftsService savedService) : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestToggleStarGiftsPinnedToTop, IBool>
{
    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Payments.RequestToggleStarGiftsPinnedToTop obj)
    {
        return HandleAsync(input, obj);
    }

    private async Task<IBool> HandleAsync(IRequestInput input, MyTelegram.Schema.Payments.RequestToggleStarGiftsPinnedToTop obj)
    {
        await savedService.TogglePinnedAsync(input.UserId, obj.Stargift);
        return new TBoolTrue();
    }
}
