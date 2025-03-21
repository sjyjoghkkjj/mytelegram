//// ReSharper disable All

//using MyTelegram.Schema.Payments;

//namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Payments;

/////<summary>
///// Get the <a href="https://corefork.telegram.org/api/gifts">gifts »</a> pinned on a specific user's profile.May also be used to fetch all gifts received by the current user.
///// See <a href="https://corefork.telegram.org/method/payments.getUserStarGifts" />
/////</summary>
//internal sealed class GetUserStarGiftsHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestGetUserStarGifts, MyTelegram.Schema.Payments.IUserStarGifts>,
//    Payments.IGetUserStarGiftsHandler
//{
//    protected override Task<MyTelegram.Schema.Payments.IUserStarGifts> HandleCoreAsync(IRequestInput input,
//        MyTelegram.Schema.Payments.RequestGetUserStarGifts obj)
//    {
//        return Task.FromResult<MyTelegram.Schema.Payments.IUserStarGifts>(new TUserStarGifts
//        {
//            Gifts = [],
//            Users = []
//        });
//    }
//}
