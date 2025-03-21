// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Payments;

///<summary>
/// See <a href="https://corefork.telegram.org/method/payments.getStarsRevenueWithdrawalUrl" />
///</summary>
internal sealed class GetStarsRevenueWithdrawalUrlHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestGetStarsRevenueWithdrawalUrl, MyTelegram.Schema.Payments.IStarsRevenueWithdrawalUrl>,
    Payments.IGetStarsRevenueWithdrawalUrlHandler
{
    protected override Task<MyTelegram.Schema.Payments.IStarsRevenueWithdrawalUrl> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Payments.RequestGetStarsRevenueWithdrawalUrl obj)
    {
        throw new NotImplementedException();
    }
}
