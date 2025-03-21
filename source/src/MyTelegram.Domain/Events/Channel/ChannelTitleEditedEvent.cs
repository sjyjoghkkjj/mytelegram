namespace MyTelegram.Domain.Events.Channel;

public class ChannelTitleEditedEvent(
    RequestInfo requestInfo,
    long channelId,
    bool broadcast,
    string title,
    IMessageAction messageAction,
    long randomId)
    : RequestAggregateEvent2<ChannelAggregate, ChannelId>(requestInfo)
{
    public long ChannelId { get; } = channelId;
    public bool Broadcast { get; } = broadcast;
    public long RandomId { get; } = randomId;
    public string Title { get; } = title;
    public IMessageAction MessageAction { get; } = messageAction;
}
