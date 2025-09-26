namespace MyTelegram.ReadModel.Impl;

public class UserReactionReadModel : IUserReactionReadModel,
    IAmReadModelFor<MessageAggregate, MessageId, MessageReactionAddedEvent>,
    IAmReadModelFor<MessageAggregate, MessageId, MessageReactionRemovedEvent>
{
    public string Id { get; private set; } = null!;
    public long UserId { get; private set; }
    public long PeerId { get; private set; }
    public int MessageId { get; private set; }
    public long ReactionId { get; private set; }
    public Reaction Reaction { get; private set; } = null!;

    public Task ApplyAsync(IReadModelContext context,
        IDomainEvent<MessageAggregate, MessageId, MessageReactionAddedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        var e = domainEvent.AggregateEvent;
        var (ownerPeerId, msgId) = ParseAggregateIdentity(domainEvent.AggregateIdentity.Value);
        var reaction = ToDomainReaction(e.Reaction, e.UserId, DateTime.UtcNow.ToTimestamp());
        Id = $"{ownerPeerId}_{msgId}_{e.UserId}_{reaction.GetReactionId()}";
        UserId = e.UserId;
        PeerId = ownerPeerId;
        MessageId = msgId;
        ReactionId = reaction.GetReactionId();
        Reaction = reaction;
        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context,
        IDomainEvent<MessageAggregate, MessageId, MessageReactionRemovedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        var e = domainEvent.AggregateEvent;
        var (ownerPeerId, msgId) = ParseAggregateIdentity(domainEvent.AggregateIdentity.Value);
        var reaction = ToDomainReaction(e.Reaction, e.UserId, DateTime.UtcNow.ToTimestamp());
        var id = $"{ownerPeerId}_{msgId}_{e.UserId}_{reaction.GetReactionId()}";
        context.MarkForDeletion(id);
        return Task.CompletedTask;
    }

    private static (long ownerPeerId, int messageId) ParseAggregateIdentity(string aggregateId)
    {
        // message_{ownerPeerId}_{messageId} or quick_reply_message_{ownerPeerId}_{messageId}
        var parts = aggregateId.Split('_');
        if (parts.Length < 3) return (0, 0);
        if (long.TryParse(parts[^2], out var owner) && int.TryParse(parts[^1], out var msg))
        {
            return (owner, msg);
        }
        return (0, 0);
    }

    private static Reaction ToDomainReaction(MyTelegram.Schema.IReaction reaction, long userId, int? date)
    {
        switch (reaction)
        {
            case MyTelegram.Schema.TReactionEmoji r:
                return new Reaction(userId, r.Emoticon, null, date);
            case MyTelegram.Schema.TReactionCustomEmoji r:
                return new Reaction(userId, null, r.DocumentId, date);
            case MyTelegram.Schema.TReactionPaid:
                return new Reaction(userId, null, null, date);
            default:
                return new Reaction(userId, null, null, date);
        }
    }
}

