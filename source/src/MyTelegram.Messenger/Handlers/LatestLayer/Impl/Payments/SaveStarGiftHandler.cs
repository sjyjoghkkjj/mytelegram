// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Payments;

///<summary>
/// See <a href="https://corefork.telegram.org/method/payments.saveStarGift" />
///</summary>
internal sealed class SaveStarGiftHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestSaveStarGift, IBool>,
    Payments.ISaveStarGiftHandler
{
    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Payments.RequestSaveStarGift obj)
    {
        throw new NotImplementedException();
    }
}
