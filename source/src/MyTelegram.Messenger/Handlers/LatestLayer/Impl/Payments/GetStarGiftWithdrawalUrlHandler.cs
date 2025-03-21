namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Payments;

///<summary>
/// See <a href="https://corefork.telegram.org/method/payments.getStarGiftWithdrawalUrl" />
///</summary>
internal sealed class GetStarGiftWithdrawalUrlHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestGetStarGiftWithdrawalUrl, MyTelegram.Schema.Payments.IStarGiftWithdrawalUrl>,
    Payments.IGetStarGiftWithdrawalUrlHandler
{
    protected override Task<MyTelegram.Schema.Payments.IStarGiftWithdrawalUrl> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Payments.RequestGetStarGiftWithdrawalUrl obj)
    {
        throw new NotImplementedException();
    }
}
