// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Messages;

///<summary>
/// See <a href="https://corefork.telegram.org/method/messages.editFactCheck" />
///</summary>
internal sealed class EditFactCheckHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestEditFactCheck, MyTelegram.Schema.IUpdates>,
    Messages.IEditFactCheckHandler
{
    protected override Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestEditFactCheck obj)
    {
        throw new NotImplementedException();
    }
}
