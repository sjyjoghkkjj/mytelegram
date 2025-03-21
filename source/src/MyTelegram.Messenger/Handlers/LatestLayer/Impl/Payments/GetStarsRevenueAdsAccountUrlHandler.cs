// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Payments;

///<summary>
/// See <a href="https://corefork.telegram.org/method/payments.getStarsRevenueAdsAccountUrl" />
///</summary>
internal sealed class GetStarsRevenueAdsAccountUrlHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestGetStarsRevenueAdsAccountUrl, MyTelegram.Schema.Payments.IStarsRevenueAdsAccountUrl>,
    Payments.IGetStarsRevenueAdsAccountUrlHandler
{
    protected override Task<MyTelegram.Schema.Payments.IStarsRevenueAdsAccountUrl> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Payments.RequestGetStarsRevenueAdsAccountUrl obj)
    {
        throw new NotImplementedException();
    }
}
