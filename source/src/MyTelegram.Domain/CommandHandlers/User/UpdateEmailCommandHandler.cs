namespace MyTelegram.Domain.CommandHandlers.User;

public class UpdateEmailCommandHandler : CommandHandler<UserAggregate, UserId, UpdateEmailCommand>
{
    public override Task ExecuteAsync(UserAggregate aggregate, UpdateEmailCommand command, CancellationToken cancellationToken)
    {
        aggregate.UpdateEmail(command.Email, command.EnableEmailLogin);
        return Task.CompletedTask;
    }
}

