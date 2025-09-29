using MyTelegram.Schema.Messages;
using MyTelegram.ReadModel.Interfaces;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

///<summary>
/// Got popular <a href="https://corefork.telegram.org/api/reactions">message reactions</a>
/// See <a href="https://corefork.telegram.org/method/messages.getTopReactions" />
///</summary>
internal sealed class GetTopReactionsHandler(IQueryProcessor queryProcessor) : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestGetTopReactions, MyTelegram.Schema.Messages.IReactions>
{
    protected override Task<MyTelegram.Schema.Messages.IReactions> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestGetTopReactions obj)
    {
        return HandleAsync(input, obj);
    }

    private async Task<IReactions> HandleAsync(IRequestInput input, MyTelegram.Schema.Messages.RequestGetTopReactions obj)
    {
        // Aggregate from the owner's recent messages reactions
        var counter = new Dictionary<long, int>();
        var take = Math.Clamp(obj.Limit, 5, 50);
        var messages = await queryProcessor.ProcessAsync(new GetRecentMessagesQuery(input.UserId, take), default);
        foreach (var m in messages)
        {
            if (m.Reactions == null) { continue; }
            foreach (var r in m.Reactions)
            {
                var id = r.GetReactionId();
                counter[id] = counter.GetValueOrDefault(id) + r.Count;
            }
        }

        var top = counter
            .OrderByDescending(kv => kv.Value)
            .Take(take)
            .Select(kv => (MyTelegram.Schema.IAvailableReaction)new MyTelegram.Schema.TAvailableReaction
            {
                Reaction = mFromId(kv.Key),
                Title = kv.Value.ToString()
            }).ToList();

        return new TReactions { Reactions = new TVector<MyTelegram.Schema.IAvailableReaction>(top) };

        static MyTelegram.Schema.IReaction mFromId(long id)
        {
            // Heuristic: values >= 1e12 are likely custom emoji document IDs
            if (id >= 1_000_000_000_000)
            {
                return new MyTelegram.Schema.TReactionCustomEmoji { DocumentId = id };
            }
            // Fallback: cannot reconstruct emoticon reliably, return empty to avoid lying
            return new MyTelegram.Schema.TReactionEmpty();
        }
    }
}
