namespace MyTelegram.Domain.Commands.Channel;

public class ReorderPinnedForumTopicsCommand(
    ChannelId aggregateId,
    RequestInfo requestInfo,
    List<int> topicIds
) : RequestCommand2<ChannelAggregate, ChannelId, IExecutionResult>(aggregateId, requestInfo)
{
    public List<int> TopicIds { get; } = topicIds;
}

public class ReorderPinnedForumTopicsCommandHandler : CommandHandler<ChannelAggregate, ChannelId, ReorderPinnedForumTopicsCommand>
{
    public override Task ExecuteAsync(ChannelAggregate aggregate, ReorderPinnedForumTopicsCommand command, CancellationToken cancellationToken)
    {
        aggregate.ReorderPinnedForumTopics(command.RequestInfo, command.TopicIds);
        return Task.CompletedTask;
    }
}

