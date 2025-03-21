// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Channels;

///<summary>
/// See <a href="https://corefork.telegram.org/method/channels.setBoostsToUnblockRestrictions" />
///</summary>
internal sealed class SetBoostsToUnblockRestrictionsHandler : RpcResultObjectHandler<MyTelegram.Schema.Channels.RequestSetBoostsToUnblockRestrictions, MyTelegram.Schema.IUpdates>,
    Channels.ISetBoostsToUnblockRestrictionsHandler
{
    protected override Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Channels.RequestSetBoostsToUnblockRestrictions obj)
    {
        RpcErrors.RpcErrors400.ChatNotModified.ThrowRpcError();

        return null!;
    }
}
