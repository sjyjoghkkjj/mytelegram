namespace MyTelegram.Domain.CommandHandlers.Channel;

public class ChangeAvailableReactionsCommandHandler : CommandHandler<ChannelAggregate, ChannelId, ChangeAvailableReactionsCommand>
{
    public override Task ExecuteAsync(ChannelAggregate aggregate,
        ChangeAvailableReactionsCommand command,
        CancellationToken cancellationToken)
    {
        aggregate.ChangeAvailableReactions(command.RequestInfo,
            command.ReactionType,
            command.AllowCustom,
            command.AvailableReactions);
        return Task.CompletedTask;
    }
}

