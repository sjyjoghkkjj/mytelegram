// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Fragment;

///<summary>
/// See <a href="https://corefork.telegram.org/method/fragment.getCollectibleInfo" />
///</summary>
internal sealed class GetCollectibleInfoHandler : RpcResultObjectHandler<MyTelegram.Schema.Fragment.RequestGetCollectibleInfo, MyTelegram.Schema.Fragment.ICollectibleInfo>,
    Fragment.IGetCollectibleInfoHandler
{
    protected override Task<MyTelegram.Schema.Fragment.ICollectibleInfo> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Fragment.RequestGetCollectibleInfo obj)
    {
        throw new NotImplementedException();
    }
}
