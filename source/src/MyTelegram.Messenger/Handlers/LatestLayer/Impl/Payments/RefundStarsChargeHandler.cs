// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Payments;

///<summary>
/// See <a href="https://corefork.telegram.org/method/payments.refundStarsCharge" />
///</summary>
internal sealed class RefundStarsChargeHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestRefundStarsCharge, MyTelegram.Schema.IUpdates>,
    Payments.IRefundStarsChargeHandler
{
    protected override Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Payments.RequestRefundStarsCharge obj)
    {
        throw new NotImplementedException();
    }
}
