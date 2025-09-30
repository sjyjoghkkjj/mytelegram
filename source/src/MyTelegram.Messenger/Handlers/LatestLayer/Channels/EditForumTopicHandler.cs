namespace MyTelegram.Messenger.Handlers.LatestLayer.Channels;

///<summary>
/// Edit <a href="https://corefork.telegram.org/api/forum">forum topic</a>; requires <a href="https://corefork.telegram.org/api/rights"><code>manage_topics</code> rights</a>.
/// <para>Possible errors</para>
/// Code Type Description
/// 400 TOPIC_ID_INVALID The specified topic ID is invalid.
/// 400 TOPIC_NOT_MODIFIED The updated topic info is equal to the current topic info, nothing was changed.
/// See <a href="https://corefork.telegram.org/method/channels.editForumTopic" />
///</summary>
internal sealed class EditForumTopicHandler(
    IAccessHashHelper accessHashHelper,
    IPeerHelper peerHelper,
    IChannelAdminRightsChecker rightsChecker,
    ICommandBus commandBus) : RpcResultObjectHandler<MyTelegram.Schema.Channels.RequestEditForumTopic, MyTelegram.Schema.IUpdates>
{
    protected override async Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Channels.RequestEditForumTopic obj)
    {
        await accessHashHelper.CheckAccessHashAsync(input, obj.Channel);
        var peer = peerHelper.GetChannel(obj.Channel);
        await rightsChecker.CheckAdminRightAsync(peer.PeerId, input.UserId, r => r.AdminRights.ManageTopics, RpcErrors.RpcErrors403.ChatAdminRequired);

        var cmd = new EditForumTopicCommand(ChannelId.Create(peer.PeerId), input.ToRequestInfo(), obj.TopicId, obj.Title, obj.IconColor, obj.IconEmojiId);
        await commandBus.PublishAsync(cmd);
        return null!;
    }
}
