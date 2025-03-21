using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyTelegram.QueryHandlers.MongoDB.ChatAdmin;

public class GetChatAdminListByChannelIdQueryHandler(IQueryOnlyReadModelStore<ChatAdminReadModel> store) : IQueryHandler<GetChatAdminListByChannelIdQuery,
    IReadOnlyCollection<IChatAdminReadModel>>
{
    public async Task<IReadOnlyCollection<IChatAdminReadModel>> ExecuteQueryAsync(GetChatAdminListByChannelIdQuery query, CancellationToken cancellationToken)
    {
        return await store.FindAsync(p => p.PeerId == query.PeerId, query.Skip, query.Limit, cancellationToken: cancellationToken);
    }
}