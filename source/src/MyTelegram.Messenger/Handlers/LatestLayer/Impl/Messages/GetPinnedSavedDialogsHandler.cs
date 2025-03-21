// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Messages;

///<summary>
/// See <a href="https://corefork.telegram.org/method/messages.getPinnedSavedDialogs" />
///</summary>
internal sealed class GetPinnedSavedDialogsHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestGetPinnedSavedDialogs, MyTelegram.Schema.Messages.ISavedDialogs>,
    Messages.IGetPinnedSavedDialogsHandler
{
    protected override Task<MyTelegram.Schema.Messages.ISavedDialogs> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestGetPinnedSavedDialogs obj)
    {
        return Task.FromResult<MyTelegram.Schema.Messages.ISavedDialogs>(new TSavedDialogs
        {
            Dialogs = new(),
            Chats = new(),
            Messages = new(),
            Users = new()
        });
    }
}
