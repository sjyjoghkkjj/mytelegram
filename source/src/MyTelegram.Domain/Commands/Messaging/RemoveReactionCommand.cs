namespace MyTelegram.Domain.Commands.Messaging;

public class RemoveReactionCommand(
    MessageId aggregateId,
    RequestInfo requestInfo,
    long userId,
    MyTelegram.Schema.IReaction reaction)
    : RequestCommand2<MessageAggregate, MessageId, IExecutionResult>(aggregateId, requestInfo)
{
    public long UserId { get; } = userId;
    public MyTelegram.Schema.IReaction Reaction { get; } = reaction;

    protected override IEnumerable<byte[]> GetSourceIdComponents()
    {
        yield return BitConverter.GetBytes(RequestInfo.ReqMsgId);
        yield return BitConverter.GetBytes(UserId);
    }
}