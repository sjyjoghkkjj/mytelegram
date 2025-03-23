using MyTelegram.Schema.Payments;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Payments;

///<summary>
/// See <a href="https://corefork.telegram.org/method/payments.getSavedStarGifts" />
///</summary>
internal sealed class GetSavedStarGiftsHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestGetSavedStarGifts, MyTelegram.Schema.Payments.ISavedStarGifts>,
    Payments.IGetSavedStarGiftsHandler
{
    protected override Task<MyTelegram.Schema.Payments.ISavedStarGifts> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Payments.RequestGetSavedStarGifts obj)
    {
        return Task.FromResult<MyTelegram.Schema.Payments.ISavedStarGifts>(new TSavedStarGifts
        {
            Chats = [],
            Gifts = [],
            Users = []
        });
    }
}
