// ReSharper disable All

using MyTelegram.Schema.Payments;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Payments;

///<summary>
/// See <a href="https://corefork.telegram.org/method/payments.getStarsSubscriptions" />
///</summary>
internal sealed class GetStarsSubscriptionsHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestGetStarsSubscriptions, MyTelegram.Schema.Payments.IStarsStatus>,
    Payments.IGetStarsSubscriptionsHandler
{
    protected override Task<MyTelegram.Schema.Payments.IStarsStatus> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Payments.RequestGetStarsSubscriptions obj)
    {
        return Task.FromResult<MyTelegram.Schema.Payments.IStarsStatus>(new TStarsStatus
        {
            Balance = new TStarsAmount(),
            Chats = [],
            Users = []
        });
    }
}
