namespace MyTelegram.ReadModel.Impl;

public class ScheduleMessageReadModel : IScheduleMessageReadModel, IAmReadModel,
    IApplyAsync<MessageAggregate, MessageId, ScheduledMessageCreatedEvent>
{
    public string Id { get; private set; } = default!;
    public long UserId { get; private set; }
    public long ToPeerId { get; private set; }
    public int MessageId { get; private set; }
    public SendMessageItem Item { get; private set; } = default!;
    public int ScheduleDate { get; private set; }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<MessageAggregate, MessageId, ScheduledMessageCreatedEvent> domainEvent, CancellationToken cancellationToken)
    {
        var item = domainEvent.AggregateEvent.MessageItem;
        UserId = item.SenderUserId;
        ToPeerId = item.ToPeer.PeerId;
        MessageId = domainEvent.AggregateIdentity.MessageId;
        Item = new SendMessageItem(
            item.SenderUserId,
            item.ToPeer,
            item.MessageType,
            item.MessageSubType,
            item.InputReplyTo,
            item.Entities,
            item.Flags,
            item.Message,
            item.RandomId,
            item.Media,
            item.Effect,
            item.SendAs
        );
        ScheduleDate = domainEvent.AggregateEvent.ScheduleDate;
        Id = domainEvent.AggregateIdentity.Value;
        return Task.CompletedTask;
    }
}