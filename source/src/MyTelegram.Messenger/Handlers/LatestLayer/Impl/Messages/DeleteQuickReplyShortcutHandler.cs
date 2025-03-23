// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Messages;

///<summary>
/// See <a href="https://corefork.telegram.org/method/messages.deleteQuickReplyShortcut" />
///</summary>
internal sealed class DeleteQuickReplyShortcutHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestDeleteQuickReplyShortcut, IBool>,
    Messages.IDeleteQuickReplyShortcutHandler
{
    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestDeleteQuickReplyShortcut obj)
    {
        throw new NotImplementedException();
    }
}
