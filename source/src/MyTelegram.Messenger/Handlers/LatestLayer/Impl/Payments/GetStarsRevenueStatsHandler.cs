// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Payments;

///<summary>
/// See <a href="https://corefork.telegram.org/method/payments.getStarsRevenueStats" />
///</summary>
internal sealed class GetStarsRevenueStatsHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestGetStarsRevenueStats, MyTelegram.Schema.Payments.IStarsRevenueStats>,
    Payments.IGetStarsRevenueStatsHandler
{
    protected override Task<MyTelegram.Schema.Payments.IStarsRevenueStats> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Payments.RequestGetStarsRevenueStats obj)
    {
        throw new NotImplementedException();
    }
}
