namespace MyTelegram.Domain.CommandHandlers.Messaging;

public class SendReactionCommandHandler : CommandHandler<MessageAggregate, MessageId, SendReactionCommand>
{
    public override Task ExecuteAsync(MessageAggregate aggregate,
        SendReactionCommand command,
        CancellationToken cancellationToken)
    {
        aggregate.SendReaction(command.RequestInfo,
            command.UserId,
            command.Reaction,
            command.AddToRecent);
        return Task.CompletedTask;
    }
}