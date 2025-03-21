// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Messages;

///<summary>
/// See <a href="https://corefork.telegram.org/method/messages.editQuickReplyShortcut" />
///</summary>
internal sealed class EditQuickReplyShortcutHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestEditQuickReplyShortcut, IBool>,
    Messages.IEditQuickReplyShortcutHandler
{
    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestEditQuickReplyShortcut obj)
    {
        throw new NotImplementedException();
    }
}
