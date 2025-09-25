namespace MyTelegram.Domain.Events.Messaging;

public class MessageReactionRemovedEvent(
    RequestInfo requestInfo,
    long userId,
    MyTelegram.Schema.IReaction reaction)
    : RequestAggregateEvent2<MessageAggregate, MessageId>(requestInfo)
{
    public long UserId { get; } = userId;
    public MyTelegram.Schema.IReaction Reaction { get; } = reaction;
}