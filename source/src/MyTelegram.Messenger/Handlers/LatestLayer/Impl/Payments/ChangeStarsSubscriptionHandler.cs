// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Payments;

///<summary>
/// See <a href="https://corefork.telegram.org/method/payments.changeStarsSubscription" />
///</summary>
internal sealed class ChangeStarsSubscriptionHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestChangeStarsSubscription, IBool>,
    Payments.IChangeStarsSubscriptionHandler
{
    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Payments.RequestChangeStarsSubscription obj)
    {
        throw new NotImplementedException();
    }
}
