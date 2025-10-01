namespace MyTelegram.Domain.Commands.Channel;

public class ChangeAvailableReactionsCommand(
    ChannelId aggregateId,
    RequestInfo requestInfo,
    ReactionType reactionType,
    bool allowCustom,
    List<string>? availableReactions
) : RequestCommand2<ChannelAggregate, ChannelId, IExecutionResult>(aggregateId, requestInfo)
{
    public ReactionType ReactionType { get; } = reactionType;
    public bool AllowCustom { get; } = allowCustom;
    public List<string>? AvailableReactions { get; } = availableReactions;
}

