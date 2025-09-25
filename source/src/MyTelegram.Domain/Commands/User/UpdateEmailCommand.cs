namespace MyTelegram.Domain.Commands.User;

public class UpdateEmailCommand(UserId aggregateId, string? email, bool enableEmailLogin)
    : Command<UserAggregate, UserId, IExecutionResult>(aggregateId)
{
    public string? Email { get; } = email;
    public bool EnableEmailLogin { get; } = enableEmailLogin;
}

