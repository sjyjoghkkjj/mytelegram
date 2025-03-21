// ReSharper disable All

using MyTelegram.Schema.Payments;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Payments;

///<summary>
/// See <a href="https://corefork.telegram.org/method/payments.getStarsStatus" />
///</summary>
internal sealed class GetStarsStatusHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestGetStarsStatus, MyTelegram.Schema.Payments.IStarsStatus>,
    Payments.IGetStarsStatusHandler
{
    protected override Task<MyTelegram.Schema.Payments.IStarsStatus> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Payments.RequestGetStarsStatus obj)
    {
        return Task.FromResult<MyTelegram.Schema.Payments.IStarsStatus>(new TStarsStatus
        {
            Balance = new TStarsAmount(),
            Chats = [],
            History = [],
            Users = []
        });
    }
}
