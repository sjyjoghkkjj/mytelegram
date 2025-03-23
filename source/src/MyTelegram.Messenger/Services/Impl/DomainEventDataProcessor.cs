namespace MyTelegram.Messenger.Services.Impl;

public class DomainEventDataProcessor(ICachedReadModelUpdater cachedReadModelManager) : IDataProcessor<IDomainEvent>
{
    public Task ProcessAsync(IDomainEvent data)
    {
        return cachedReadModelManager.UpdateAsync([data], CancellationToken.None);
    }
}