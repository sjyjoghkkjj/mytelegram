namespace MyTelegram.Domain.Events.Channel;

public class ForumTopicCreatedEvent(
    RequestInfo requestInfo,
    long channelId,
    int topicId,
    string title,
    int? iconColor,
    long? iconEmojiId,
    int date,
    int topMessageId,
    Peer? sendAs
) : RequestAggregateEvent2<ChannelAggregate, ChannelId>(requestInfo)
{
    public long ChannelId { get; } = channelId;
    public int TopicId { get; } = topicId;
    public string Title { get; } = title;
    public int? IconColor { get; } = iconColor;
    public long? IconEmojiId { get; } = iconEmojiId;
    public int Date { get; } = date;
    public int TopMessageId { get; } = topMessageId;
    public Peer? SendAs { get; } = sendAs;
}

public class ForumTopicEditedEvent(
    RequestInfo requestInfo,
    long channelId,
    int topicId,
    string? title,
    int? iconColor,
    long? iconEmojiId
) : RequestAggregateEvent2<ChannelAggregate, ChannelId>(requestInfo)
{
    public long ChannelId { get; } = channelId;
    public int TopicId { get; } = topicId;
    public string? Title { get; } = title;
    public int? IconColor { get; } = iconColor;
    public long? IconEmojiId { get; } = iconEmojiId;
}

public class ForumTopicPinnedEvent(
    RequestInfo requestInfo,
    long channelId,
    int topicId,
    bool pinned
) : RequestAggregateEvent2<ChannelAggregate, ChannelId>(requestInfo)
{
    public long ChannelId { get; } = channelId;
    public int TopicId { get; } = topicId;
    public bool Pinned { get; } = pinned;
}

public class ForumTopicPinnedOrderChangedEvent(
    RequestInfo requestInfo,
    long channelId,
    List<int> pinnedTopicIds
) : RequestAggregateEvent2<ChannelAggregate, ChannelId>(requestInfo)
{
    public long ChannelId { get; } = channelId;
    public List<int> PinnedTopicIds { get; } = pinnedTopicIds;
}

