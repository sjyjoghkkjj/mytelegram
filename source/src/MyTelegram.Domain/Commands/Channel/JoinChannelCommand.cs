namespace MyTelegram.Domain.Commands.Channel;

public class JoinChannelCommand(
    ChannelMemberId aggregateId,
    RequestInfo requestInfo,
    long selfUserId,
    long channelId,
    bool isBroadcast
    )
    : RequestCommand2<ChannelMemberAggregate, ChannelMemberId, IExecutionResult>(aggregateId, requestInfo)
{
    public long ChannelId { get; } = channelId;
    public bool IsBroadcast { get; } = isBroadcast;
    public long SelfUserId { get; } = selfUserId;
}
