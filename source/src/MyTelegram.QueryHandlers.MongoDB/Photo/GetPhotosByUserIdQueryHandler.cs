using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyTelegram.Domain.Extensions;

namespace MyTelegram.QueryHandlers.MongoDB.Photo;

public class GetPhotosByUserIdQueryHandler(IQueryOnlyReadModelStore<PhotoReadModel> store) : IQueryHandler<GetPhotosByUserIdQuery, IReadOnlyCollection<IPhotoReadModel>>
{
    public async Task<IReadOnlyCollection<IPhotoReadModel>> ExecuteQueryAsync(GetPhotosByUserIdQuery query, CancellationToken cancellationToken)
    {
        return await store.FindAsync(p => p.UserId == query.UserId && query.PhotoIds.Contains(p.PhotoId), cancellationToken: cancellationToken);
    }
}