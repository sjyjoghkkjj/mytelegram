namespace MyTelegram.QueryHandlers.MongoDB.Messaging;

public class GetMessageReadParticipantsQueryHandler(IQueryOnlyReadModelStore<ReadingHistoryReadModel> store) :
    IQueryHandler<GetMessageReadParticipantsQuery,
    IReadOnlyCollection<IReadingHistoryReadModel>>
{
    public async Task<IReadOnlyCollection<IReadingHistoryReadModel>> ExecuteQueryAsync(GetMessageReadParticipantsQuery query,
        CancellationToken cancellationToken)
    {
        return await store.FindAsync(p => p.TargetPeerId == query.TargetPeerId && p.MessageId == query.MessageId, cancellationToken: cancellationToken);
    }
}
