// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Messages;

///<summary>
/// See <a href="https://corefork.telegram.org/method/messages.deleteFactCheck" />
///</summary>
internal sealed class DeleteFactCheckHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestDeleteFactCheck, MyTelegram.Schema.IUpdates>,
    Messages.IDeleteFactCheckHandler
{
    protected override Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestDeleteFactCheck obj)
    {
        throw new NotImplementedException();
    }
}
