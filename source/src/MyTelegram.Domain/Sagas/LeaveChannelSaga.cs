namespace MyTelegram.Domain.Sagas;

[JsonConverter(typeof(SystemTextJsonSingleValueObjectConverter<LeaveChannelSagaId>))]
public class LeaveChannelSagaId(string value) : SingleValueObject<string>(value), ISagaId;

public class LeaveChannelSagaLocator : DefaultSagaLocator<LeaveChannelSaga, LeaveChannelSagaId>
{
    protected override LeaveChannelSagaId CreateSagaId(string requestId)
    {
        return new LeaveChannelSagaId(requestId);
    }
}

public class LeaveChannelSaga(LeaveChannelSagaId id, IEventStore eventStore) : MyInMemoryAggregateSaga<LeaveChannelSaga, LeaveChannelSagaId, LeaveChannelSagaLocator>(id, eventStore),
    ISagaIsStartedBy<ChannelMemberAggregate, ChannelMemberId, ChannelMemberLeftEvent>
{
    public Task HandleAsync(IDomainEvent<ChannelMemberAggregate, ChannelMemberId, ChannelMemberLeftEvent> domainEvent, ISagaContext sagaContext, CancellationToken cancellationToken)
    {
        var command = new UpdateParticipantCountCommand(ChannelId.Create(domainEvent.AggregateEvent.ChannelId), -1);
        Publish(command);
        Complete();
        return Task.CompletedTask;
    }
}
