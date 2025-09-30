namespace MyTelegram.Domain.Aggregates.Privacy;

public class PrivacyId(string value) : Identity<PrivacyId>(value)
{
    public static PrivacyId Create(long userId, PrivacyType type)
    {
        return NewDeterministic(GuidFactories.Deterministic.Namespaces.Commands, $"privacy-{userId}-{(int)type}");
    }
}

public class PrivacyChangedEvent(RequestInfo requestInfo, long userId, PrivacyType privacyType, List<PrivacyValueData> rules) : RequestAggregateEvent2<PrivacyAggregate, PrivacyId>(requestInfo)
{
    public long UserId { get; } = userId;
    public PrivacyType PrivacyType { get; } = privacyType;
    public List<PrivacyValueData> Rules { get; } = rules;
}

public class PrivacyState : AggregateState<PrivacyAggregate, PrivacyId, PrivacyState>,
    IApply<PrivacyChangedEvent>
{
    public long UserId { get; private set; }
    public PrivacyType PrivacyType { get; private set; }
    public List<PrivacyValueData> Rules { get; private set; } = [];

    public void Apply(PrivacyChangedEvent aggregateEvent)
    {
        UserId = aggregateEvent.UserId;
        PrivacyType = aggregateEvent.PrivacyType;
        Rules = aggregateEvent.Rules;
    }
}

public class UpdatePrivacyCommand(PrivacyId aggregateId, RequestInfo requestInfo, long userId, PrivacyType privacyType, List<PrivacyValueData> rules)
    : RequestCommand2<PrivacyAggregate, PrivacyId, IExecutionResult>(aggregateId, requestInfo)
{
    public long UserId { get; } = userId;
    public PrivacyType PrivacyType { get; } = privacyType;
    public List<PrivacyValueData> Rules { get; } = rules;
}

public class UpdatePrivacyCommandHandler : CommandHandler<PrivacyAggregate, PrivacyId, UpdatePrivacyCommand>
{
    public override Task ExecuteAsync(PrivacyAggregate aggregate, UpdatePrivacyCommand command, CancellationToken cancellationToken)
    {
        aggregate.UpdatePrivacy(command.RequestInfo, command.UserId, command.PrivacyType, command.Rules);
        return Task.CompletedTask;
    }
}

public class PrivacyAggregate : MyInMemorySnapshotAggregateRoot<PrivacyAggregate, PrivacyId, PrivacySnapshot>
{
    private readonly PrivacyState _state = new();
    public PrivacyAggregate(PrivacyId id) : base(id, SnapshotEveryFewVersionsStrategy.Default)
    {
        Register(_state);
    }

    public void UpdatePrivacy(RequestInfo requestInfo, long userId, PrivacyType privacyType, List<PrivacyValueData> rules)
    {
        Emit(new PrivacyChangedEvent(requestInfo, userId, privacyType, rules));
    }

    protected override Task<PrivacySnapshot> CreateSnapshotAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new PrivacySnapshot(_state.UserId, _state.PrivacyType, _state.Rules));
    }

    protected override Task LoadSnapshotAsync(PrivacySnapshot snapshot, ISnapshotMetadata metadata, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public record PrivacySnapshot(long UserId, PrivacyType PrivacyType, List<PrivacyValueData> Rules) : ISnapshot;

