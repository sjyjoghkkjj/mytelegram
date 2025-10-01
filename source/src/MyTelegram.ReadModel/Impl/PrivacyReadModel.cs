namespace MyTelegram.ReadModel.Impl;

public class PrivacyReadModel : IPrivacyReadModel,
    IAmReadModelFor<PrivacyAggregate, PrivacyId, PrivacyChangedEvent>
{
    public string Id { get; private set; } = null!;
    public PrivacyType PrivacyType { get; private set; }
    public IReadOnlyList<PrivacyValueData> PrivacyValueDataList { get; private set; } = Array.Empty<PrivacyValueData>();
    public long UserId { get; private set; }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<PrivacyAggregate, PrivacyId, PrivacyChangedEvent> domainEvent, CancellationToken cancellationToken)
    {
        Id = domainEvent.AggregateIdentity.Value;
        UserId = domainEvent.AggregateEvent.UserId;
        PrivacyType = domainEvent.AggregateEvent.PrivacyType;
        PrivacyValueDataList = domainEvent.AggregateEvent.Rules;
        return Task.CompletedTask;
    }
}