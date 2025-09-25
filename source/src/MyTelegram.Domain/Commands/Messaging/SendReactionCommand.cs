namespace MyTelegram.Domain.Commands.Messaging;

public class SendReactionCommand(
    MessageId aggregateId,
    RequestInfo requestInfo,
    long userId,
    MyTelegram.Schema.IReaction reaction,
    bool addToRecent = false)
    : RequestCommand2<MessageAggregate, MessageId, IExecutionResult>(aggregateId, requestInfo)
{
    public long UserId { get; } = userId;
    public MyTelegram.Schema.IReaction Reaction { get; } = reaction;
    public bool AddToRecent { get; } = addToRecent;

    protected override IEnumerable<byte[]> GetSourceIdComponents()
    {
        yield return BitConverter.GetBytes(RequestInfo.ReqMsgId);
        yield return BitConverter.GetBytes(UserId);
    }
}