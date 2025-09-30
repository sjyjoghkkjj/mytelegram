namespace MyTelegram.Messenger.Handlers.LatestLayer.Channels;

///<summary>
/// Pin or unpin <a href="https://corefork.telegram.org/api/forum">forum topics</a>
/// See <a href="https://corefork.telegram.org/method/channels.updatePinnedForumTopic" />
///</summary>
internal sealed class UpdatePinnedForumTopicHandler(
    IAccessHashHelper accessHashHelper,
    IPeerHelper peerHelper,
    IChannelAdminRightsChecker rightsChecker,
    ICommandBus commandBus) : RpcResultObjectHandler<MyTelegram.Schema.Channels.RequestUpdatePinnedForumTopic, MyTelegram.Schema.IUpdates>
{
    protected override async Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Channels.RequestUpdatePinnedForumTopic obj)
    {
        await accessHashHelper.CheckAccessHashAsync(input, obj.Channel);
        var peer = peerHelper.GetChannel(obj.Channel);
        await rightsChecker.CheckAdminRightAsync(peer.PeerId, input.UserId, r => r.AdminRights.ManageTopics, RpcErrors.RpcErrors403.ChatAdminRequired);

        var cmd = new PinForumTopicCommand(ChannelId.Create(peer.PeerId), input.ToRequestInfo(), obj.TopicId, obj.Pinned);
        await commandBus.PublishAsync(cmd);
        return null!;
    }
}
