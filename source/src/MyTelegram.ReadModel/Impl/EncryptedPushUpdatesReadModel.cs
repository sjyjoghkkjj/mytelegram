namespace MyTelegram.ReadModel.Impl;

public class EncryptedPushUpdatesReadModel : IEncryptedPushUpdatesReadModel,
    IAmReadModel,
    IApplyAsync<PushUpdatesAggregate, PushUpdatesId, EncryptedPushUpdatesCreatedEvent>
{
    public string Id { get; private set; } = default!;
    public long InboxOwnerPeerId { get; private set; }
    public long InboxOwnerPermAuthKeyId { get; private set; }
    public int Qts { get; private set; }
    public byte[] Data { get; private set; } = Array.Empty<byte>();

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<PushUpdatesAggregate, PushUpdatesId, EncryptedPushUpdatesCreatedEvent> domainEvent, CancellationToken cancellationToken)
    {
        InboxOwnerPeerId = domainEvent.AggregateEvent.InboxOwnerPeerId;
        InboxOwnerPermAuthKeyId = domainEvent.AggregateEvent.InboxOwnerPermAuthKeyId;
        Qts = domainEvent.AggregateEvent.Qts;
        Data = domainEvent.AggregateEvent.Data;
        Id = domainEvent.AggregateIdentity.Value;
        return Task.CompletedTask;
    }
}

