namespace MyTelegram.Domain.Events.UserName;

public class UserNameCreatedEvent(long userId, string userName, int date) : AggregateEvent<UserNameAggregate, UserNameId>
{
    public long UserId { get; } = userId;
    public string UserName { get; } = userName;
    public int Date { get; } = date;
}
