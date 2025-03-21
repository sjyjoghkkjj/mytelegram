// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Stats;

///<summary>
/// See <a href="https://corefork.telegram.org/method/stats.getBroadcastRevenueStats" />
///</summary>
internal sealed class GetBroadcastRevenueStatsHandler : RpcResultObjectHandler<MyTelegram.Schema.Stats.RequestGetBroadcastRevenueStats, MyTelegram.Schema.Stats.IBroadcastRevenueStats>,
    Stats.IGetBroadcastRevenueStatsHandler
{
    protected override Task<MyTelegram.Schema.Stats.IBroadcastRevenueStats> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Stats.RequestGetBroadcastRevenueStats obj)
    {
        throw new NotImplementedException();
    }
}
