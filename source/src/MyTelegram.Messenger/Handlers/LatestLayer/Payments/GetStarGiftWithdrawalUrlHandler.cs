using MyTelegram.Messenger.Services;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Payments;

///<summary>
/// See <a href="https://corefork.telegram.org/method/payments.getStarGiftWithdrawalUrl" />
///</summary>
internal sealed class GetStarGiftWithdrawalUrlHandler(ISavedStarGiftsService savedService) : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestGetStarGiftWithdrawalUrl, MyTelegram.Schema.Payments.IStarGiftWithdrawalUrl>
{
    protected override Task<MyTelegram.Schema.Payments.IStarGiftWithdrawalUrl> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Payments.RequestGetStarGiftWithdrawalUrl obj)
    {
        return savedService.GetWithdrawalUrlAsync(input.UserId, obj.Stargift);
    }
}
