namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Payments;

///<summary>
/// Get <a href="https://corefork.telegram.org/api/stars">Telegram Star revenue statistics »</a>.
/// <para>Possible errors</para>
/// Code Type Description
/// 400 PEER_ID_INVALID The provided peer id is invalid.
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
