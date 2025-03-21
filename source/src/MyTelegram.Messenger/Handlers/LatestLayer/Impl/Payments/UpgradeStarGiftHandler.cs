namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Payments;

///<summary>
/// See <a href="https://corefork.telegram.org/method/payments.upgradeStarGift" />
///</summary>
internal sealed class UpgradeStarGiftHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestUpgradeStarGift, MyTelegram.Schema.IUpdates>,
    Payments.IUpgradeStarGiftHandler
{
    protected override Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Payments.RequestUpgradeStarGift obj)
    {
        throw new NotImplementedException();
    }
}
