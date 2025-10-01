namespace MyTelegram.Domain.Commands.PushUpdates;

public class CreateEncryptedPushUpdatesCommand(
    PushUpdatesId aggregateId,
    long inboxOwnerPeerId,
    byte[] data,
    int qts,
    long inboxOwnerPermAuthKeyId)
    : Command<PushUpdatesAggregate, PushUpdatesId, IExecutionResult>(aggregateId)
{
    public long InboxOwnerPeerId { get; } = inboxOwnerPeerId;
    public byte[] Data { get; } = data;
    public int Qts { get; } = qts;
    public long InboxOwnerPermAuthKeyId { get; } = inboxOwnerPermAuthKeyId;
}