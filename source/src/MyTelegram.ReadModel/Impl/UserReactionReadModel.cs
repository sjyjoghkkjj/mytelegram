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
        // Compose deterministic id: ownerPeerId_messageId_userId_reactionId
        var reaction = new Reaction(e.Reaction);
        Id = $"{e.OwnerPeerId}_{e.MessageId}_{e.UserId}_{reaction.GetReactionId()}";
        UserId = e.UserId;
        PeerId = e.OwnerPeerId;
        MessageId = e.MessageId;
        ReactionId = reaction.GetReactionId();
        Reaction = reaction;
        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context,
        IDomainEvent<MessageAggregate, MessageId, MessageReactionRemovedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        var e = domainEvent.AggregateEvent;
        var reaction = new Reaction(e.Reaction);
        var id = $"{e.OwnerPeerId}_{e.MessageId}_{e.UserId}_{reaction.GetReactionId()}";
        context.MarkForDeletion(id);
        return Task.CompletedTask;
    }
}

