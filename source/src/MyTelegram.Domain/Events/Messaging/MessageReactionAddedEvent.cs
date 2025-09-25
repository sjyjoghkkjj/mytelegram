namespace MyTelegram.Domain.Events.Messaging;

public class MessageReactionAddedEvent(
    RequestInfo requestInfo,
    long userId,
    MyTelegram.Schema.IReaction reaction,
    bool addToRecent = false)
    : RequestAggregateEvent2<MessageAggregate, MessageId>(requestInfo)
{
    public long UserId { get; } = userId;
    public MyTelegram.Schema.IReaction Reaction { get; } = reaction;
    public bool AddToRecent { get; } = addToRecent;
}