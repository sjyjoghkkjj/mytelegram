namespace MyTelegram.Domain.CommandHandlers.Messaging;

public class CancelScheduledMessageCommandHandler : CommandHandler<MessageAggregate, MessageId, CancelScheduledMessageCommand>
{
    public override Task ExecuteAsync(MessageAggregate aggregate,
        CancelScheduledMessageCommand command,
        CancellationToken cancellationToken)
    {
        aggregate.CancelScheduledMessage(command.RequestInfo, command.ScheduleDate);
        return Task.CompletedTask;
    }
}