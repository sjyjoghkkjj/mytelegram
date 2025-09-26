namespace MyTelegram.QueryHandlers.MongoDB.Stories;

public class GetStoryReactionsListQueryHandler(
    IQueryOnlyReadModelStore<StoryReactionReadModel> store,
    IQueryOnlyReadModelStore<UserReadModel> userStore
) : IQueryHandler<GetStoryReactionsListQuery, IReadOnlyCollection<IStoryViewDetailsReadModel>>
{
    public async Task<IReadOnlyCollection<IStoryViewDetailsReadModel>> ExecuteQueryAsync(GetStoryReactionsListQuery query, CancellationToken cancellationToken)
    {
        // Map StoryReactionReadModel -> IStoryViewDetailsReadModel (simple projection)
        var items = await store.FindAsync(p => p.OwnerPeerId == query.OwnerPeerId && p.StoryId == query.StoryId,
            cancellationToken: cancellationToken);
        var list = new List<IStoryViewDetailsReadModel>();
        foreach (var r in items)
        {
            list.Add(new StoryViewDetailsReadModel
            {
                Id = $"{r.OwnerPeerId}_{r.StoryId}_{r.UserId}",
                OwnerPeerId = r.OwnerPeerId,
                StoryId = r.StoryId,
                UserId = r.UserId,
                Date = r.Date,
                ReactionDate = r.Date,
                ReactionId = r.Reaction.GetReactionId(),
                Reaction = r.Reaction.ToSchema()
            });
        }
        return list;
    }

    private class StoryViewDetailsReadModel : IStoryViewDetailsReadModel
    {
        public string Id { get; set; } = null!;
        public long OwnerPeerId { get; set; }
        public int StoryId { get; set; }
        public long UserId { get; set; }
        public int Date { get; set; }
        public long ReactionId { get; set; }
        public IReaction? Reaction { get; set; }
        public int ReactionDate { get; set; }
    }
}

