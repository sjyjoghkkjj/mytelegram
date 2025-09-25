namespace MyTelegram.Domain.Commands.Messaging;

public class CancelScheduledMessageCommand(
    MessageId aggregateId,
    RequestInfo requestInfo,
    int scheduleDate)
    : RequestCommand2<MessageAggregate, MessageId, IExecutionResult>(aggregateId, requestInfo)
{
    public int ScheduleDate { get; } = scheduleDate;

    protected override IEnumerable<byte[]> GetSourceIdComponents()
    {
        yield return BitConverter.GetBytes(RequestInfo.ReqMsgId);
        yield return BitConverter.GetBytes(ScheduleDate);
    }
}