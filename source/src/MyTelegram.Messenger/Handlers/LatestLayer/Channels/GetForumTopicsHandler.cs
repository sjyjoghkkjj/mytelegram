namespace MyTelegram.Messenger.Handlers.LatestLayer.Channels;

///<summary>
/// Get <a href="https://corefork.telegram.org/api/forum">topics of a forum</a>
/// <para>Possible errors</para>
/// Code Type Description
/// 400 CHANNEL_FORUM_MISSING This supergroup is not a forum.
/// 400 CHANNEL_INVALID The provided channel is invalid.
/// See <a href="https://corefork.telegram.org/method/channels.getForumTopics" />
///</summary>
internal sealed class GetForumTopicsHandler(IAccessHashHelper accessHashHelper, IPeerHelper peerHelper, IQueryProcessor queryProcessor, IPtsHelper ptsHelper)
    : RpcResultObjectHandler<MyTelegram.Schema.Channels.RequestGetForumTopics, MyTelegram.Schema.Messages.IForumTopics>
{
    protected override async Task<MyTelegram.Schema.Messages.IForumTopics> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Channels.RequestGetForumTopics obj)
    {
        await accessHashHelper.CheckAccessHashAsync(input, obj.Channel);
        var peer = peerHelper.GetChannel(obj.Channel);
        var topics = await queryProcessor.ProcessAsync(new GetForumTopicsQuery(peer.PeerId, obj.OffsetDate, obj.OffsetId, obj.OffsetTopic, obj.Q, obj.Limit));

        var tlTopics = new TVector<IForumTopic>(topics.Select(t => new TForumTopic
        {
            Id = t.TopicId,
            Title = t.Title,
            Pinned = t.Pinned,
            IconEmojiId = t.IconEmojiId,
            IconColor = t.IconColor,
            TopMessage = t.TopMessage,
            Date = t.Date
        }));

        return new TForumTopics
        {
            Pts = ptsHelper.GetCachedPts(peer.PeerId),
            Chats = [],
            Messages = [],
            Topics = tlTopics,
            Users = []
        };
    }
}
