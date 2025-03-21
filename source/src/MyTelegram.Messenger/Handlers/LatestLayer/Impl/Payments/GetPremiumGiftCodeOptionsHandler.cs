// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Payments;

///<summary>
/// See <a href="https://corefork.telegram.org/method/payments.getPremiumGiftCodeOptions" />
///</summary>
internal sealed class GetPremiumGiftCodeOptionsHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestGetPremiumGiftCodeOptions, TVector<MyTelegram.Schema.IPremiumGiftCodeOption>>,
    Payments.IGetPremiumGiftCodeOptionsHandler
{
    protected override Task<TVector<MyTelegram.Schema.IPremiumGiftCodeOption>> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Payments.RequestGetPremiumGiftCodeOptions obj)
    {
        return Task.FromResult<TVector<MyTelegram.Schema.IPremiumGiftCodeOption>>(
            new TVector<IPremiumGiftCodeOption>
            {
                new TPremiumGiftCodeOption
                {
                    Months=3,
                    Currency="USD",
                    Users=1
                }
            });
    }
}
