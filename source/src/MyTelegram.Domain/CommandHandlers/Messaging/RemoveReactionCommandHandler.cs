namespace MyTelegram.Domain.CommandHandlers.Messaging;

public class RemoveReactionCommandHandler : CommandHandler<MessageAggregate, MessageId, RemoveReactionCommand>
{
    public override Task ExecuteAsync(MessageAggregate aggregate,
        RemoveReactionCommand command,
        CancellationToken cancellationToken)
    {
        aggregate.RemoveReaction(command.RequestInfo,
            command.UserId,
            command.Reaction);
        return Task.CompletedTask;
    }
}