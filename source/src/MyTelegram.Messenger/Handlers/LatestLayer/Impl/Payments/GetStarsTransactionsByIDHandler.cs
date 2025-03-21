// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Payments;

///<summary>
/// See <a href="https://corefork.telegram.org/method/payments.getStarsTransactionsByID" />
///</summary>
internal sealed class GetStarsTransactionsByIDHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestGetStarsTransactionsByID, MyTelegram.Schema.Payments.IStarsStatus>,
    Payments.IGetStarsTransactionsByIDHandler
{
    protected override Task<MyTelegram.Schema.Payments.IStarsStatus> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Payments.RequestGetStarsTransactionsByID obj)
    {
        throw new NotImplementedException();
    }
}
