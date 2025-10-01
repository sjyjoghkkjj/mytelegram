namespace MyTelegram.Domain.Aggregates.Stars;

public class StarsAccountId(string value) : Identity<StarsAccountId>(value)
{
    public static StarsAccountId Create(long userId)
    {
        return NewDeterministic(GuidFactories.Deterministic.Namespaces.Commands, $"stars-{userId}");
    }
}

