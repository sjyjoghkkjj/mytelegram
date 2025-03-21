namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Payments;

///<summary>
/// See <a href="https://corefork.telegram.org/method/payments.getStarGiftUpgradePreview" />
///</summary>
internal sealed class GetStarGiftUpgradePreviewHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestGetStarGiftUpgradePreview, MyTelegram.Schema.Payments.IStarGiftUpgradePreview>,
    Payments.IGetStarGiftUpgradePreviewHandler
{
    protected override Task<MyTelegram.Schema.Payments.IStarGiftUpgradePreview> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Payments.RequestGetStarGiftUpgradePreview obj)
    {
        throw new NotImplementedException();
    }
}
