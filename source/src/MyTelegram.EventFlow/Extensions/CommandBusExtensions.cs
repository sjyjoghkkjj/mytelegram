using EventFlow;
using EventFlow.Queries;

namespace MyTelegram.EventFlow.Extensions;

public static class CommandBusExtensions
{
    public static Task<TResult> PublishAsync<TAggregate, TIdentity, TResult>(this ICommandBus commandBus, ICommand<TAggregate, TIdentity, TResult> command) where TAggregate : IAggregateRoot<TIdentity> where TIdentity : IIdentity where TResult : IExecutionResult
    {
        return commandBus.PublishAsync(command, CancellationToken.None);
    }
}


public static class QueryProcessorExtensions
{
    public static Task<TResult> ProcessAsync<TResult>(this IQueryProcessor queryProcessor, IQuery<TResult> query)
    {
        return queryProcessor.ProcessAsync(query, CancellationToken.None);
    }
}