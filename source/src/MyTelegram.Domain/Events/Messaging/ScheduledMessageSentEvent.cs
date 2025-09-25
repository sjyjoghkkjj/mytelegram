namespace MyTelegram.Domain.Events.Messaging;

public class ScheduledMessageSentEvent(
    RequestInfo requestInfo,
    MessageItem messageItem,
    int scheduleDate)
    : RequestAggregateEvent2<MessageAggregate, MessageId>(requestInfo)
{
    public MessageItem MessageItem { get; } = messageItem;
    public int ScheduleDate { get; } = scheduleDate;
}