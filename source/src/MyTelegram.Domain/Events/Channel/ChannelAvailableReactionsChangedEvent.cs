namespace MyTelegram.Domain.Events.Channel;

public class ChannelAvailableReactionsChangedEvent(
    RequestInfo requestInfo,
    long channelId,
    ReactionType reactionType,
    bool allowCustom,
    List<string>? availableReactions
    ) : RequestAggregateEvent2<ChannelAggregate, ChannelId>(requestInfo)
{
    public long ChannelId { get; } = channelId;
    public ReactionType ReactionType { get; } = reactionType;
    public bool AllowCustom { get; } = allowCustom;
    public List<string>? AvailableReactions { get; } = availableReactions;
}

