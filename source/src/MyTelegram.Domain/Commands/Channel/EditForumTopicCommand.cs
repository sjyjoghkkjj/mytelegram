namespace MyTelegram.Domain.Commands.Channel;

public class EditForumTopicCommand(
    ChannelId aggregateId,
    RequestInfo requestInfo,
    int topicId,
    string? title,
    int? iconColor,
    long? iconEmojiId
) : RequestCommand2<ChannelAggregate, ChannelId, IExecutionResult>(aggregateId, requestInfo)
{
    public int TopicId { get; } = topicId;
    public string? Title { get; } = title;
    public int? IconColor { get; } = iconColor;
    public long? IconEmojiId { get; } = iconEmojiId;
}

public class EditForumTopicCommandHandler : CommandHandler<ChannelAggregate, ChannelId, EditForumTopicCommand>
{
    public override Task ExecuteAsync(ChannelAggregate aggregate, EditForumTopicCommand command, CancellationToken cancellationToken)
    {
        aggregate.EditForumTopic(command.RequestInfo, command.TopicId, command.Title, command.IconColor, command.IconEmojiId);
        return Task.CompletedTask;
    }
}

