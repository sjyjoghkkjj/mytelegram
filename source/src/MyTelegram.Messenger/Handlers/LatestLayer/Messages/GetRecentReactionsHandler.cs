using MyTelegram.Schema.Messages;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

/// <summary>
/// Get recently used <a href="https://corefork.telegram.org/api/reactions">message reactions</a>
/// See <a href="https://corefork.telegram.org/method/messages.getRecentReactions" />
///</summary>
internal sealed class GetRecentReactionsHandler(IQueryProcessor queryProcessor)
    : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestGetRecentReactions, MyTelegram.Schema.Messages.IReactions>
{
    protected override async Task<MyTelegram.Schema.Messages.IReactions> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestGetRecentReactions obj)
    {
        // Собираем по последним N сообщениям текущего пользователя последние реакции
        var take = Math.Clamp(obj.Limit, 5, 50);
        var messages = await queryProcessor.ProcessAsync(new GetRecentMessagesQuery(input.UserId, take), default);

        var set = new Dictionary<long, IReaction>();
        foreach (var m in messages)
        {
            if (m.RecentReactions2 == null) continue;
            foreach (var rr in m.RecentReactions2)
            {
                var id = rr.Reaction.GetReactionId();
                if (!set.ContainsKey(id))
                {
                    set[id] = rr.Reaction;
                }
            }
        }

        var top = set.Values.Take(take).Select(r => (IAvailableReaction)new TAvailableReaction
        {
            Reaction = r is TReactionEmoji e ? e.Emoticon : ":",
            Title = string.Empty,
            StaticIcon = new TDocumentEmpty { Id = 0 },
            AppearAnimation = new TDocumentEmpty { Id = 0 },
            SelectAnimation = new TDocumentEmpty { Id = 0 },
            ActivateAnimation = new TDocumentEmpty { Id = 0 },
            EffectAnimation = new TDocumentEmpty { Id = 0 }
        }).ToList();

        return new TReactions { Reactions = new TVector<IAvailableReaction>(top) };
    }
}
