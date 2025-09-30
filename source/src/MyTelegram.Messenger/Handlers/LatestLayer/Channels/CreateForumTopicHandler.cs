namespace MyTelegram.Messenger.Handlers.LatestLayer.Channels;

///<summary>
/// Create a <a href="https://corefork.telegram.org/api/forum">forum topic</a>; requires <a href="https://corefork.telegram.org/api/rights"><code>manage_topics</code> rights</a>.
/// <para>Possible errors</para>
/// Code Type Description
/// 400 CHANNEL_INVALID The provided channel is invalid.
/// 403 CHAT_WRITE_FORBIDDEN You can't write in this chat.
/// See <a href="https://corefork.telegram.org/method/channels.createForumTopic" />
///</summary>
internal sealed class CreateForumTopicHandler(
    IAccessHashHelper accessHashHelper,
    IPeerHelper peerHelper,
    IChannelAdminRightsChecker rightsChecker,
    ICommandBus commandBus) : RpcResultObjectHandler<MyTelegram.Schema.Channels.RequestCreateForumTopic, MyTelegram.Schema.IUpdates>
{
    protected override async Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Channels.RequestCreateForumTopic obj)
    {
        await accessHashHelper.CheckAccessHashAsync(input, obj.Channel);
        var peer = peerHelper.GetChannel(obj.Channel);
        await rightsChecker.CheckAdminRightAsync(peer.PeerId, input.UserId, r => r.AdminRights.ManageTopics, RpcErrors.RpcErrors403.ChatAdminRequired);

        if (string.IsNullOrWhiteSpace(obj.Title))
        {
            RpcErrors.RpcErrors400.TopicTitleEmpty.ThrowRpcError();
        }

        var topicId = (int)(obj.RandomId % int.MaxValue);
        var sendAs = obj.SendAs != null ? peerHelper.GetPeer(obj.SendAs, input.UserId) : null;
        var evt = new ForumTopicCreatedEvent(input.ToRequestInfo(), peer.PeerId, topicId, obj.Title, obj.IconColor, obj.IconEmojiId, (int)DateTime.UtcNow.ToTimestamp(), 0, sendAs);
        await commandBus.PublishAsync(new RaiseEventCommand<ChannelAggregate, ChannelId>(ChannelId.Create(peer.PeerId), input.ToRequestInfo(), evt));

        return null!;
    }
}
