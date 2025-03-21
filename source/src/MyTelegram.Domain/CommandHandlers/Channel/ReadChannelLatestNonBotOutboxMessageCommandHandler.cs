namespace MyTelegram.Domain.CommandHandlers.Channel;

public class ReadChannelLatestNonBotOutboxMessageCommandHandler : CommandHandler<ChannelAggregate, ChannelId,
    ReadChannelLatestNoneBotOutboxMessageCommand>
{
    public override Task ExecuteAsync(ChannelAggregate aggregate,
        ReadChannelLatestNoneBotOutboxMessageCommand command,
        CancellationToken cancellationToken)
    {
        aggregate.ReadChannelLatestNonBotOutboxMessage(command.RequestInfo, command.SourceCommandId);
        return Task.CompletedTask;
    }
}
