using EventFlow.ReadStores;

namespace MyTelegram.Messenger.Services.Caching;

public class MultipleAggregateCachedReadModelManager<TReadModelInterface, TReadModel, TReadModelLocator>(
    IReadModelDomainEventApplier readModelDomainEventApplier,
    IServiceProvider serviceProvider,
    TReadModelLocator readModelLocator,
    IReadModelCacheHelper<TReadModelInterface> readModelCacheHelper) :
    CachedReadModelManager<TReadModelInterface, TReadModel>(readModelDomainEventApplier, serviceProvider,
        readModelCacheHelper)
    where TReadModel : class, IReadModel
    where TReadModelInterface : IReadModel
    where TReadModelLocator : IReadModelLocator
{
    private TReadModelLocator _readModelLocator = readModelLocator;

    protected override IEnumerable<string> GetReadModelIds(IDomainEvent domainEvent)
    {
        return _readModelLocator.GetReadModelIds(domainEvent);
    }
}