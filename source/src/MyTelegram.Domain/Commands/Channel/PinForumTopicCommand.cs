namespace MyTelegram.Domain.Commands.Channel;

public class PinForumTopicCommand(
    ChannelId aggregateId,
    RequestInfo requestInfo,
    int topicId,
    bool pinned
) : RequestCommand2<ChannelAggregate, ChannelId, IExecutionResult>(aggregateId, requestInfo)
{
    public int TopicId { get; } = topicId;
    public bool Pinned { get; } = pinned;
}

public class PinForumTopicCommandHandler : CommandHandler<ChannelAggregate, ChannelId, PinForumTopicCommand>
{
    public override Task ExecuteAsync(ChannelAggregate aggregate, PinForumTopicCommand command, CancellationToken cancellationToken)
    {
        aggregate.PinForumTopic(command.RequestInfo, command.TopicId, command.Pinned);
        return Task.CompletedTask;
    }
}

