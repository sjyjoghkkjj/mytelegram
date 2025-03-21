// ReSharper disable All

using MyTelegram.Schema.Payments;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Payments;

///<summary>
/// See <a href="https://corefork.telegram.org/method/payments.getStarsTransactions" />
///</summary>
internal sealed class GetStarsTransactionsHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestGetStarsTransactions, MyTelegram.Schema.Payments.IStarsStatus>,
    Payments.IGetStarsTransactionsHandler
{
    protected override Task<MyTelegram.Schema.Payments.IStarsStatus> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Payments.RequestGetStarsTransactions obj)
    {
        return Task.FromResult<MyTelegram.Schema.Payments.IStarsStatus>(new TStarsStatus
        {
            Balance = new TStarsAmount(),
            Chats = [],
            Users = []
        });
    }
}
