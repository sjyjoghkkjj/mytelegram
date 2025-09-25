namespace MyTelegram.Domain.Events.User;

public class UserEmailUpdatedEvent(string? email, bool enableEmailLogin) : AggregateEvent<UserAggregate, UserId>
{
    public string? Email { get; } = email;
    public bool EnableEmailLogin { get; } = enableEmailLogin;
}

