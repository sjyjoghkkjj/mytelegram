// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Payments;

///<summary>
/// See <a href="https://corefork.telegram.org/method/payments.sendStarsForm" />
///</summary>
internal sealed class SendStarsFormHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestSendStarsForm, MyTelegram.Schema.Payments.IPaymentResult>,
    Payments.ISendStarsFormHandler
{
    protected override Task<MyTelegram.Schema.Payments.IPaymentResult> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Payments.RequestSendStarsForm obj)
    {
        throw new NotImplementedException();
    }
}
