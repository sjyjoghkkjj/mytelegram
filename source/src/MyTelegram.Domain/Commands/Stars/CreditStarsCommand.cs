namespace MyTelegram.Domain.Commands.Stars;

public class CreditStarsCommand(
    StarsAccountId aggregateId,
    RequestInfo requestInfo,
    long userId,
    long amount,
    string currency,
    string reason,
    string txId,
    int date
) : RequestCommand2<StarsAccountAggregate, StarsAccountId, IExecutionResult>(aggregateId, requestInfo)
{
    public long UserId { get; } = userId;
    public long Amount { get; } = amount;
    public string Currency { get; } = currency;
    public string Reason { get; } = reason;
    public string TxId { get; } = txId;
    public int Date { get; } = date;
}

public class CreditStarsCommandHandler : CommandHandler<StarsAccountAggregate, StarsAccountId, CreditStarsCommand>
{
    public override Task ExecuteAsync(StarsAccountAggregate aggregate, CreditStarsCommand command, CancellationToken cancellationToken)
    {
        aggregate.Credit(command.RequestInfo, command.UserId, command.Amount, command.Currency, command.Reason, command.TxId, command.Date);
        return Task.CompletedTask;
    }
}

