namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

/// <summary>
/// Get unread reactions to messages you sent
/// <para>Possible errors</para>
/// Code Type Description
/// 400 PEER_ID_INVALID The provided peer id is invalid.
/// See <a href="https://corefork.telegram.org/method/messages.getUnreadReactions" />
///</summary>
internal sealed class GetUnreadReactionsHandler(IQueryProcessor queryProcessor)
    : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestGetUnreadReactions, MyTelegram.Schema.Messages.IMessages>
{
    protected override async Task<MyTelegram.Schema.Messages.IMessages> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestGetUnreadReactions obj)
    {
        // Простейшая реализация: берём последние сообщения владельца, где есть recentReactions
        var take = Math.Clamp(obj.Limit, 5, 50);
        var messages = await queryProcessor.ProcessAsync(new GetRecentMessagesQuery(input.UserId, take), default);
        var withReactions = messages.Where(m => m.RecentReactions2 != null && m.RecentReactions2.Count > 0)
            .Select(m => (IMessage)new TMessage
            {
                Id = m.MessageId,
                PeerId = new TPeerUser { UserId = m.ToPeerId },
                Date = m.Date,
                Message = m.Message,
                Out = m.SenderPeerId == input.UserId
            }).ToList();

        return new TMessages
        {
            Chats = [],
            Messages = new TVector<IMessage>(withReactions),
            Users = []
        };
    }
}
