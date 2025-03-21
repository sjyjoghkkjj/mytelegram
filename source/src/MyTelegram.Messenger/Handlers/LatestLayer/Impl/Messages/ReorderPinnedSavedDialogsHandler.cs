// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Messages;

///<summary>
/// See <a href="https://corefork.telegram.org/method/messages.reorderPinnedSavedDialogs" />
///</summary>
internal sealed class ReorderPinnedSavedDialogsHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestReorderPinnedSavedDialogs, IBool>,
    Messages.IReorderPinnedSavedDialogsHandler
{
    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestReorderPinnedSavedDialogs obj)
    {
        throw new NotImplementedException();
    }
}
