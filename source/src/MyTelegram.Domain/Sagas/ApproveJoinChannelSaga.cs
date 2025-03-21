namespace MyTelegram.Domain.Sagas;

public class ApproveJoinChannelSaga(ApproveJoinChannelSagaId id, IEventStore eventStore)
    : MyInMemoryAggregateSaga<ApproveJoinChannelSaga, ApproveJoinChannelSagaId, ApproveJoinChannelSagaLocator>(id,
            eventStore),
        ISagaIsStartedBy<ChannelAggregate, ChannelId, ChatJoinRequestHiddenEvent>
{
    public Task HandleAsync(IDomainEvent<ChannelAggregate, ChannelId, ChatJoinRequestHiddenEvent> domainEvent, ISagaContext sagaContext, CancellationToken cancellationToken)
    {
        if (domainEvent.AggregateEvent.Approved)
        {
            var command = new StartInviteToChannelCommand(TempId.New,
                domainEvent.AggregateEvent.RequestInfo,
                domainEvent.AggregateEvent.ChannelId,
                domainEvent.AggregateEvent.IsBroadcast,
                false,
                domainEvent.AggregateEvent.RequestInfo.UserId,
                0,
                0,
                [domainEvent.AggregateEvent.UserId],
                [],
                ChatJoinType.ApprovedByAdmin
            );
            Publish(command);
        }

        return CompleteAsync(cancellationToken);
    }
}