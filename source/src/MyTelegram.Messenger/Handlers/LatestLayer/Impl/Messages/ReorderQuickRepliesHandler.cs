// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Messages;

///<summary>
/// See <a href="https://corefork.telegram.org/method/messages.reorderQuickReplies" />
///</summary>
internal sealed class ReorderQuickRepliesHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestReorderQuickReplies, IBool>,
    Messages.IReorderQuickRepliesHandler
{
    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestReorderQuickReplies obj)
    {
        throw new NotImplementedException();
    }
}
