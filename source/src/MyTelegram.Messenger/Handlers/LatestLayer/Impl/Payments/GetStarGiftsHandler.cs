// ReSharper disable All

using MyTelegram.Schema.Payments;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Payments;

///<summary>
/// See <a href="https://corefork.telegram.org/method/payments.getStarGifts" />
///</summary>
internal sealed class GetStarGiftsHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestGetStarGifts, MyTelegram.Schema.Payments.IStarGifts>,
    Payments.IGetStarGiftsHandler
{
    protected override Task<MyTelegram.Schema.Payments.IStarGifts> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Payments.RequestGetStarGifts obj)
    {
        return Task.FromResult<MyTelegram.Schema.Payments.IStarGifts>(new TStarGifts
        {
            Gifts = []
        });
    }
}
