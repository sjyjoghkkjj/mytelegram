namespace MyTelegram.Messenger.Handlers.LatestLayer.Channels;

///<summary>
/// Reorder pinned forum topics
/// <para>Possible errors</para>
/// Code Type Description
/// 400 CHANNEL_INVALID The provided channel is invalid.
/// See <a href="https://corefork.telegram.org/method/channels.reorderPinnedForumTopics" />
///</summary>
internal sealed class ReorderPinnedForumTopicsHandler(
    IAccessHashHelper accessHashHelper,
    IPeerHelper peerHelper,
    IChannelAdminRightsChecker rightsChecker,
    ICommandBus commandBus) : RpcResultObjectHandler<MyTelegram.Schema.Channels.RequestReorderPinnedForumTopics, MyTelegram.Schema.IUpdates>
{
    protected override async Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Channels.RequestReorderPinnedForumTopics obj)
    {
        await accessHashHelper.CheckAccessHashAsync(input, obj.Channel);
        var peer = peerHelper.GetChannel(obj.Channel);
        await rightsChecker.CheckAdminRightAsync(peer.PeerId, input.UserId, r => r.AdminRights.ManageTopics, RpcErrors.RpcErrors403.ChatAdminRequired);

        var cmd = new ReorderPinnedForumTopicsCommand(ChannelId.Create(peer.PeerId), input.ToRequestInfo(), obj.Order.ToList());
        await commandBus.PublishAsync(cmd);
        return null!;
    }
}
