// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Payments;

///<summary>
/// See <a href="https://corefork.telegram.org/method/payments.getStarsGiveawayOptions" />
///</summary>
internal sealed class GetStarsGiveawayOptionsHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestGetStarsGiveawayOptions, TVector<MyTelegram.Schema.IStarsGiveawayOption>>,
    Payments.IGetStarsGiveawayOptionsHandler
{
    protected override Task<TVector<MyTelegram.Schema.IStarsGiveawayOption>> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Payments.RequestGetStarsGiveawayOptions obj)
    {
        throw new NotImplementedException();
    }
}
