namespace MyTelegram.Domain.Commands.Channel;

public class CreateForumTopicCommand(
    ChannelId aggregateId,
    RequestInfo requestInfo,
    int topicId,
    string title,
    int? iconColor,
    long? iconEmojiId,
    int date,
    int topMessageId,
    Peer? sendAs
) : RequestCommand2<ChannelAggregate, ChannelId, IExecutionResult>(aggregateId, requestInfo)
{
    public int TopicId { get; } = topicId;
    public string Title { get; } = title;
    public int? IconColor { get; } = iconColor;
    public long? IconEmojiId { get; } = iconEmojiId;
    public int Date { get; } = date;
    public int TopMessageId { get; } = topMessageId;
    public Peer? SendAs { get; } = sendAs;
}

public class CreateForumTopicCommandHandler : CommandHandler<ChannelAggregate, ChannelId, CreateForumTopicCommand>
{
    public override Task ExecuteAsync(ChannelAggregate aggregate, CreateForumTopicCommand command, CancellationToken cancellationToken)
    {
        aggregate.CreateForumTopic(command.RequestInfo, command.TopicId, command.Title, command.IconColor, command.IconEmojiId, command.Date, command.TopMessageId, command.SendAs);
        return Task.CompletedTask;
    }
}

