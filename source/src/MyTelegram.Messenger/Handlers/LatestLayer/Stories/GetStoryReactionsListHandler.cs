using MyTelegram.Schema.Stories;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Stories;

///<summary>
/// See <a href="https://corefork.telegram.org/method/stories.getStoryReactionsList" />
///</summary>
internal sealed class GetStoryReactionsListHandler(IQueryProcessor queryProcessor) : RpcResultObjectHandler<MyTelegram.Schema.Stories.RequestGetStoryReactionsList, MyTelegram.Schema.Stories.IStoryReactionsList>
{
    protected override async Task<MyTelegram.Schema.Stories.IStoryReactionsList> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Stories.RequestGetStoryReactionsList obj)
    {
        var ownerPeerId = input.UserId;
        var details = await queryProcessor.ProcessAsync(new GetStoryReactionsListQuery(ownerPeerId, obj.StoryId, null, 0, 100));
        var list = new TStoryReactionsList
        {
            Count = details.Count,
            Reactions = new TVector<IStoryReaction>(),
            Chats = [],
            Users = []
        };
        foreach (var d in details)
        {
            list.Reactions.Add(new TStoryReaction
            {
                UserId = d.UserId,
                Reaction = d.Reaction ?? new TReactionEmpty(),
                Date = d.ReactionDate
            });
        }
        return list;
    }
}
