namespace MyTelegram.Domain.Events.Messaging;

public class ScheduledMessageCancelledEvent(
    RequestInfo requestInfo,
    int scheduleDate)
    : RequestAggregateEvent2<MessageAggregate, MessageId>(requestInfo)
{
    public int ScheduleDate { get; } = scheduleDate;
}