// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Stats;

///<summary>
/// See <a href="https://corefork.telegram.org/method/stats.getBroadcastRevenueWithdrawalUrl" />
///</summary>
internal sealed class GetBroadcastRevenueWithdrawalUrlHandler : RpcResultObjectHandler<MyTelegram.Schema.Stats.RequestGetBroadcastRevenueWithdrawalUrl, MyTelegram.Schema.Stats.IBroadcastRevenueWithdrawalUrl>,
    Stats.IGetBroadcastRevenueWithdrawalUrlHandler
{
    protected override Task<MyTelegram.Schema.Stats.IBroadcastRevenueWithdrawalUrl> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Stats.RequestGetBroadcastRevenueWithdrawalUrl obj)
    {
        throw new NotImplementedException();
    }
}
