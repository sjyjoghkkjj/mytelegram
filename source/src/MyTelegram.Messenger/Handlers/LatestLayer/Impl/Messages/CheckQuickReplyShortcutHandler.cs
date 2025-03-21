// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Messages;

///<summary>
/// See <a href="https://corefork.telegram.org/method/messages.checkQuickReplyShortcut" />
///</summary>
internal sealed class CheckQuickReplyShortcutHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestCheckQuickReplyShortcut, IBool>,
    Messages.ICheckQuickReplyShortcutHandler
{
    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestCheckQuickReplyShortcut obj)
    {
        throw new NotImplementedException();
    }
}
