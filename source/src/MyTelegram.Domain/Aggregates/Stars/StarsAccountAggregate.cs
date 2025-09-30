namespace MyTelegram.Domain.Aggregates.Stars;

public record StarsTransaction(long UserId, long Amount, string Currency, string Reason, int Date, string TxId);

public class StarsAccountState : AggregateState<StarsAccountAggregate, StarsAccountId, StarsAccountState>,
    IApply<StarsCreditedEvent>,
    IApply<StarsDebitedEvent>
{
    public long UserId { get; private set; }
    public long Balance { get; private set; }
    public List<StarsTransaction> Transactions { get; } = new();

    public void Apply(StarsCreditedEvent aggregateEvent)
    {
        UserId = aggregateEvent.UserId;
        Balance += aggregateEvent.Amount;
        Transactions.Add(new StarsTransaction(aggregateEvent.UserId, aggregateEvent.Amount, aggregateEvent.Currency, aggregateEvent.Reason, aggregateEvent.Date, aggregateEvent.TxId));
    }

    public void Apply(StarsDebitedEvent aggregateEvent)
    {
        UserId = aggregateEvent.UserId;
        Balance -= aggregateEvent.Amount;
        Transactions.Add(new StarsTransaction(aggregateEvent.UserId, -aggregateEvent.Amount, aggregateEvent.Currency, aggregateEvent.Reason, aggregateEvent.Date, aggregateEvent.TxId));
    }
}

public class StarsCreditedEvent(RequestInfo requestInfo, long userId, long amount, string currency, string reason, int date, string txId) : RequestAggregateEvent2<StarsAccountAggregate, StarsAccountId>(requestInfo)
{
    public long UserId { get; } = userId;
    public long Amount { get; } = amount;
    public string Currency { get; } = currency;
    public string Reason { get; } = reason;
    public int Date { get; } = date;
    public string TxId { get; } = txId;
}

public class StarsDebitedEvent(RequestInfo requestInfo, long userId, long amount, string currency, string reason, int date, string txId) : RequestAggregateEvent2<StarsAccountAggregate, StarsAccountId>(requestInfo)
{
    public long UserId { get; } = userId;
    public long Amount { get; } = amount;
    public string Currency { get; } = currency;
    public string Reason { get; } = reason;
    public int Date { get; } = date;
    public string TxId { get; } = txId;
}

public class StarsAccountAggregate : MyInMemorySnapshotAggregateRoot<StarsAccountAggregate, StarsAccountId, StarsAccountSnapshot>
{
    private readonly StarsAccountState _state = new();

    public StarsAccountAggregate(StarsAccountId id) : base(id, SnapshotEveryFewVersionsStrategy.Default)
    {
        Register(_state);
    }

    public void Credit(RequestInfo requestInfo, long userId, long amount, string currency, string reason, string txId, int date)
    {
        Emit(new StarsCreditedEvent(requestInfo, userId, amount, currency, reason, date, txId));
    }

    public void Debit(RequestInfo requestInfo, long userId, long amount, string currency, string reason, string txId, int date)
    {
        if (_state.Balance < amount)
        {
            RpcErrors.RpcErrors400.NotEnoughStars.ThrowRpcError();
        }
        Emit(new StarsDebitedEvent(requestInfo, userId, amount, currency, reason, date, txId));
    }

    protected override Task<StarsAccountSnapshot> CreateSnapshotAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new StarsAccountSnapshot(_state.UserId, _state.Balance));
    }

    protected override Task LoadSnapshotAsync(StarsAccountSnapshot snapshot, ISnapshotMetadata metadata, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public record StarsAccountSnapshot(long UserId, long Balance) : ISnapshot;

