// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Payments;

///<summary>
/// See <a href="https://corefork.telegram.org/method/payments.convertStarGift" />
///</summary>
internal sealed class ConvertStarGiftHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestConvertStarGift, IBool>,
    Payments.IConvertStarGiftHandler
{
    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Payments.RequestConvertStarGift obj)
    {
        throw new NotImplementedException();
    }
}
