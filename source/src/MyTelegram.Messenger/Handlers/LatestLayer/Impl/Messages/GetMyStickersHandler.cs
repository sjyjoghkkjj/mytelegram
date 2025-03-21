// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Messages;

///<summary>
/// See <a href="https://corefork.telegram.org/method/messages.getMyStickers" />
///</summary>
internal sealed class GetMyStickersHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestGetMyStickers, MyTelegram.Schema.Messages.IMyStickers>,
    Messages.IGetMyStickersHandler
{
    protected override Task<MyTelegram.Schema.Messages.IMyStickers> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestGetMyStickers obj)
    {
        throw new NotImplementedException();
    }
}
