using MyTelegram.Services.Services.IdGenerator;

namespace MyTelegram.Messenger.Services.Impl;

public class MongoDbHighValueGenerator(IMongoDbIdGenerator idGenerator) : IHiLoHighValueGenerator, ITransientDependency
{
    public Task<long> GetNewHighValueAsync(IdType idType, long key, CancellationToken cancellationToken = default)
    {
        return idGenerator.NextLongIdAsync(idType, key, cancellationToken: cancellationToken);
    }
}
