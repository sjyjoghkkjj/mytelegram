// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Stats;

///<summary>
/// See <a href="https://corefork.telegram.org/method/stats.getBroadcastRevenueTransactions" />
///</summary>
internal sealed class GetBroadcastRevenueTransactionsHandler : RpcResultObjectHandler<MyTelegram.Schema.Stats.RequestGetBroadcastRevenueTransactions, MyTelegram.Schema.Stats.IBroadcastRevenueTransactions>,
    Stats.IGetBroadcastRevenueTransactionsHandler
{
    protected override Task<MyTelegram.Schema.Stats.IBroadcastRevenueTransactions> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Stats.RequestGetBroadcastRevenueTransactions obj)
    {
        throw new NotImplementedException();
    }
}
