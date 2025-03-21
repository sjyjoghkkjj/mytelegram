// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Payments;

///<summary>
/// See <a href="https://corefork.telegram.org/method/payments.getStarsTopupOptions" />
///</summary>
internal sealed class GetStarsTopupOptionsHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestGetStarsTopupOptions, TVector<MyTelegram.Schema.IStarsTopupOption>>,
    Payments.IGetStarsTopupOptionsHandler
{
    protected override Task<TVector<MyTelegram.Schema.IStarsTopupOption>> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Payments.RequestGetStarsTopupOptions obj)
    {
        return Task.FromResult<TVector<MyTelegram.Schema.IStarsTopupOption>>([]);
    }
}
