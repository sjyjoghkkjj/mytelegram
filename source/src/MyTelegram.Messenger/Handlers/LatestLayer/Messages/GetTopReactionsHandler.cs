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
        // Упрощённая агрегация: берём топ реакций из последних N сообщений владельца
        var reactions = new Dictionary<long, int>();
        var take = Math.Clamp(obj.Limit, 5, 50);
        var messages = await queryProcessor.ProcessAsync(new GetRecentMessagesQuery(input.UserId, take), default);
        foreach (var m in messages)
        {
            if (m.Reactions != null)
            {
                foreach (var r in m.Reactions)
                {
                    var id = r.Reaction.GetReactionId();
                    reactions[id] = reactions.GetValueOrDefault(id) + r.Count;
                }
            }
        }

        var top = reactions.OrderByDescending(kv => kv.Value).Take(take)
            .Select(kv => (MyTelegram.Schema.IAvailableReaction)new MyTelegram.Schema.TAvailableReaction
            {
                Reaction = new MyTelegram.Schema.TReactionEmoji { Emoticon = ":" },
                Title = kv.Value.ToString()
            }).ToList();

        return new TReactions { Reactions = new TVector<MyTelegram.Schema.IAvailableReaction>(top) };
    }
}
