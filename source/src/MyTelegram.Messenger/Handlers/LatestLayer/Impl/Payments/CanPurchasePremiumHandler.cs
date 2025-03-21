namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Payments;

///<summary>
/// Checks whether Telegram Premium purchase is possible. Must be called before in-store Premium purchase, official apps only.
/// <para>Possible errors</para>
/// Code Type Description
/// 406 PREMIUM_CURRENTLY_UNAVAILABLE You cannot currently purchase a Premium subscription.
/// See <a href="https://corefork.telegram.org/method/payments.canPurchasePremium" />
///</summary>
internal sealed class CanPurchasePremiumHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestCanPurchasePremium, IBool>,
    Payments.ICanPurchasePremiumHandler
{
    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Payments.RequestCanPurchasePremium obj)
    {
        throw new NotImplementedException();
    }
}
